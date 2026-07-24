# AGENTS.md

## Repository responsibility

- This repository owns product-independent .NET foundations shared by MackySoft products.
- Keep each NuGet package family independently versioned and independently releasable.
- Do not add product-specific input policies, error contracts, schemas, or compatibility layers.

## Text vocabularies

- `MackySoft.Text.Vocabularies` owns finite typed vocabulary declaration, validation, exact resolution, and deterministic enumeration.
- `MackySoft.Text.Vocabularies.Json` owns only the `System.Text.Json` adapter.
- The two packages form the `text-vocabularies` release family and must use the same version.

## Validation

```bash
bash scripts/verify.sh

bash scripts/pack-package-family.sh \
  --family text-vocabularies \
  --version 0.1.0 \
  --output artifacts/packages

bash scripts/verify-package-family.sh \
  --family text-vocabularies \
  --version 0.1.0 \
  --package-dir artifacts/packages
```

## Release

- Package publication is performed only by `.github/workflows/package-publish.yaml`.
- The workflow requires an explicit package family and version.
- Do not publish packages, create releases, or merge release pull requests without explicit user authorization.
