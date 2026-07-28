# MackySoft.JsonSchema.Generation

`MackySoft.JsonSchema.Generation` builds one immutable JSON Contract Model from
an effective `System.Text.Json` contract and deterministically projects that
model to JSON Schema Draft 2020-12 and describe-oriented type metadata.

The package is product-independent. It does not own CLI behavior, artifact
placement, manifests, operation execution, or product validation policy.

## Installation

Pin the independently versioned package family:

```bash
dotnet add package MackySoft.JsonSchema.Generation --version "[0.3.0]"
```

```xml
<PackageReference Include="MackySoft.JsonSchema.Generation" Version="[0.3.0]" />
```

## Generate a contract

Resolve the same `JsonTypeInfo` used by runtime serialization and pass that
object directly to the request:

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
    TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
};
serializerOptions.MakeReadOnly();

JsonTypeInfo orderTypeInfo =
    serializerOptions.GetTypeInfo(typeof(Order));

var generator = new JsonContractGenerator(
    new JsonContractGeneratorOptions(
        JsonContractGenerationSettings.ClosedObjects));

JsonContractGenerationResult result = generator.Generate(
    new JsonContractGenerationRequest(
        "orders/create@1",
        orderTypeInfo,
        new JsonSchemaDocumentOptions(
            JsonSchemaDocumentKind.Complete,
            id: "https://schemas.example.test/orders/create.json",
            logicalName: "Create order")));

byte[] schemaUtf8 = result.GetJsonSchemaUtf8();
byte[] typeMetadataUtf8 = result.GetTypeMetadataUtf8();
string digest = result.ContractDigest;
```

A source-generated context can supply the input directly:

```csharp
JsonTypeInfo orderTypeInfo = ApiJsonContext.Default.Order;
```

The request has no separate `Type`, serializer-options, or resolver inputs.
`JsonTypeInfo.Type`, `JsonTypeInfo.Options`, its converter, properties,
polymorphism options, naming, ignore rules, and requiredness form one
authoritative serializer contract.

## Built-in attributes

Version 0.3.0 exposes exactly these built-in annotation and constraint
attributes:

- `TitleAttribute`
- `DescriptionAttribute`
- `PatternAttribute`
- `LengthAttribute`
- `ItemCountAttribute`
- `PropertyCountAttribute`

`PatternAttribute` and the typed metadata builder accept the portable
`$(?![\s\S])` suffix when the pattern must match the actual end of the input.
A trailing `$` alone also matches immediately before a final line terminator
under ECMAScript semantics. Other lookaround forms remain outside the supported
interoperable subset.

They describe facts that are not already present in the effective serializer
contract. Generation validates each constraint against the completed target
shape: string constraints require a string, item counts require an array, and
property counts require an object or dictionary.

Requiredness comes only from `JsonPropertyInfo.IsRequired`, including C#
`required` and `[JsonRequired]`. Serialized names come only from
`JsonPropertyInfo.Name`. Member nullability comes from CLR nullable metadata.
Arbitrary JSON comes from an effective `object`, `JsonElement`,
`JsonDocument`, `JsonNode`, or an explicit type mapper. Polymorphic branches
and discriminators come only from `JsonTypeInfo.PolymorphismOptions`.

There is no built-in enum, const, example, required, nullability, arbitrary,
discriminator, branch, or raw-JSON attribute.

## Typed metadata providers

Register providers explicitly by the value type they handle:

```csharp
using MackySoft.JsonSchema.Generation.Extensibility;
using MackySoft.JsonSchema.Generation.Metadata;

var metadata = new JsonContractMetadataRegistry()
    .RegisterProvider(new OrderMetadataProvider());

var generator = new JsonContractGenerator(
    new JsonContractGeneratorOptions(
        JsonContractGenerationSettings.ClosedObjects,
        metadataRegistry: metadata));

sealed class OrderMetadataProvider
    : IJsonContractMetadataProvider<Order>
{
    public string StableId => "example.orders.metadata";

    public string ContractVersion => "1";

    public void ProvideMetadata(
        JsonContractMetadataContext<Order> context,
        JsonContractMetadataBuilder<Order> builder)
    {
        if (context.PropertyInfo is null)
        {
            builder.SetTitle("Order");
            builder.AddExample(new Order { Id = "sample" });
        }
    }
}
```

`JsonContractMetadataContext<TValue>` exposes:

- the exact `JsonTypeInfo<TValue>` for the target value;
- the declaring `JsonTypeInfo`;
- the effective `JsonPropertyInfo`, or `null` for a type target.

Consumers observe a serialized property name through
`context.PropertyInfo.Name`; no property-name string is accepted by the API.

The scoped `JsonContractMetadataBuilder<TValue>` supports title, description,
pattern, string length, item count, property count, typed examples, typed
const, and exact numeric bounds. It has no operation for requiredness,
nullability, arbitrary values, enum values, polymorphic branches, or
discriminators.

`AddExample(TValue)` and `SetConst(TValue)` serialize the actual value through
the context's `JsonTypeInfo<TValue>`. They do not accept `JsonElement` or raw
JSON. A property-scoped declaration is rejected when a property-only converter
or number-handling override cannot be represented by that standalone type
information.

## Consumer attribute interpreters

A consumer can retain its own attribute vocabulary without adding attributes
to this package. Register one interpreter for a specific attribute type and
value type:

```csharp
[AttributeUsage(AttributeTargets.Property)]
sealed class PositiveAttribute : Attribute
{
}

sealed class PositiveInt32Interpreter
    : IJsonContractAttributeInterpreter<PositiveAttribute, int>
{
    public string StableId => "example.positive-int32";

    public string ContractVersion => "1";

    public void InterpretAttribute(
        PositiveAttribute attribute,
        JsonContractMetadataContext<int> context,
        JsonContractMetadataBuilder<int> builder)
    {
        builder.SetExclusiveMinimum(JsonContractNumber.FromInt64(0));
    }
}

var metadata = new JsonContractMetadataRegistry()
    .RegisterAttributeInterpreter<
        PositiveAttribute,
        int>(new PositiveInt32Interpreter());
```

Unregistered consumer attributes are ignored. The two generic arguments fix
both dispatch dimensions; there is no runtime attribute-name or value-kind
pseudo-union.

## Exact numeric bounds

Numeric bounds never pass through `double` or `float`:

```csharp
builder.SetMinimum(
    JsonContractNumber.FromInt64(9_007_199_254_740_993));
builder.SetMaximum(
    JsonContractNumber.FromDecimal(1234567890.123456789m));
builder.SetExclusiveMaximum(
    JsonContractNumber.Parse("1e1000"));
```

`JsonContractNumber` accepts `Int64`, `UInt64`, `Decimal`, `BigInteger`, or one
strict JSON number token. Inclusive and exclusive bounds are different
builder operations. Exact numeric semantics participate in schema bytes and
the contract digest without binary floating-point rounding.

## Text vocabularies

A closed enum-to-string contract is declared with
`MackySoft.Text.Vocabularies`, recognized through the effective converter, and
mapped explicitly by a type mapper. Canonical texts are read from the
vocabulary type that is already being mapped; callers do not submit a second
type or a hand-written value array.

```csharp
public JsonContractTypeMapping Map(JsonContractTypeMapperContext context)
{
    return JsonContractTypeMapping.TextVocabulary();
}
```

The mapper context exposes the mapped `JsonTypeInfo`, its declaring
`JsonTypeInfo`, and the effective `JsonPropertyInfo` when mapping a property.
The CLR type, serializer options, serialized property name, and
property-specific converter are derived from those authoritative STJ
contracts rather than supplied as parallel inputs. The effective converter is
`context.PropertyInfo?.CustomConverter ?? context.TypeInfo.Converter`.

Multiple canonical texts project to `enum`. One canonical text projects to
`const`. `MackySoft.Text.Vocabularies.Json` is not a dependency of this
package. An enum cannot use `Scalar` or `ContractType` to substitute another
string-valued shape; its finite strings must come from `TextVocabulary()` on
that mapped enum itself.

## Other extension points

The remaining extension points have separate responsibilities:

- `IJsonContractTypeMapper` interprets a recognized converter or value-object
  representation.
- `IJsonContractModelContributor` adds deterministic product metadata to the
  normalized model.
- `IJsonSchemaDocumentPostProcessor` adds delivery-only `x-*` members to the
  emitted schema and cannot mutate contract semantics.

Every registered extension declares a stable ID and contract version.
Registration order does not affect output. Duplicate stable IDs within an
extension category are rejected.

## Projection and determinism

Both JSON Schema and type metadata are emitted from the same immutable model.
Property names and requiredness therefore cannot diverge between projections.
STJ tagged polymorphism produces `oneOf` references and a discriminator from
the registered derived types; no branch list is declared a second time.

The SHA-256 contract digest covers normalized contract semantics, generation
settings, and semantic extension identities. Document-only values such as
`$id`, logical name, schema document kind, and post-processors do not change
the contract digest.

Generation rejects ambiguous converters, duplicate contract IDs, duplicate
extension IDs, conflicting annotations, invalid constraint/shape
combinations, unsupported polymorphism, and unstable mappings with a
`JsonContractGenerationException`.

## 0.3.0 clean break

Version 0.3.0 intentionally removes the 0.2 metadata pseudo-union, non-generic
metadata provider, raw JSON metadata factories, manual enum/branch APIs, and
the multi-source generation request. It provides no compatibility aliases,
obsolete shims, or legacy overloads.
