# Architecture

`MackySoft.JsonSchema.Generation` is one independently releasable package with
one public semantic pipeline:

```text
effective JsonTypeInfo + typed annotations/extensions
                         |
                         v
                immutable Contract Model
                  /                 \
                 v                   v
     JSON Schema Draft 2020-12   describe metadata
```

The package stays in one assembly because every public capability participates in
that pipeline and shares the same versioned contract. Directories identify owners;
they are not layers that may reach through one another.

## Public responsibility boundaries

- `Generation/` owns request validation and orchestration. It is the only public
  entry point that coordinates all other responsibilities.
- `Configuration/` owns semantic generation policies shared by model building,
  digest calculation, and projection.
- `ContractModel/` owns the immutable, serializer-independent semantic model. It
  does not inspect CLR types or emit documents.
- `Annotations/` owns the six product-independent annotations and constraints
  that are absent from an STJ contract: title, description, pattern, length,
  item count, and property count.
- `Metadata/` owns typed provider/interpreter contexts, scoped typed builders,
  and exact numeric-bound values. It does not expose a metadata-kind
  pseudo-union or raw JSON values.
- `Extensibility/` owns typed metadata providers, typed consumer-attribute
  interpreters, type mappers, model contributors, and delivery-only schema
  vendor-extension processors. A metadata registry fixes both the attribute
  and value type before generation.
- `Projection/` owns public artifact options. Artifact identity such as `$id` and
  logical name is deliberately separate from semantic generation settings.
- `Diagnostics/` owns classified failures and their stable diagnostic context.

The facade types in `Generation/` use the package namespace
`MackySoft.JsonSchema.Generation`. Every other public directory has a matching
child namespace (`Configuration`, `ContractModel`, `Annotations`, `Metadata`,
`Extensibility`, `Projection`, or `Diagnostics`). This makes cross-responsibility
dependencies explicit in source and prevents the facade's dependency on internal
orchestration from creating a reverse dependency through shared public types.

## Internal dependency direction

```text
Generation facade
  |
  +--> ModelBuilding traversal --> Definitions
  |          |
  |          +--> SerializerMetadata
  |          +--> TypeMappings --> TypeSystem
  |          +--> Metadata resolution --> typed sinks
  |          |               |
  |          |               +------------> Annotations
  |          |
  |          +--------------------------------> ContractModel
  |          +--------------------------------> Extensibility
  |
  +--> Digest --------------> Semantic writer --> ContractModel
  |
  +--> Projection ----------> ContractModel
        |
        +--------------------> Configuration
```

- `Internal/ModelBuilding/` owns traversal from the exact root `JsonTypeInfo`.
  The root instance is never re-resolved; child contracts come from its
  `JsonSerializerOptions`. Model building interprets CLR nullability,
  converters, mappings, and STJ polymorphism.
- `Internal/ModelBuilding/Definitions/` owns deterministic definition
  registration and pending traversal state. `TypeMappings/` owns mapper
  selection, validates mapped shapes, and derives a text vocabulary's canonical
  strings from the mapped type and effective converter. `Shapes/` owns the
  shared structural baseline without exposing traversal or reflection; that
  baseline may come from serializer metadata or an explicitly mapped
  surrogate. `TypeSystem/` owns CLR scalar, collection, nullability, and
  vocabulary-declaration recognition. A mapper cannot submit another
  vocabulary type or a manual value array.
- `Internal/ModelBuilding/SerializerMetadata/` resolves serialized members from
  authoritative `JsonTypeInfo` without owning graph traversal.
- `Internal/ModelBuilding/Contributions/` owns the semantic target index shared
  by public contributor navigation and contribution validation. Contributors
  receive context-scoped targets and do not reproduce the model pointer grammar.
- `Internal/ModelBuilding/Validation/` validates completed serializer-derived
  shapes against typed annotations, constraints, examples, and constants.
  `Variants/` interprets only `System.Text.Json` polymorphism and discriminator
  registrations. There is no property-set or manually declared `oneOf` path.
- `Internal/Metadata/Declarations/` invokes six built-in attributes and
  explicitly registered typed extensions into slot-specific declaration
  sinks. `Normalization/` preserves exact JSON-number semantics while
  deterministically ordering structured values. `Validation/` resolves
  per-slot conflicts and shape-independent invariants. Requiredness,
  nullability, arbitrary shape, finite vocabularies, and polymorphism never
  enter this declaration pipeline.
- `Internal/Determinism/Semantics/` owns the closed semantic representation;
  `Internal/Determinism/Digests/` owns canonical hashing. Shared code-point and
  JSON-value ordering primitives remain directly under `Internal/Determinism/`.
- `Internal/Projection/JsonSchema/` and
  `Internal/Projection/TypeMetadata/` consume only the completed Contract Model.
  They must not reflect over DTOs or reinterpret serializer metadata.
- `Internal/Projection/JsonSchema/JsonSchemaNodeWriter` owns Draft 2020-12 node
  shape and delegates the ordered validation keywords to
  `JsonSchemaConstraintWriter`. `VendorExtensions/` separately owns
  post-processor snapshots, JSON Pointer resolution, `x-` validation, conflict
  detection, and deterministic application.
- `Internal/Common/` contains only small defensive-copy primitives shared by
  immutable public values. Domain decisions do not belong there.

The dependency direction is represented by both physical directories and
responsibility-specific namespaces. The root facade may depend inward; internal
components do not depend back on the root facade. Tests verify observable
behavior and public contracts; they do not reconstruct these source-layout
rules. `.dotmet/rules.json` is the repository verification surface for these
namespace classifications and dependency directions.

## Explicit exclusions

Product naming, operation semantics, schema placement, manifests, CLI behavior,
artifact storage, and compatibility policy remain in consumers. Document
post-processors may add delivery-only `x-` properties and cannot mutate standard
JSON Schema structure.
