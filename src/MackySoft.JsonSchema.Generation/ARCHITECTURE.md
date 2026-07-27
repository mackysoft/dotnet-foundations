# Architecture

`MackySoft.JsonSchema.Generation` is one independently releasable package with one
public semantic pipeline:

```text
System.Text.Json type contract + annotations + registered extensions
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
- `Annotations/` owns product-independent declarations that a metadata resolver
  can translate into model facts.
- `Metadata/` owns declarative metadata values passed across the public extension
  boundary.
- `Extensibility/` owns the four explicit extension contracts and their contexts:
  metadata providers, type mappers, model contributors, and delivery-only schema
  vendor-extension processors. Its public type-mapping factory also constructs
  finite string mappings from core `MackySoft.Text.Vocabularies`
  declarations; a consumer mapper remains responsible for recognizing the
  serializer adapter.
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
  |          +--> Metadata resolution --> Annotations
  |          |               |
  |          |               +------------> Metadata values
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

- `Internal/ModelBuilding/` is the only code allowed to interpret
  `JsonTypeInfo`, CLR nullability, converters, and type mappings.
- `Internal/ModelBuilding/Definitions/` owns deterministic definition
  registration and pending traversal state. `TypeMappings/` owns mapper
  selection and validates mapped shapes. `Shapes/` owns the shared
  structural baseline without exposing traversal or reflection; that baseline
  may come from serializer metadata or an explicitly mapped surrogate.
  `TypeSystem/` owns CLR scalar, collection, and nullability interpretation,
  and verifies how a text-vocabulary type participates in the authoritative
  serializer contract. It does not construct the public finite vocabulary
  mapping.
- `Internal/ModelBuilding/SerializerMetadata/` resolves serialized members from
  authoritative `JsonTypeInfo` without owning graph traversal.
- `Internal/ModelBuilding/Contributions/` owns the semantic target index shared
  by public contributor navigation and contribution validation. Contributors
  receive context-scoped targets and do not reproduce the model pointer grammar.
- `Internal/ModelBuilding/Validation/` validates completed serializer-derived
  shapes against declarative constraints and finite JSON values. `Variants/`
  owns property-set `oneOf` composition and the interpretation of
  `System.Text.Json` polymorphism and discriminator registrations.
- `Internal/Metadata/Declarations/` takes finite snapshots from attributes and
  providers. `Normalization/` canonicalizes declared JSON values, and
  `Validation/` resolves conflicts and cross-metadata invariants.
  `Contracts/` owns the resolved values, target context, and classified failure
  helpers shared by those components. The metadata facade coordinates them but
  does not construct model nodes or documents.
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
