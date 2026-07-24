#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat >&2 <<'EOF'
Usage: scripts/resolve-package-family.sh --family <family> --version <version>

Validates one independently versioned package family and exposes its release identity.
EOF
}

family=""
version=""

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

if [[ -z "${family}" || -z "${version}" ]]; then
  usage
  exit 2
fi

repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
source "${repository_root}/scripts/package-family-common.sh"

resolve_package_family "${family}"
validate_package_family_version "${version}"

tag_name="${package_family_name}/${package_family_version}"
package_ids="$(printf '%s\n' "${package_family_ids[@]}")"

if [[ -n "${GITHUB_OUTPUT:-}" ]]; then
  {
    echo "family=${package_family_name}"
    echo "version=${package_family_version}"
    echo "tag_name=${tag_name}"
    echo "package_ids<<PACKAGE_IDS"
    printf '%s\n' "${package_family_ids[@]}"
    echo "PACKAGE_IDS"
  } >> "${GITHUB_OUTPUT}"
else
  printf 'family=%s\nversion=%s\ntag_name=%s\npackage_ids=\n%s\n' \
    "${package_family_name}" \
    "${package_family_version}" \
    "${tag_name}" \
    "${package_ids}"
fi
