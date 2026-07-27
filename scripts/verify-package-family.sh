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

  required_entries=(
    "${nuspec_entry}"
    README.md
    LICENSE
    "lib/netstandard2.1/${package_id}.dll"
  )
  if [[ "${package_id}" == "MackySoft.FileSystem" ]]; then
    required_entries+=("lib/net8.0/${package_id}.dll")
  fi
  if [[ "${package_id}" == "MackySoft.Json.Canonicalization" ]]; then
    required_entries+=(
      THIRD-PARTY-NOTICES.md
      licenses/Apache-2.0.txt
      licenses/MPL-2.0.txt
      "lib/netstandard2.1/${package_id}.xml"
    )
  fi
  if [[ "${package_id}" == "MackySoft.JsonSchema.Generation" ]]; then
    required_entries+=("lib/netstandard2.1/${package_id}.xml")
  fi

  for required_entry in "${required_entries[@]}"; do
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
    MackySoft.FileSystem)
      if [[ -n "${dependency_ids}" ]]; then
        echo "ERROR: Filesystem package must not declare package dependencies." >&2
        printf '%s\n' "${dependency_ids}" >&2
        exit 1
      fi
      ;;
    MackySoft.Json.Canonicalization)
      expected_dependencies="System.Text.Json"
      if [[ "${dependency_ids}" != "${expected_dependencies}" ]]; then
        echo "ERROR: JSON canonicalization dependency set is incorrect." >&2
        printf '%s\n' "${dependency_ids}" >&2
        exit 1
      fi

      third_party_notices="${temp_dir}/${package_id}.THIRD-PARTY-NOTICES.md"
      apache_license="${temp_dir}/${package_id}.Apache-2.0.txt"
      mpl_license="${temp_dir}/${package_id}.MPL-2.0.txt"
      unzip -p "${package_path}" THIRD-PARTY-NOTICES.md > "${third_party_notices}"
      unzip -p "${package_path}" licenses/Apache-2.0.txt > "${apache_license}"
      unzip -p "${package_path}" licenses/MPL-2.0.txt > "${mpl_license}"
      if ! grep -F "19d51d7fe467d4706a3ff08adf8a748f29fc21e0" "${third_party_notices}" >/dev/null \
        || ! grep -F "dotnet/es6numberserializer" "${third_party_notices}" >/dev/null \
        || ! grep -F "Copyright 2010 the V8 project authors" "${third_party_notices}" >/dev/null \
        || ! grep -F "Copyright (c) 1991, 2000, 2001 by Lucent Technologies" "${third_party_notices}" >/dev/null \
        || ! grep -F "Mozilla Public License files" "${third_party_notices}" >/dev/null \
        || ! grep -F "Copyright 2006-2018 WebPKI.org" "${third_party_notices}" >/dev/null; then
        echo "ERROR: JSON canonicalization third-party notice is incomplete." >&2
        exit 1
      fi
      if ! grep -F "Apache License" "${apache_license}" >/dev/null \
        || ! grep -F "Version 2.0, January 2004" "${apache_license}" >/dev/null \
        || ! grep -F "Mozilla Public License Version 2.0" "${mpl_license}" >/dev/null; then
        echo "ERROR: JSON canonicalization third-party license text is incomplete." >&2
        exit 1
      fi
      ;;
    MackySoft.JsonSchema.Generation)
      expected_dependencies=$'MackySoft.Json.Canonicalization\nMackySoft.Text.Vocabularies\nSystem.Text.Json'
      if [[ "${dependency_ids}" != "${expected_dependencies}" ]]; then
        echo "ERROR: JSON Schema generation dependency set is incorrect." >&2
        printf '%s\n' "${dependency_ids}" >&2
        exit 1
      fi

      canonicalization_dependency_version="$(
        read_dependency_version "${nuspec_path}" "MackySoft.Json.Canonicalization"
      )"
      if [[ "${canonicalization_dependency_version}" != "0.1.0" ]]; then
        echo "ERROR: JSON Schema generation must depend on MackySoft.Json.Canonicalization 0.1.0." >&2
        exit 1
      fi

      vocabulary_dependency_version="$(
        read_dependency_version "${nuspec_path}" "MackySoft.Text.Vocabularies"
      )"
      if [[ "${vocabulary_dependency_version}" != "0.1.0" ]]; then
        echo "ERROR: JSON Schema generation must depend on MackySoft.Text.Vocabularies 0.1.0." >&2
        exit 1
      fi

      system_text_json_dependency_version="$(
        read_dependency_version "${nuspec_path}" "System.Text.Json"
      )"
      if [[ "${system_text_json_dependency_version}" != "8.0.5" ]]; then
        echo "ERROR: JSON Schema generation must depend on System.Text.Json 8.0.5." >&2
        exit 1
      fi
      ;;
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

package_source_xml_value="$(
  PACKAGE_SOURCE_VALUE="${package_dir}" perl -e '
    my $value = $ENV{"PACKAGE_SOURCE_VALUE"};
    $value =~ s/&/&amp;/g;
    $value =~ s/"/&quot;/g;
    $value =~ s/</&lt;/g;
    $value =~ s/>/&gt;/g;
    print $value;
  '
)"
nuget_config="${temp_dir}/NuGet.config"
{
  printf '%s\n' '<?xml version="1.0" encoding="utf-8"?>'
  printf '%s\n' '<configuration>'
  printf '%s\n' '  <packageSources>'
  printf '%s\n' '    <clear />'
  printf '    <add key="package-family" value="%s" />\n' "${package_source_xml_value}"
  printf '%s\n' '    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />'
  printf '%s\n' '  </packageSources>'
  printf '%s\n' '  <packageSourceMapping>'
  printf '%s\n' '    <packageSource key="package-family">'
  for package_id in "${package_family_ids[@]}"; do
    printf '      <package pattern="%s" />\n' "${package_id}"
  done
  printf '%s\n' '    </packageSource>'
  printf '%s\n' '    <packageSource key="nuget.org">'
  printf '%s\n' '      <package pattern="Microsoft.*" />'
  if [[ "${package_family_name}" == "json-schema-generation" ]]; then
    printf '%s\n' '      <package pattern="MackySoft.Json.Canonicalization" />'
    printf '%s\n' '      <package pattern="MackySoft.Text.Vocabularies" />'
  fi
  printf '%s\n' '      <package pattern="NETStandard.Library" />'
  printf '%s\n' '      <package pattern="System.*" />'
  printf '%s\n' '      <package pattern="runtime.*" />'
  printf '%s\n' '    </packageSource>'
  printf '%s\n' '  </packageSourceMapping>'
  printf '%s\n' '</configuration>'
} > "${nuget_config}"

verify_restored_package_provenance() {
  local package_id
  local package_directory_name
  local package_metadata

  for package_id in "${package_family_ids[@]}"; do
    package_directory_name="$(
      tr '[:upper:]' '[:lower:]' <<< "${package_id}"
    )"
    package_metadata="$(
      printf '%s/%s/%s/.nupkg.metadata' \
        "${NUGET_PACKAGES}" \
        "${package_directory_name}" \
        "${package_family_version}"
    )"
    if [[ ! -f "${package_metadata}" ]] \
      || ! grep -F "\"source\": \"${package_dir}\"" "${package_metadata}" >/dev/null; then
      echo "ERROR: External consumer did not restore ${package_id} from the generated package directory." >&2
      exit 1
    fi
  done
}

case "${package_family_name}" in
  filesystem)
    dotnet new classlib \
      --name FileSystemPackageConsumer \
      --framework netstandard2.1 \
      --output "${consumer_dir}" \
      --no-restore \
      >/dev/null
    consumer_project_path="${consumer_dir}/FileSystemPackageConsumer.csproj"
    PACKAGE_VERSION="${package_family_version}" perl -0pi -e '
      my $version = $ENV{"PACKAGE_VERSION"};
      s{<TargetFramework>netstandard2\.1</TargetFramework>}{<TargetFrameworks>netstandard2.1;net8.0</TargetFrameworks>};
      s{</Project>}{  <ItemGroup>\n    <PackageReference Include="MackySoft.FileSystem" Version="[$version]" />\n  </ItemGroup>\n</Project>};
    ' "${consumer_project_path}"
    cat > "${consumer_dir}/Class1.cs" <<'CS'
using MackySoft.FileSystem;

namespace FileSystemPackageConsumer
{
    public static class GuardedPathConsumer
    {
        public static ContainedPath Resolve (string rootText, string pathText)
        {
            AbsolutePath root = AbsolutePath.Parse(rootText);
            RootRelativePath relativePath = RootRelativePath.Parse(pathText);
            return ContainedPath.Create(root, relativePath);
        }
    }
}
CS
    dotnet restore "${consumer_project_path}" \
      --no-cache \
      --force-evaluate \
      --configfile "${nuget_config}"
    verify_restored_package_provenance
    dotnet build \
      "${consumer_project_path}" \
      --configuration Release \
      --no-restore
    ;;
  json-canonicalization)
    dotnet new console --framework net10.0 --output "${consumer_dir}" --no-restore >/dev/null
    consumer_project_path="${consumer_dir}/consumer.csproj"
    PACKAGE_VERSION="${package_family_version}" perl -0pi -e '
      my $version = $ENV{"PACKAGE_VERSION"};
      s{</Project>}{  <ItemGroup>\n    <PackageReference Include="MackySoft.Json.Canonicalization" Version="[$version]" />\n  </ItemGroup>\n</Project>};
    ' "${consumer_project_path}"
    cat > "${consumer_dir}/Program.cs" <<'CS'
using System.Text;
using System.Text.Json;
using MackySoft.Json.Canonicalization;

byte[] rawJson = Encoding.UTF8.GetBytes(
    """{"b":1,"a":9007199254740993,"text":"€"}""");
byte[] canonicalJson = Rfc8785JsonCanonicalizer.Canonicalize(rawJson);
const string expected = """{"a":9007199254740992,"b":1,"text":"€"}""";

using JsonDocument document = JsonDocument.Parse(rawJson);
byte[] canonicalElement = Rfc8785JsonCanonicalizer.Canonicalize(document.RootElement);

if (Encoding.UTF8.GetString(canonicalJson) != expected
    || !canonicalJson.AsSpan().SequenceEqual(canonicalElement))
{
    throw new InvalidOperationException(
        "Package consumer observed unexpected RFC 8785 canonical bytes.");
}

try
{
    Rfc8785JsonCanonicalizer.Canonicalize(
        Encoding.UTF8.GetBytes("""{"duplicate":1,"duplicate":2}"""));
}
catch (JsonCanonicalizationException exception)
    when (exception.FailureKind == JsonCanonicalizationFailureKind.DuplicateProperty)
{
    return;
}

throw new InvalidOperationException(
    "Package consumer did not observe the expected duplicate-property failure.");
CS
    dotnet restore "${consumer_project_path}" \
      --no-cache \
      --force-evaluate \
      --configfile "${nuget_config}"
    verify_restored_package_provenance
    dotnet run \
      --project "${consumer_project_path}" \
      --configuration Release \
      --no-restore
    ;;
  json-schema-generation)
    dotnet new classlib \
      --name JsonSchemaGenerationPackageConsumer \
      --framework netstandard2.1 \
      --output "${consumer_dir}" \
      --no-restore \
      >/dev/null
    consumer_project_path="${consumer_dir}/JsonSchemaGenerationPackageConsumer.csproj"
    PACKAGE_VERSION="${package_family_version}" perl -0pi -e '
      my $version = $ENV{"PACKAGE_VERSION"};
      s{</Project>}{  <ItemGroup>\n    <PackageReference Include="MackySoft.JsonSchema.Generation" Version="[$version]" />\n  </ItemGroup>\n</Project>};
    ' "${consumer_project_path}"
    cat > "${consumer_dir}/Class1.cs" <<'CS'
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using MackySoft.JsonSchema.Generation;
using MackySoft.JsonSchema.Generation.Annotations;
using MackySoft.JsonSchema.Generation.Configuration;
using MackySoft.JsonSchema.Generation.ContractModel;
using MackySoft.JsonSchema.Generation.Extensibility;
using MackySoft.JsonSchema.Generation.Projection;

namespace JsonSchemaGenerationPackageConsumer
{
    public static class ContractModelConsumer
    {
        public static System.Type ContractModelType => typeof(JsonContractModel);

        public static JsonContractGenerationResult Generate()
        {
            var serializerOptions = new JsonSerializerOptions
            {
                UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            };
            var generator = new JsonContractGenerator(
                new JsonContractGeneratorOptions(
                    JsonContractGenerationSettings.ClosedObjects,
                    modelContributors: new IJsonContractModelContributor[]
                    {
                        new ConsumerModelContributor(),
                    }));
            return generator.Generate(
                new JsonContractGenerationRequest(
                    "package.consumer/example",
                    typeof(ExampleContract),
                    serializerOptions,
                    new DefaultJsonTypeInfoResolver(),
                    new JsonSchemaDocumentOptions(
                        JsonSchemaDocumentKind.Complete,
                        id: null,
                        logicalName: "example")));
        }

        public static void Verify()
        {
            JsonContractGenerationResult result = Generate();
            byte[] schemaUtf8 = result.GetJsonSchemaUtf8();
            byte[] metadataUtf8 = result.GetTypeMetadataUtf8();

            using JsonDocument schema = JsonDocument.Parse(schemaUtf8);
            using JsonDocument metadata = JsonDocument.Parse(metadataUtf8);
            JsonElement schemaRoot = schema.RootElement;
            JsonElement metadataRoot = metadata.RootElement;

            if (result.Model.ContractId != "package.consumer/example"
                || result.ContractDigest.Length != 64
                || !IsLowercaseHex(result.ContractDigest)
                || schemaRoot.GetProperty("$schema").GetString()
                    != JsonContractGenerationSettings.Draft202012Dialect
                || schemaRoot.GetProperty("x-contract-id").GetString()
                    != result.Model.ContractId
                || schemaRoot.GetProperty("x-contract-digest").GetString()
                    != result.ContractDigest
                || schemaRoot
                    .GetProperty("properties")
                    .GetProperty("Value")
                    .GetProperty("description")
                    .GetString()
                    != "Package consumer value."
                || metadataRoot.GetProperty("contractId").GetString()
                    != result.Model.ContractId
                || metadataRoot.GetProperty("contractDigest").GetString()
                    != result.ContractDigest
                || metadataRoot.GetProperty("schemaName").GetString()
                    != "example"
                || metadataRoot
                    .GetProperty("root")
                    .GetProperty("kind")
                    .GetString()
                    != "object"
                || metadataRoot
                    .GetProperty("contributions")
                    .GetArrayLength()
                    != 1
                || metadataRoot
                    .GetProperty("contributions")[0]
                    .GetProperty("targetPointer")
                    .GetString()
                    != "/root/properties/0/value")
            {
                throw new InvalidOperationException(
                    "Package consumer observed inconsistent contract generation outputs.");
            }
        }

        private static bool IsLowercaseHex(string value)
        {
            foreach (char character in value)
            {
                if (!((character >= '0' && character <= '9')
                    || (character >= 'a' && character <= 'f')))
                {
                    return false;
                }
            }

            return true;
        }
    }

    public sealed class ExampleContract
    {
        [JsonContractDescription("Package consumer value.")]
        public string Value { get; set; } = string.Empty;
    }

    public sealed class ConsumerModelContributor : IJsonContractModelContributor
    {
        public string StableId => "package.consumer.contributor";

        public string ContractVersion => "1";

        public IReadOnlyList<JsonContractModelContribution> GetContributions(
            JsonContractModelContext context)
        {
            JsonContractModelTarget target =
                context.GetTarget(context.Root.Properties[0].Value);
            return new[]
            {
                new JsonContractModelContribution(
                    target,
                    "consumerHint",
                    JsonSerializer.SerializeToElement(true),
                    StableId),
            };
        }
    }
}
CS
    dotnet restore "${consumer_project_path}" \
      --no-cache \
      --force-evaluate \
      --configfile "${nuget_config}"
    verify_restored_package_provenance
    dotnet build \
      "${consumer_project_path}" \
      --configuration Release \
      --no-restore

    runnable_host_dir="${temp_dir}/runnable-host"
    dotnet new console \
      --name JsonSchemaGenerationPackageConsumerHost \
      --framework net10.0 \
      --output "${runnable_host_dir}" \
      --no-restore \
      >/dev/null
    runnable_host_project_path="${runnable_host_dir}/JsonSchemaGenerationPackageConsumerHost.csproj"
    PACKAGE_VERSION="${package_family_version}" perl -0pi -e '
      my $version = $ENV{"PACKAGE_VERSION"};
      s{</Project>}{  <ItemGroup>\n    <ProjectReference Include="../consumer/JsonSchemaGenerationPackageConsumer.csproj" />\n    <PackageReference Include="MackySoft.JsonSchema.Generation" Version="[$version]" />\n  </ItemGroup>\n</Project>};
    ' "${runnable_host_project_path}"
    cat > "${runnable_host_dir}/Program.cs" <<'CS'
using JsonSchemaGenerationPackageConsumer;

ContractModelConsumer.Verify();
CS
    dotnet restore "${runnable_host_project_path}" \
      --no-cache \
      --force-evaluate \
      --configfile "${nuget_config}"
    verify_restored_package_provenance
    dotnet run \
      --project "${runnable_host_project_path}" \
      --configuration Release \
      --no-restore
    ;;
  text-vocabularies)
    dotnet new console --framework net10.0 --output "${consumer_dir}" --no-restore >/dev/null
    consumer_project_path="${consumer_dir}/consumer.csproj"
    PACKAGE_VERSION="${package_family_version}" perl -0pi -e '
      my $version = $ENV{"PACKAGE_VERSION"};
      s{</Project>}{  <ItemGroup>\n    <PackageReference Include="MackySoft.Text.Vocabularies.Json" Version="[$version]" />\n  </ItemGroup>\n</Project>};
    ' "${consumer_project_path}"

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
    dotnet restore "${consumer_project_path}" \
      --no-cache \
      --force-evaluate \
      --configfile "${nuget_config}"
    verify_restored_package_provenance
    dotnet run \
      --project "${consumer_project_path}" \
      --configuration Release \
      --no-restore
    ;;
  *)
    echo "ERROR: Package consumer is not configured for ${package_family_name}." >&2
    exit 1
    ;;
esac

echo "Verified ${package_family_name} ${package_family_version}: ${package_dir}"
