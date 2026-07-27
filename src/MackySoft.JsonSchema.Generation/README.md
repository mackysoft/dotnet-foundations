# MackySoft.JsonSchema.Generation

[![NuGet](https://img.shields.io/nuget/v/MackySoft.JsonSchema.Generation?label=MackySoft.JsonSchema.Generation)](https://www.nuget.org/packages/MackySoft.JsonSchema.Generation) [![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/mackysoft/dotnet-foundations/blob/master/LICENSE)

`MackySoft.JsonSchema.Generation` builds one immutable JSON Contract Model from a .NET DTO and its authoritative `System.Text.Json` metadata. It projects that model directly to JSON Schema Draft 2020-12 and structured type metadata.

The package targets `netstandard2.1`.

The source responsibility and dependency boundaries are documented in
[ARCHITECTURE.md](https://github.com/mackysoft/dotnet-foundations/blob/master/src/MackySoft.JsonSchema.Generation/ARCHITECTURE.md).

## Installation

Install an exact package version:

```bash
dotnet add package MackySoft.JsonSchema.Generation --version "[0.2.0]"
```

```xml
<PackageReference Include="MackySoft.JsonSchema.Generation" Version="[0.2.0]" />
```

If the runtime serializer writes a `MackySoft.Text.Vocabularies` enum as its
canonical text, the consumer must also reference
`MackySoft.Text.Vocabularies.Json` 0.1.0, register its converter, and register
a type mapper that recognizes that converter contract. The generation package
depends on the vocabulary declaration package, not on the serializer adapter.

## Generate a Contract

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using MackySoft.JsonSchema.Generation;
using MackySoft.JsonSchema.Generation.Configuration;
using MackySoft.JsonSchema.Generation.Projection;

var serializerOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
};

var generator = new JsonContractGenerator(
    new JsonContractGeneratorOptions(
        JsonContractGenerationSettings.ClosedObjects));

var request = new JsonContractGenerationRequest(
    contractId: "example.widget/v1",
    contractType: typeof(WidgetContract),
    serializerOptions,
    new DefaultJsonTypeInfoResolver(),
    new JsonSchemaDocumentOptions(
        JsonSchemaDocumentKind.Complete,
        id: "https://schemas.example.test/widget.schema.json",
        logicalName: "widget"));

JsonContractGenerationResult result = generator.Generate(request);
byte[] schemaUtf8 = result.GetJsonSchemaUtf8();
byte[] typeMetadataUtf8 = result.GetTypeMetadataUtf8();
string contractDigest = result.ContractDigest;
```

Pass the same `JsonSerializerOptions` behavior and resolver that the product uses at runtime. A source-generated `JsonSerializerContext` can be supplied as the resolver. The request captures an options snapshot when it is constructed, so later mutation of the caller's options is not observed; the explicit resolver argument replaces any resolver on those options. JSON property names, ignored members, property order, required members, collection shapes, and registered polymorphism come from the resolved `JsonTypeInfo`; the generator does not derive a competing JSON naming policy from CLR names.

`contractId` is a product-assigned semantic identifier. It must contain 1 through 256 characters and match `[A-Za-z0-9][A-Za-z0-9._:/@-]*`. It is not inferred from a CLR type, schema path, package version, or `$id`.

## Contract Metadata

Product-neutral attributes add metadata that becomes part of the model and both projections:

- `TitleAttribute`, `DescriptionAttribute`, and `ExampleAttribute`;
- `RequiredAttribute` and `AllowNullAttribute`;
- `ConstAttribute` and `EnumAttribute`;
- `RangeAttribute`, `LengthAttribute`, `PatternAttribute`, `ItemCountAttribute`, and `PropertyCountAttribute`;
- `OneOfBranchAttribute` and `DiscriminatorAttribute`;
- `AnyValueAttribute` for an explicitly unconstrained JSON value.

Metadata cannot silently override the serializer contract. An explicit required or nullability assertion that disagrees with the resolved CLR and `JsonTypeInfo` contract fails generation.
At type scope, `AllowNullAttribute` can assert that a reference-type
root accepts JSON `null`; it does not make a non-nullable serialized member of
that type nullable.

The attributes are declared in
`MackySoft.JsonSchema.Generation.Annotations`. A namespace alias keeps their
contract context explicit and avoids collisions with framework attributes:

```csharp
using System.Text.Json.Serialization;
using Contract = MackySoft.JsonSchema.Generation.Annotations;

sealed class WidgetContract
{
    [JsonRequired]
    [Contract.Required]
    [Contract.Description("The stable widget name.")]
    public string Name { get; set; } = string.Empty;
}
```

Version 0.2.0 removes the 0.1.0 `JsonContract*Attribute` type names. It does
not provide compatibility aliases; consumers must use the names listed above.
Attribute-backed diagnostic source IDs therefore use the new fully qualified
attribute type names.

Enums declared by `MackySoft.Text.Vocabularies` use their canonical vocabulary
texts only when an explicit type mapper recognizes the custom converter that
writes those texts. With the default numeric serializer contract, a
non-vocabulary CLR enum is represented by the range of its underlying integer
type, including undeclared and flags-combined values; it is not inferred as a
finite set of declared members. A closed enum-to-string contract must use
`MackySoft.Text.Vocabularies`, and its configured converter must emit the exact
canonical vocabulary texts. A custom numeric enum converter, a non-enum
converter that changes the wire representation, or a value object whose
converter replaces its object shape requires an explicit type mapper.

For a globally registered vocabulary adapter, the consumer can bind the
adapter instance and mapper explicitly:

```csharp
using System.Text.Json.Serialization;
using MackySoft.JsonSchema.Generation.Extensibility;
using MackySoft.Text.Vocabularies;
using MackySoft.Text.Vocabularies.Json;

var vocabularyConverter = new VocabularyJsonConverterFactory();
serializerOptions.Converters.Add(vocabularyConverter);

var generator = new JsonContractGenerator(
    new JsonContractGeneratorOptions(
        JsonContractGenerationSettings.ClosedObjects,
        typeMappers: new[]
        {
            new VocabularyTypeMapper(vocabularyConverter),
        }));

sealed class VocabularyTypeMapper(
    VocabularyJsonConverterFactory converter)
    : IJsonContractTypeMapper
{
    public string StableId => "example.text-vocabulary";

    public string ContractVersion => "1";

    public bool CanMap(JsonContractTypeMapperContext context)
    {
        return Vocabulary.IsVocabulary(context.TargetType)
            && IsVocabularyAdapter(context.EffectiveConverter);
    }

    public JsonContractTypeMapping Map(
        JsonContractTypeMapperContext context)
    {
        return JsonContractTypeMapping.TextVocabulary(context.TargetType);
    }

    private bool IsVocabularyAdapter(JsonConverter effectiveConverter)
    {
        return ReferenceEquals(effectiveConverter, converter)
            || effectiveConverter.GetType().DeclaringType
                == typeof(VocabularyJsonConverterFactory);
    }
}
```

The mapper must recognize the actual custom converter contract. A mapper cannot
replace a built-in `System.Text.Json` representation; for example, mapping a
default numeric enum to vocabulary strings fails as `UnsupportedConverter`.
`JsonContractTypeMapping.ContractType` can reuse a surrogate CLR type when a
recognized converter exposes that type's wire representation. The surrogate's
normalized serializer structure, annotations, and value constraints form the
baseline contract. Metadata on the mapped source can add examples and narrow
constraints, but a different title or description or a wider constraint fails
as typed metadata diagnostics. Null acceptance continues to come from the
mapped source and its use site.

The immutable model is exposed from
`MackySoft.JsonSchema.Generation.ContractModel`; declarative provider values are
exposed separately from `MackySoft.JsonSchema.Generation.Metadata`.

## Extension Boundary

Extensions are registered on `JsonContractGeneratorOptions`. Every extension has a stable ID and contract version.

The extension contracts are in
`MackySoft.JsonSchema.Generation.Extensibility`. Generation settings and
projection options are in the `Configuration` and `Projection` namespaces,
respectively. Typed failures are in `Diagnostics`.

- A metadata provider contributes declarative metadata before the model is fixed.
- A type mapper declares the JSON shape of an explicitly recognized custom converter.
- A model contributor inspects the fixed structure and returns product-specific projection metadata; it cannot mutate JSON structure.
- A document post-processor returns additions for delivery-only `x-` vendor properties. It cannot edit standard annotations or validation keywords.

When one or more document post-processors are registered, the schema root also
contains the built-in `x-document-post-processors` array. Each entry contains
the processor's `stableId` and `contractVersion`, ordered by stable ID. This
identity annotation and the declared vendor extensions affect the schema bytes
and therefore the delivery artifact SHA-256, but they do not affect
`contractDigest` or the type metadata bytes.

Model contributors obtain context-scoped `JsonContractModelTarget` values from
`JsonContractModelContext.ModelTarget`, `RootTarget`, or the `GetTarget`
overloads for nodes, properties, variants, and definitions.
`JsonContractModelContribution` accepts that typed target; contributors do not
construct semantic JSON Pointer strings. `JsonContractModelTarget.Pointer` is
the read-only pointer included in the model and type metadata projections.

Extensions of the same kind are evaluated by stable ID in Unicode code-point order. Duplicate IDs, multiple matching type mappers, unequal metadata for the same target, structural contribution attempts, and conflicting document additions fail instead of using registration order as precedence.

Each extension callback must return the same complete finite snapshot for the
same effective input, stable ID, and contract version. Extensions must not read
current time, network state, mutable ambient state, or unordered live
collections. Stable ordering by the generator establishes deterministic
evaluation; it is not a precedence rule for resolving different declarations.

## Type Metadata Projection in 0.2.0

`GetTypeMetadataUtf8()` returns UTF-8 JSON without a byte-order mark. The root
object contains these required properties in this order:

| Property | JSON value |
| --- | --- |
| `contractId` | Stable contract ID string |
| `contractDigest` | Lowercase SHA-256 contract digest string |
| `schemaName` | Product-owned logical name string, or `null` |
| `root` | Contract node object |
| `definitions` | Definition objects in `Model.Definitions` order |
| `contributions` | Contribution objects in `Model.Contributions` order |

Every node contains all of these properties in this order:
`kind`, `isNullable`, `scalarKind`, `annotations`, `constraints`, `constant`,
`allowedValues`, `referenceId`, `items`, `additionalProperties`, `properties`,
`variants`, and `discriminator`.

The nested object shapes are:

| Object | Required properties in emitted order |
| --- | --- |
| annotations | `title`, `description`, `examples` |
| constraints | `minimum`, `exclusiveMinimum`, `maximum`, `exclusiveMaximum`, `minimumLength`, `maximumLength`, `minimumItems`, `maximumItems`, `minimumProperties`, `maximumProperties`, `pattern`, `format` |
| property | `name`, `isRequired`, `value` |
| variant | `name`, `value`, `requiredProperties`, `discriminatorValue`, `annotations` |
| non-null discriminator | `propertyName` |
| definition | `id`, `value` |
| contribution | `targetPointer`, `name`, `value`, `sourceId` |

Every listed property is present. Optional scalar or child-object values are
written as JSON `null`; collection values are written as arrays even when
empty. `properties`, `variants`, `allowedValues`, `requiredProperties`,
`examples`, `definitions`, and `contributions` preserve the deterministic order
of their corresponding JSON Contract Model collections.

The 0.2.0 projection has no independent `formatVersion` property. Its
compatibility boundary is the exact `MackySoft.JsonSchema.Generation` NuGet
version. A consumer pinned to exact version 0.2.0 may rely on this shape.
Upgrading the package requires an explicit consumer review of metadata shape
and semantics; a tolerant JSON parser alone does not establish compatibility.

## Determinism and Digest

The model, JSON Schema bytes, and type metadata bytes are deterministic for the same request and generator options. JSON output is UTF-8 without a byte-order mark.

`contractDigest` is the lowercase SHA-256 of the RFC 8785 canonical bytes for a closed object containing:

- `model`: the complete semantic model projection, excluding CLR reflection objects; and
- `settings`: the dialect, nullability, object closure, reference projection, and stable identities of metadata providers, type mappers, and model contributors.

Before RFC 8785 canonicalization, JSON numbers in the semantic model are
represented losslessly by their normalized decimal text and number category.
Consequently, distinct integers such as `9007199254740992` and
`9007199254740993` produce distinct contract digests.

Schema `$id`, logical name, file path, product and generator versions, formatting, and document post-processors are artifact concerns and do not change `contractDigest`.

A fragment that contains local references is a complete schema resource root:
its `#/$defs/...` references resolve against the fragment itself. A consumer
that embeds it below another schema root must first establish an explicit JSON
Schema resource boundary; direct child insertion is not supported.

## Known Constraints

The supported `System.Text.Json` profile is deterministic and case-sensitive.
`PropertyNameCaseInsensitive` must be `false`, `ReferenceHandler` must be
`null`, and effective number handling must not read numbers from strings or
write them as strings. A required property cannot also be conditionally
omittable through its ignore policy or `ShouldSerialize`. Object closure must
match the effective serializer contract:
`DisallowAdditionalProperties` and `DisallowUnevaluatedProperties` require
`JsonUnmappedMemberHandling.Disallow`; `AllowAdditionalProperties` requires a
non-disallowing contract. Mismatches fail with a typed generation error.

JSON Schema's `integer` type is mathematical rather than lexical, so Draft
2020-12 validators can accept `1.0` and `1e0` as integers. Strict
`System.Text.Json` integer deserialization rejects those number spellings for
integral CLR types. Schema validation success therefore does not replace
deserialization with the authoritative serializer options. A consumer such as
uCLI must retain that deserialization step and must not treat schema acceptance
alone as runtime contract acceptance. The generator checks finite `const` and
`enum` declarations before numeric normalization: fractional or exponent
number spellings for integral CLR types and `decimal` values outside the CLR
range, such as `8e28`, fail as `InvalidMetadataValue`.

Built-in `System.Text.Json` polymorphism is accepted only as a finite,
fail-closed union. The base must be abstract or an interface, unknown derived
types and discriminator values must fail, every derived type must have a unique
string or integer discriminator, and each derived contract must be an object
whose properties do not collide with the discriminator. A concrete or fallback
base representation is not inferred as an additional branch.

`PatternAttribute` accepts the interoperable ECMA-262 token subset
recommended by JSON Schema Draft 2020-12: individual characters, simple and
range character classes, simple and range quantifiers (including lazy forms),
`^` and `$`, plain groups, alternation, and standard escaped characters.
.NET-only groups, inline options, named groups, backreferences, lookarounds,
and other constructs outside that subset fail as `InvalidMetadataValue`; the
package does not translate one regular-expression dialect into another.

`char` projects an exact one-BMP-scalar pattern. `Guid` projects a 36-character
canonical pattern plus the `uuid` format annotation. `DateTime`,
`DateTimeOffset`, `DateOnly`, `TimeOnly`, `TimeSpan`, `Uri`, `Version`, and
`byte[]` require an explicit type mapper because their `System.Text.Json`
lexical acceptance has no exact built-in projection. Without a mapper,
generation fails as `UnsupportedTypeInfo` instead of emitting a broader string
schema. Declared `const` and `enum` values for a mapped built-in lexical
converter must round-trip unchanged through that serializer contract.

RFC 8785 canonicalization uses the JSON number semantics of IEEE 754 binary64.
An attribute, metadata provider, model contributor, or document post-processor
value whose number would change during that canonicalization, such as the
integer `9007199254740993`, is rejected as `InvalidMetadataValue`,
`InvalidModelContribution`, or `InvalidDocumentExtension` instead of being
rounded silently. Type-mapper shapes and the internal contract-digest semantic
projection use lossless number encoding and continue to distinguish such model
numbers.

## Failures

`JsonContractGenerationException` exposes a `JsonContractGenerationFailureKind` and diagnostic context such as the contract ID, target CLR type, JSON property, metadata kind, and conflicting extension IDs. Classified failures include invalid or duplicate contract IDs, unsupported type information or converters, conflicting metadata, duplicate extension IDs, multiple matching type mappers, invalid model contributions, document extension conflicts, and digest failures.

## Responsibility Boundary

This package owns JSON structure normalization and projection. It does not own product schema names, paths, manifests, CLI commands, artifact storage, dynamic catalogs, semantic validation, permissions, side effects, or execution behavior.

## Repository and Support

Source, issues, and support are available in the [MackySoft .NET Foundations repository](https://github.com/mackysoft/dotnet-foundations).

## License

This package is under the [MIT License](https://github.com/mackysoft/dotnet-foundations/blob/master/LICENSE).
