# AGENTS.md

## Repository responsibility

- This repository owns product-independent .NET foundations shared by MackySoft products.
- Keep each NuGet package family independently versioned and independently releasable.
- Do not add product-specific input policies, error contracts, schemas, or compatibility layers.

## Text vocabularies

- `MackySoft.Text.Vocabularies` owns finite typed vocabulary declaration, validation, exact resolution, and deterministic enumeration.
- `MackySoft.Text.Vocabularies.Json` owns only the `System.Text.Json` adapter.
- The two packages form the `text-vocabularies` release family and must use the same version.

## Filesystem paths

- `MackySoft.FileSystem` owns immutable guarded values for normalized absolute paths, root-relative paths, and lexical containment under the running operating system's path rules.
- Raw path text is accepted only at factory boundaries. Typed combination and derivation must not return to a raw parser.
- The package does not own file I/O, mutable physical state, symbolic-link guarantees, locks, access control, storage layouts, transports, or product-specific path policies.
- `MackySoft.FileSystem` forms the independently versioned `filesystem` release family.

## JSON canonicalization

- `MackySoft.Json.Canonicalization` owns product-independent RFC 8785 canonical UTF-8 JSON generation and classified input failures.
- The package forms the independently versioned `json-canonicalization` family. Its initial version is `0.1.0`.
- Keep hash and digest APIs, product-specific projections and validation, JSON Schema generation, artifact storage, and Unity distribution outside this family.

## JSON Schema generation

- `MackySoft.JsonSchema.Generation` owns the read-only JSON contract model built from .NET DTOs, shared annotations, `System.Text.Json` type information, and explicitly registered extensions.
- The package deterministically projects that model to JSON Schema Draft 2020-12 and describe-oriented type metadata.
- The same authoritative input and settings must produce the same normalized model, contract digest, and UTF-8 JSON bytes. Ambiguous converters, conflicting annotations or extensions, and duplicate stable identifiers must produce typed failures instead of inferred behavior.
- Closed contractual enum-to-string literals must use `MackySoft.Text.Vocabularies`; do not add a package-local manual codec or depend on `MackySoft.Text.Vocabularies.Json`.
- Public extension points are limited to metadata providers, type mappers, model contributors, and document post-processors that add delivery-specific vendor extensions.
- Keep uCLI and dotmet concepts, schema placement, manifests, CLI behavior, operation execution semantics, and unrestricted post-processing outside this family.
- The package forms the independently versioned `json-schema-generation` family. Its initial version is `0.1.0`.

## Test responsibility

- Automated tests verify observable package behavior and public API contracts.
- Do not use test projects to inspect repository layout, project files, package metadata, workflow files, dependency direction, or source placement.
- Project references and package dependencies are declared by project files. Final package contents, dependency closure, and isolated consumer installation are verified by the package verification scripts and CI.

## Validation

```bash
bash scripts/verify.sh

bash scripts/pack-package-family.sh \
  --family filesystem \
  --version 0.1.0 \
  --output artifacts/filesystem

bash scripts/verify-package-family.sh \
  --family filesystem \
  --version 0.1.0 \
  --package-dir artifacts/filesystem

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

bash scripts/pack-package-family.sh \
  --family json-schema-generation \
  --version 0.2.0 \
  --output artifacts/packages/json-schema-generation

bash scripts/verify-package-family.sh \
  --family json-schema-generation \
  --version 0.2.0 \
  --package-dir artifacts/packages/json-schema-generation
```

## Release

- Package publication is performed only by `.github/workflows/package-publish.yaml`.
- The workflow requires an explicit package family and version.
- Do not publish packages, create releases, or merge release pull requests without explicit user authorization.
