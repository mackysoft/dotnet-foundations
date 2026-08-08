# MackySoft .NET Foundations

[![verify](https://github.com/mackysoft/dotnet-foundations/actions/workflows/verify.yaml/badge.svg)](https://github.com/mackysoft/dotnet-foundations/actions/workflows/verify.yaml) [![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

This repository owns product-independent .NET foundations shared by MackySoft products. NuGet packages are separated by responsibility, and each package family has its own version and release lifecycle. The repository itself does not have a single package version.

## Package families

| Family | Package | Version | Responsibility |
| --- | --- | --- | --- |
| `filesystem` | [`MackySoft.FileSystem`](src/MackySoft.FileSystem/README.md) | `0.2.1` | Guarded lexical path values using current-platform identity rules. |
| `filesystem` | [`MackySoft.FileSystem.Physical`](src/MackySoft.FileSystem.Physical/README.md) | `0.2.1` | Physical entry observation, link-policy path resolution, and complete single-file publication. |
| `json-canonicalization` | [`MackySoft.Json.Canonicalization`](src/MackySoft.Json.Canonicalization/README.md) | `0.1.0` | RFC 8785 canonical UTF-8 JSON generation. |
| `json-schema-generation` | [`MackySoft.JsonSchema.Generation`](src/MackySoft.JsonSchema.Generation/README.md) | `0.3.1` | Deterministic JSON contract modeling, JSON Schema generation, and type-metadata projection. |
| `text-vocabularies` | [`MackySoft.Text.Vocabularies`](src/MackySoft.Text.Vocabularies/README.md) | `0.1.0` | Finite typed vocabularies with exact canonical text mappings. |
| `text-vocabularies` | [`MackySoft.Text.Vocabularies.Json`](src/MackySoft.Text.Vocabularies.Json/README.md) | `0.1.0` | `System.Text.Json` string value and property-name adapters. |

The packages in the `filesystem` and `text-vocabularies` families are released together at the same version. The `json-canonicalization` and `json-schema-generation` families are versioned and released independently.

## Filesystem boundary

`MackySoft.FileSystem` validates raw path text once at a factory boundary, then carries normalized absolute paths, root-relative paths, and proven lexical containment through immutable guarded values. Separator, root, fully-qualified, casing, equality, and containment behavior follow the operating system running the process.

The package does not access the filesystem or guarantee existence, node kind, permissions, symbolic-link behavior, physical containment, or the case sensitivity of a mounted volume. It contains no file I/O, locking, access-control, repository layout, cache, transport, or product-specific path policy.

`MackySoft.FileSystem.Physical` consumes those guarded values at an operating-system boundary. It classifies current entry state without following the final link, resolves a `ContainedPath` with explicit link and missing-tail policies, verifies link-resolved containment using current-platform lexical identity rules, and publishes one complete file through a same-directory temporary sibling.

Physical observations and resolutions are snapshots rather than persistent proofs. The package reports directly detected resolved-path or required-entry changes but does not reserve path segments, inspect per-volume case-sensitivity overrides, guarantee provider-independent atomic visibility, define product storage layouts or required files, preserve access-control metadata, coordinate multi-file transactions, or provide a general-purpose filesystem interface.

## JSON canonicalization boundary

`MackySoft.Json.Canonicalization` produces canonical UTF-8 bytes for JSON values according to RFC 8785. It owns strict JSON input validation, binary64 number interpretation, deterministic serialization, and classified canonicalization failures.

The package does not define hash or digest APIs, product-specific projections or validation, JSON Schema generation, artifact storage, or Unity distribution.

## JSON Schema generation boundary

`MackySoft.JsonSchema.Generation` builds a read-only JSON contract model from one effective `System.Text.Json` `JsonTypeInfo`, six contract-independent annotations, and explicitly registered typed extensions. It deterministically projects the same model to JSON Schema Draft 2020-12 and describe-oriented type metadata. The same authoritative input and settings produce the same normalized model, contract digest, and UTF-8 JSON bytes; ambiguous or conflicting input produces a typed failure.

Closed contractual enum-to-string literals use `MackySoft.Text.Vocabularies`; the JSON adapter is not part of this package's dependency boundary. Public extension points are limited to typed metadata providers, typed consumer-attribute interpreters, type mappers, model contributors, and document post-processors that add delivery-specific vendor extensions. The package does not own product-specific concepts, schema placement, manifests, CLI behavior, operation execution semantics, or unrestricted document post-processing.

## Text vocabulary boundary

The core package validates one-to-one mappings between a finite set of typed values and canonical texts. Resolution uses ordinal comparison. Definitions reject missing members, empty definitions, duplicate values, duplicate texts, empty or whitespace-only texts, and leading or trailing whitespace.

The JSON adapter delegates discovery and validation to the core package. It reads and writes vocabulary values as JSON strings and property names, checks JSON token kinds, and reports JSON data failures as `JsonException`.

The packages do not trim, ignore case, resolve aliases, normalize Unicode, describe values, generate JSON Schema, implement general text codecs, or canonicalize JSON documents.

## Development

Run the source verification:

```bash
bash scripts/verify.sh
```

Pack and verify one package family:

```bash
bash scripts/pack-package-family.sh \
  --family filesystem \
  --version 0.2.1 \
  --output artifacts/filesystem

bash scripts/verify-package-family.sh \
  --family filesystem \
  --version 0.2.1 \
  --package-dir artifacts/filesystem

bash scripts/pack-package-family.sh \
  --family text-vocabularies \
  --version 0.1.0 \
  --output artifacts/packages/text-vocabularies

bash scripts/verify-package-family.sh \
  --family text-vocabularies \
  --version 0.1.0 \
  --package-dir artifacts/packages/text-vocabularies
```

To pack and verify the JSON canonicalization family:

```bash
bash scripts/pack-package-family.sh \
  --family json-canonicalization \
  --version 0.1.0 \
  --output artifacts/packages/json-canonicalization

bash scripts/verify-package-family.sh \
  --family json-canonicalization \
  --version 0.1.0 \
  --package-dir artifacts/packages/json-canonicalization
```

To pack and verify the JSON Schema generation family:

```bash
bash scripts/pack-package-family.sh \
  --family json-schema-generation \
  --version 0.3.1 \
  --output artifacts/packages/json-schema-generation

bash scripts/verify-package-family.sh \
  --family json-schema-generation \
  --version 0.3.1 \
  --package-dir artifacts/packages/json-schema-generation
```

The package consumer verification restores from the generated local packages and builds a separate application. It therefore checks the NuGet dependency boundary rather than project-reference behavior.

## Release

`.github/workflows/package-publish.yaml` accepts an explicit package family and version. It validates, packs, and publishes only that family. Package publication uses NuGet Trusted Publishing and does not use a long-lived NuGet API key.

## License

This repository is licensed under the [MIT License](LICENSE).
