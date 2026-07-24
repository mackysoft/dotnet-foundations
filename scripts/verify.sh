#!/usr/bin/env bash
set -euo pipefail

repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
solution_path="${repository_root}/DotNetFoundations.slnx"

dotnet restore "${solution_path}"
dotnet format "${solution_path}" --no-restore --verify-no-changes
dotnet build "${solution_path}" --configuration Release --no-restore
dotnet test "${solution_path}" --configuration Release --no-build --no-restore
