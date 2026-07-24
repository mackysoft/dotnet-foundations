#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat >&2 <<'EOF'
Usage: scripts/pack-package-family.sh --family <family> --version <version> --output <directory>

Packs only the projects owned by one independently versioned package family.
EOF
}

family=""
version=""
output_dir=""

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
    --output)
      [[ $# -ge 2 ]] || { usage; exit 2; }
      output_dir="$2"
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

if [[ -z "${family}" || -z "${version}" || -z "${output_dir}" ]]; then
  usage
  exit 2
fi

repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
source "${repository_root}/scripts/package-family-common.sh"

resolve_package_family "${family}"
validate_package_family_version "${version}"

mkdir -p "${output_dir}"
output_dir="$(cd "${output_dir}" && pwd)"

for package_id in "${package_family_ids[@]}"; do
  package_path="${output_dir}/${package_id}.${package_family_version}.nupkg"
  if [[ -e "${package_path}" ]]; then
    echo "ERROR: Package output already exists: ${package_path}" >&2
    exit 1
  fi
done

dotnet restore "${repository_root}/DotNetFoundations.slnx"

for project_path in "${package_family_projects[@]}"; do
  dotnet pack "${repository_root}/${project_path}" \
    --configuration Release \
    --no-restore \
    "-p:${package_family_version_property}=${package_family_version}" \
    --output "${output_dir}"
done

echo "Packed ${package_family_name} ${package_family_version}: ${output_dir}"
