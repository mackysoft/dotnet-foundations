# AGENTS.md

## Repository responsibility

- This repository owns product-independent .NET foundations shared by MackySoft products.
- Keep each NuGet package family independently versioned and independently releasable.
- Do not add product-specific input policies, error contracts, schemas, or compatibility layers.

## Text vocabularies

- `MackySoft.Text.Vocabularies` owns finite typed vocabulary declaration, validation, exact resolution, and deterministic enumeration.
- `MackySoft.Text.Vocabularies.Json` owns only the `System.Text.Json` adapter.
- The two packages form the `text-vocabularies` release family and must use the same version.

## JSON canonicalization

- `MackySoft.Json.Canonicalization` owns product-independent RFC 8785 canonical UTF-8 JSON generation and classified input failures.
- The package forms the independently versioned `json-canonicalization` family. Its initial version is `0.1.0`.
- Keep hash and digest APIs, product-specific projections and validation, JSON Schema generation, artifact storage, and Unity distribution outside this family.

## Validation

```bash
bash scripts/verify.sh

bash scripts/pack-package-family.sh \
  --family text-vocabularies \
  --version 0.1.0 \
  --output artifacts/packages/text-vocabularies

bash scripts/verify-package-family.sh \
  --family text-vocabularies \
  --version 0.1.0 \
  --package-dir artifacts/packages/text-vocabularies

bash scripts/pack-package-family.sh \
  --family json-canonicalization \
  --version 0.1.0 \
  --output artifacts/packages/json-canonicalization

bash scripts/verify-package-family.sh \
  --family json-canonicalization \
  --version 0.1.0 \
  --package-dir artifacts/packages/json-canonicalization
```

## Release

- Package publication is performed only by `.github/workflows/package-publish.yaml`.
- The workflow requires an explicit package family and version.
- Do not publish packages, create releases, or merge release pull requests without explicit user authorization.
