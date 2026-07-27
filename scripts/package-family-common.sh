#!/usr/bin/env bash

resolve_package_family() {
  local requested_family="$1"

  case "${requested_family}" in
    filesystem)
      package_family_name="filesystem"
      package_family_version="$(
        sed -nE 's#.*<FileSystemVersion[^>]*>([^<]+)</FileSystemVersion>.*#\1#p' \
          "${repository_root}/eng/package-families/filesystem.props" |
          head -n 1
      )"
      package_family_version_property="FileSystemVersion"
      package_family_ids=(
        "MackySoft.FileSystem"
      )
      package_family_projects=(
        "src/MackySoft.FileSystem/MackySoft.FileSystem.csproj"
      )
      ;;
    json-canonicalization)
      package_family_name="json-canonicalization"
      package_family_version="$(
        sed -nE 's#.*<JsonCanonicalizationVersion[^>]*>([^<]+)</JsonCanonicalizationVersion>.*#\1#p' \
          "${repository_root}/eng/package-families/json-canonicalization.props" |
          head -n 1
      )"
      package_family_version_property="JsonCanonicalizationVersion"
      package_family_ids=(
        "MackySoft.Json.Canonicalization"
      )
      package_family_projects=(
        "src/MackySoft.Json.Canonicalization/MackySoft.Json.Canonicalization.csproj"
      )
      ;;
    json-schema-generation)
      package_family_name="json-schema-generation"
      package_family_version="$(
        sed -nE 's#.*<JsonSchemaGenerationVersion[^>]*>([^<]+)</JsonSchemaGenerationVersion>.*#\1#p' \
          "${repository_root}/eng/package-families/json-schema-generation.props" |
          head -n 1
      )"
      package_family_version_property="JsonSchemaGenerationVersion"
      package_family_ids=(
        "MackySoft.JsonSchema.Generation"
      )
      package_family_projects=(
        "src/MackySoft.JsonSchema.Generation/MackySoft.JsonSchema.Generation.csproj"
      )
      ;;
    text-vocabularies)
      package_family_name="text-vocabularies"
      package_family_version="$(
        sed -nE 's#.*<TextVocabulariesVersion[^>]*>([^<]+)</TextVocabulariesVersion>.*#\1#p' \
          "${repository_root}/eng/package-families/text-vocabularies.props" |
          head -n 1
      )"
      package_family_version_property="TextVocabulariesVersion"
      package_family_ids=(
        "MackySoft.Text.Vocabularies"
        "MackySoft.Text.Vocabularies.Json"
      )
      package_family_projects=(
        "src/MackySoft.Text.Vocabularies/MackySoft.Text.Vocabularies.csproj"
        "src/MackySoft.Text.Vocabularies.Json/MackySoft.Text.Vocabularies.Json.csproj"
      )
      ;;
    *)
      echo "ERROR: Unsupported package family: ${requested_family}" >&2
      return 1
      ;;
  esac

  if [[ -z "${package_family_version}" ]]; then
    echo "ERROR: Failed to resolve the configured version for ${package_family_name}." >&2
    return 1
  fi
}

validate_package_family_version() {
  local requested_version="$1"

  if [[ ! "${requested_version}" =~ ^[0-9]+\.[0-9]+\.[0-9]+(-[0-9A-Za-z.-]+)?$ ]]; then
    echo "ERROR: Package version must use SemVer format. Actual: ${requested_version}" >&2
    return 1
  fi

  if [[ "${requested_version}" != "${package_family_version}" ]]; then
    echo "ERROR: ${package_family_name} is configured for ${package_family_version}, not ${requested_version}." >&2
    return 1
  fi
}
