#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat >&2 <<'EOF'
Usage: scripts/verify-package-family.sh --family <family> --version <version> --package-dir <directory>

Verifies the package set, package metadata, dependency boundary, and an external consumer.
EOF
}

family=""
version=""
package_dir=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    --family)
      [[ $# -ge 2 ]] || { usage; exit 2; }
      family="$2"
      shift 2
      ;;
    --version)
      [[ $# -ge 2 ]] || { usage; exit 2; }
      version="$2"
      shift 2
      ;;
    --package-dir)
      [[ $# -ge 2 ]] || { usage; exit 2; }
      package_dir="$2"
      shift 2
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      usage
      exit 2
      ;;
  esac
done

if [[ -z "${family}" || -z "${version}" || -z "${package_dir}" ]]; then
  usage
  exit 2
fi

repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
source "${repository_root}/scripts/package-family-common.sh"

resolve_package_family "${family}"
validate_package_family_version "${version}"

if [[ ! -d "${package_dir}" ]]; then
  echo "ERROR: Package directory does not exist: ${package_dir}" >&2
  exit 1
fi
if ! command -v unzip >/dev/null 2>&1; then
  echo "ERROR: unzip is required." >&2
  exit 1
fi

package_dir="$(cd "${package_dir}" && pwd)"
temp_dir="$(mktemp -d)"
trap 'rm -rf "${temp_dir}"' EXIT

expected_artifacts="$(
  for package_id in "${package_family_ids[@]}"; do
    printf '%s.%s.nupkg\n' "${package_id}" "${package_family_version}"
  done |
    sort
)"
actual_artifacts="$(
  find "${package_dir}" -maxdepth 1 -type f -name '*.nupkg' -print |
    sed 's#.*/##' |
    sort
)"
if [[ "${actual_artifacts}" != "${expected_artifacts}" ]]; then
  echo "ERROR: Package artifact set does not match ${package_family_name}." >&2
  echo "Expected:" >&2
  printf '%s\n' "${expected_artifacts}" >&2
  echo "Actual:" >&2
  printf '%s\n' "${actual_artifacts}" >&2
  exit 1
fi

read_dependency_version() {
  local nuspec_path="$1"
  local dependency_id="$2"

  DEPENDENCY_ID="${dependency_id}" perl -ne '
    my $dependency_id = $ENV{"DEPENDENCY_ID"};
    while (/<dependency\b([^>]*)>/g) {
      my $attributes = $1;
      next unless $attributes =~ /\bid="([^"]+)"/;
      next unless $1 eq $dependency_id;
      print "$1\n" if $attributes =~ /\bversion="([^"]+)"/;
    }
  ' "${nuspec_path}"
}

for package_id in "${package_family_ids[@]}"; do
  package_path="${package_dir}/${package_id}.${package_family_version}.nupkg"
  nuspec_entry="${package_id}.nuspec"
  package_entries="$(unzip -Z1 "${package_path}")"

  for required_entry in "${nuspec_entry}" README.md LICENSE "lib/netstandard2.1/${package_id}.dll"; do
    if ! grep -Fx "${required_entry}" <<< "${package_entries}" >/dev/null; then
      echo "ERROR: ${package_id} is missing ${required_entry}." >&2
      exit 1
    fi
  done

  nuspec_path="${temp_dir}/${nuspec_entry}"
  unzip -p "${package_path}" "${nuspec_entry}" > "${nuspec_path}"

  grep -F "<id>${package_id}</id>" "${nuspec_path}" >/dev/null
  grep -F "<version>${package_family_version}</version>" "${nuspec_path}" >/dev/null
  grep -F "<repository type=\"git\" url=\"https://github.com/mackysoft/dotnet-foundations\"" "${nuspec_path}" >/dev/null

  dependency_ids="$(
    perl -ne '
      while (/<dependency\b([^>]*)>/g) {
        my $attributes = $1;
        print "$1\n" if $attributes =~ /\bid="([^"]+)"/;
      }
    ' "${nuspec_path}" |
      sort -u
  )"

  case "${package_id}" in
    MackySoft.Text.Vocabularies)
      if [[ -n "${dependency_ids}" ]]; then
        echo "ERROR: Core vocabulary package must not declare package dependencies." >&2
        printf '%s\n' "${dependency_ids}" >&2
        exit 1
      fi
      ;;
    MackySoft.Text.Vocabularies.Json)
      expected_dependencies=$'MackySoft.Text.Vocabularies\nSystem.Text.Json'
      if [[ "${dependency_ids}" != "${expected_dependencies}" ]]; then
        echo "ERROR: JSON adapter dependency set is incorrect." >&2
        printf '%s\n' "${dependency_ids}" >&2
        exit 1
      fi

      core_dependency_version="$(read_dependency_version "${nuspec_path}" "MackySoft.Text.Vocabularies")"
      if [[ "${core_dependency_version}" != "${package_family_version}" ]]; then
        echo "ERROR: JSON adapter must depend on MackySoft.Text.Vocabularies ${package_family_version}." >&2
        exit 1
      fi
      ;;
  esac
done

consumer_dir="${temp_dir}/consumer"
export DOTNET_CLI_HOME="${temp_dir}/dotnet-home"
export NUGET_PACKAGES="${temp_dir}/nuget-packages"
mkdir -p "${DOTNET_CLI_HOME}" "${NUGET_PACKAGES}"

dotnet new console --framework net10.0 --output "${consumer_dir}" --no-restore >/dev/null
PACKAGE_VERSION="${package_family_version}" perl -0pi -e '
  my $version = $ENV{"PACKAGE_VERSION"};
  s{</Project>}{  <ItemGroup>\n    <PackageReference Include="MackySoft.Text.Vocabularies.Json" Version="$version" />\n  </ItemGroup>\n</Project>};
' "${consumer_dir}/consumer.csproj"

cat > "${consumer_dir}/Program.cs" <<'CS'
using System.Text.Json;
using MackySoft.Text.Vocabularies;
using MackySoft.Text.Vocabularies.Json;

var options = new JsonSerializerOptions();
options.Converters.Add(new VocabularyJsonConverterFactory());

string json = JsonSerializer.Serialize(ConsumerState.Ready, options);
ConsumerState value = JsonSerializer.Deserialize<ConsumerState>(json, options);
var keyed = new Dictionary<ConsumerState, int> { [ConsumerState.Ready] = 1 };
string keyedJson = JsonSerializer.Serialize(keyed, options);

if (json != "\"ready\"" || value != ConsumerState.Ready || keyedJson != "{\"ready\":1}")
{
    throw new InvalidOperationException("Package consumer observed an unexpected vocabulary JSON contract.");
}

[VocabularyDefinition]
enum ConsumerState
{
    [VocabularyText("ready")]
    Ready,
}
CS

dotnet restore "${consumer_dir}/consumer.csproj" \
  --source "${package_dir}" \
  --source https://api.nuget.org/v3/index.json
dotnet run \
  --project "${consumer_dir}/consumer.csproj" \
  --configuration Release \
  --no-restore

echo "Verified ${package_family_name} ${package_family_version}: ${package_dir}"
