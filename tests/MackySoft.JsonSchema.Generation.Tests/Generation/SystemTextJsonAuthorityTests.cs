using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using MackySoft.JsonSchema.Generation.Annotations;
using MackySoft.JsonSchema.Generation.Configuration;
using MackySoft.JsonSchema.Generation.ContractModel;
using MackySoft.JsonSchema.Generation.Diagnostics;
using MackySoft.JsonSchema.Generation.Extensibility;
using MackySoft.JsonSchema.Generation.Metadata;
using MackySoft.JsonSchema.Generation.Projection;
using MackySoft.JsonSchema.Generation.Tests.Fixtures;

namespace MackySoft.JsonSchema.Generation.Tests.Generation;

public sealed class SystemTextJsonAuthorityTests
{
    private const string BmpScalarPattern =
        "^[\\u0000-\\uD7FF\\uE000-\\uFFFF]$";

    private const string GuidPattern =
        "^[0-9A-Fa-f]{8}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{12}$";

    public static TheoryData<Type, Type> InvalidLexicalConstantContracts =>
        new()
        {
            { typeof(InvalidCharacterConstantContract), typeof(char) },
            { typeof(InvalidGuidConstantContract), typeof(Guid) },
        };

    public static TheoryData<Type, Type> UnsupportedLexicalContracts =>
        new()
        {
            { typeof(DateTimeContract), typeof(DateTime) },
            {
                typeof(DateTimeOffsetContract),
                typeof(DateTimeOffset)
            },
            { typeof(TimeSpanContract), typeof(TimeSpan) },
            { typeof(UriContract), typeof(Uri) },
            { typeof(VersionContract), typeof(Version) },
            { typeof(ByteArrayContract), typeof(byte[]) },
            { typeof(DateOnlyContract), typeof(DateOnly) },
            { typeof(TimeOnlyContract), typeof(TimeOnly) },
        };

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_PreservesAuthoritativeJsonTypeInfoPropertyOrder ()
    {
        JsonSerializerOptions serializerOptions = ClosedSerializerOptions();
        var runtimeValue = new OrderedPropertiesContract
        {
            Zulu = 1,
            Alpha = 2,
        };
        string[] runtimeOrder = JsonDocument
            .Parse(JsonSerializer.Serialize(runtimeValue, serializerOptions))
            .RootElement
            .EnumerateObject()
            .Select(static property => property.Name)
            .ToArray();

        JsonContractGenerationResult result =
            GenerationTestHarness.Generate<OrderedPropertiesContract>(
                "tests.authoritative-property-order",
                serializerOptions);
        string[] modelOrder = result.Model.Root.Properties
            .Select(static property => property.Name)
            .ToArray();
        using JsonDocument schema = JsonDocument.Parse(
            result.GetJsonSchemaUtf8());
        string[] schemaOrder = schema.RootElement
            .GetProperty("properties")
            .EnumerateObject()
            .Select(static property => property.Name)
            .ToArray();

        Assert.Equal(new[] { "Zulu", "Alpha" }, runtimeOrder);
        Assert.Equal(runtimeOrder, modelOrder);
        Assert.Equal(runtimeOrder, schemaOrder);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_IgnoreReadOnlyPropertiesMatchesRuntimeMemberSet ()
    {
        var resolver = new DefaultJsonTypeInfoResolver();
        resolver.Modifiers.Add(
            static typeInfo =>
            {
                if (typeInfo.Type == typeof(ReadOnlyPropertiesContract))
                {
                    typeInfo.Properties
                        .Single(
                            static property =>
                                property.Name == nameof(
                                    ReadOnlyPropertiesContract.Predicate))
                        .ShouldSerialize = static (_, _) => true;
                }
            });
        JsonSerializerOptions serializerOptions = ClosedSerializerOptions();
        serializerOptions.IgnoreReadOnlyProperties = true;
        serializerOptions.TypeInfoResolver = resolver;
        var runtimeValue = new ReadOnlyPropertiesContract
        {
            Mutable = 1,
        };
        string[] runtimeProperties = JsonDocument
            .Parse(JsonSerializer.Serialize(runtimeValue, serializerOptions))
            .RootElement
            .EnumerateObject()
            .Select(static property => property.Name)
            .ToArray();

        JsonContractGenerationResult result =
            GenerationTestHarness.Generate<ReadOnlyPropertiesContract>(
                "tests.ignore-read-only-properties",
                serializerOptions);
        string[] modelProperties = result.Model.Root.Properties
            .Select(static property => property.Name)
            .ToArray();
        using JsonDocument schema = JsonDocument.Parse(
            result.GetJsonSchemaUtf8());
        string[] schemaProperties = schema.RootElement
            .GetProperty("properties")
            .EnumerateObject()
            .Select(static property => property.Name)
            .ToArray();

        Assert.Equal(
            new[] { "Mutable", "Items", "Explicit", "Predicate" },
            runtimeProperties);
        Assert.Equal(runtimeProperties, modelProperties);
        Assert.Equal(runtimeProperties, schemaProperties);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_IgnoreReadOnlyFieldsMatchesRuntimeMemberSet ()
    {
        JsonSerializerOptions serializerOptions = ClosedSerializerOptions();
        serializerOptions.IncludeFields = true;
        serializerOptions.IgnoreReadOnlyFields = true;
        var runtimeValue = new ReadOnlyFieldsContract
        {
            mutable = 1,
        };
        string[] runtimeFields = JsonDocument
            .Parse(JsonSerializer.Serialize(runtimeValue, serializerOptions))
            .RootElement
            .EnumerateObject()
            .Select(static property => property.Name)
            .ToArray();

        JsonContractGenerationResult result =
            GenerationTestHarness.Generate<ReadOnlyFieldsContract>(
                "tests.ignore-read-only-fields",
                serializerOptions);
        string[] modelFields = result.Model.Root.Properties
            .Select(static property => property.Name)
            .ToArray();
        using JsonDocument schema = JsonDocument.Parse(
            result.GetJsonSchemaUtf8());
        string[] schemaFields = schema.RootElement
            .GetProperty("properties")
            .EnumerateObject()
            .Select(static property => property.Name)
            .ToArray();

        Assert.Equal(
            new[] { "mutable", "items", "explicitValue" },
            runtimeFields);
        Assert.Equal(runtimeFields, modelFields);
        Assert.Equal(runtimeFields, schemaFields);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenPropertyNamesAreCaseInsensitive_ReportsUnsupportedTypeInfo ()
    {
        JsonSerializerOptions serializerOptions = ClosedSerializerOptions();
        serializerOptions.PropertyNameCaseInsensitive = true;

        JsonContractGenerationException exception =
            Assert.Throws<JsonContractGenerationException>(
                () => GenerationTestHarness.Generate<SimpleObjectContract>(
                    "tests.case-insensitive-properties",
                    serializerOptions));

        Assert.Equal(
            JsonContractGenerationFailureKind.UnsupportedTypeInfo,
            exception.FailureKind);
        Assert.Equal(typeof(SimpleObjectContract), exception.TargetType);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenGlobalDefaultIgnoreCanOmitRequiredProperty_ReportsUnsupportedTypeInfo ()
    {
        JsonSerializerOptions serializerOptions = ClosedSerializerOptions();
        serializerOptions.DefaultIgnoreCondition =
            JsonIgnoreCondition.WhenWritingDefault;

        JsonContractGenerationException exception =
            Assert.Throws<JsonContractGenerationException>(
                () => GenerationTestHarness.Generate<
                    GloballyIgnoredRequiredContract>(
                        "tests.required-global-ignore",
                        serializerOptions));

        Assert.Equal(
            JsonContractGenerationFailureKind.UnsupportedTypeInfo,
            exception.FailureKind);
        Assert.Equal(typeof(int), exception.TargetType);
        Assert.Equal("Value", exception.JsonPropertyName);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenLegacyNullIgnoreCanOmitRequiredProperty_ReportsUnsupportedTypeInfo ()
    {
        JsonSerializerOptions serializerOptions = ClosedSerializerOptions();
#pragma warning disable SYSLIB0020
        serializerOptions.IgnoreNullValues = true;
#pragma warning restore SYSLIB0020

        JsonContractGenerationException exception =
            Assert.Throws<JsonContractGenerationException>(
                () => GenerationTestHarness.Generate<
                    LegacyNullIgnoredRequiredContract>(
                        "tests.required-legacy-null-ignore",
                        serializerOptions));

        Assert.Equal(
            JsonContractGenerationFailureKind.UnsupportedTypeInfo,
            exception.FailureKind);
        Assert.Equal(typeof(string), exception.TargetType);
        Assert.Equal("Value", exception.JsonPropertyName);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenJsonIgnoreNullCanOmitRequiredProperty_ReportsUnsupportedTypeInfo ()
    {
        JsonContractGenerationException exception =
            Assert.Throws<JsonContractGenerationException>(
                () => GenerationTestHarness.Generate<
                    NullIgnoredRequiredContract>(
                        "tests.required-null-ignore"));

        Assert.Equal(
            JsonContractGenerationFailureKind.UnsupportedTypeInfo,
            exception.FailureKind);
        Assert.Equal(typeof(string), exception.TargetType);
        Assert.Equal("Value", exception.JsonPropertyName);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenJsonIgnoreDefaultCanOmitRequiredProperty_ReportsUnsupportedTypeInfo ()
    {
        JsonContractGenerationException exception =
            Assert.Throws<JsonContractGenerationException>(
                () => GenerationTestHarness.Generate<
                    DefaultIgnoredRequiredContract>(
                        "tests.required-default-ignore"));

        Assert.Equal(
            JsonContractGenerationFailureKind.UnsupportedTypeInfo,
            exception.FailureKind);
        Assert.Equal(typeof(int), exception.TargetType);
        Assert.Equal("Value", exception.JsonPropertyName);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenTypeInfoAllowsQuotedNumbers_ReportsUnsupportedConverter ()
    {
        var resolver = new DefaultJsonTypeInfoResolver();
        resolver.Modifiers.Add(
            static typeInfo =>
            {
                if (typeInfo.Type == typeof(int))
                {
                    typeInfo.NumberHandling =
                        JsonNumberHandling.AllowReadingFromString;
                }
            });
        JsonSerializerOptions serializerOptions = ClosedSerializerOptions();
        serializerOptions.TypeInfoResolver = resolver;

        JsonContractGenerationException exception =
            Assert.Throws<JsonContractGenerationException>(
                () => GenerationTestHarness.Generate<NumberContract>(
                    "tests.type-info-number-handling",
                    serializerOptions));

        Assert.Equal(
            JsonContractGenerationFailureKind.UnsupportedConverter,
            exception.FailureKind);
        Assert.Equal(typeof(int), exception.TargetType);
        Assert.Equal("Value", exception.JsonPropertyName);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenClosedSchemaContradictsSkippedUnmappedMembers_ReportsUnsupportedTypeInfo ()
    {
        var serializerOptions = new JsonSerializerOptions
        {
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
        };

        JsonContractGenerationException exception =
            Assert.Throws<JsonContractGenerationException>(
                () => GenerationTestHarness.Generate<SimpleObjectContract>(
                    "tests.closed-schema-runtime-skip",
                    serializerOptions));

        Assert.Equal(
            JsonContractGenerationFailureKind.UnsupportedTypeInfo,
            exception.FailureKind);
        Assert.Equal(typeof(SimpleObjectContract), exception.TargetType);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenOpenSchemaContradictsDisallowedUnmappedMembers_ReportsUnsupportedTypeInfo ()
    {
        JsonSerializerOptions serializerOptions = ClosedSerializerOptions();
        var settings = new JsonContractGenerationSettings(
            JsonObjectClosure.AllowAdditionalProperties);

        JsonContractGenerationException exception =
            Assert.Throws<JsonContractGenerationException>(
                () => GenerationTestHarness.Generate<SimpleObjectContract>(
                    "tests.open-schema-runtime-disallow",
                    serializerOptions,
                    settings: settings));

        Assert.Equal(
            JsonContractGenerationFailureKind.UnsupportedTypeInfo,
            exception.FailureKind);
        Assert.Equal(typeof(SimpleObjectContract), exception.TargetType);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_UsesTypeInfoUnmappedMemberOverride ()
    {
        var resolver = new DefaultJsonTypeInfoResolver();
        resolver.Modifiers.Add(
            static typeInfo =>
            {
                if (typeInfo.Type == typeof(SimpleObjectContract))
                {
                    typeInfo.UnmappedMemberHandling =
                        JsonUnmappedMemberHandling.Disallow;
                }
            });
        var serializerOptions = new JsonSerializerOptions
        {
            TypeInfoResolver = resolver,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
        };

        JsonContractGenerationResult result =
            GenerationTestHarness.Generate<SimpleObjectContract>(
                "tests.type-info-unmapped-override",
                serializerOptions);

        using JsonDocument schema = JsonDocument.Parse(
            result.GetJsonSchemaUtf8());
        Assert.False(
            schema.RootElement
                .GetProperty("additionalProperties")
                .GetBoolean());
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_RepresentableLexicalScalarsProjectExactConstraints ()
    {
        JsonContractGenerationResult result =
            GenerationTestHarness.Generate<LexicalScalarContract>(
                "tests.lexical-scalar-shapes");
        JsonContractNode character = GenerationTestHarness
            .GetProperty(result.Model.Root, "Character")
            .Value;
        JsonContractNode guid = GenerationTestHarness
            .GetProperty(result.Model.Root, "Guid")
            .Value;

        Assert.Equal(1, character.Constraints.MinimumLength);
        Assert.Equal(1, character.Constraints.MaximumLength);
        Assert.Equal(BmpScalarPattern, character.Constraints.Pattern);
        Assert.Equal("uuid", guid.Constraints.Format);
        Assert.Equal(36, guid.Constraints.MinimumLength);
        Assert.Equal(36, guid.Constraints.MaximumLength);
        Assert.Equal(GuidPattern, guid.Constraints.Pattern);

        using JsonDocument schema = JsonDocument.Parse(
            result.GetJsonSchemaUtf8());
        JsonElement properties = schema.RootElement.GetProperty("properties");
        JsonElement characterSchema = properties.GetProperty("Character");
        Assert.Equal(1, characterSchema.GetProperty("minLength").GetInt32());
        Assert.Equal(1, characterSchema.GetProperty("maxLength").GetInt32());
        Assert.Equal(
            BmpScalarPattern,
            characterSchema.GetProperty("pattern").GetString());
        JsonElement guidSchema = properties.GetProperty("Guid");
        Assert.Equal(36, guidSchema.GetProperty("minLength").GetInt32());
        Assert.Equal(36, guidSchema.GetProperty("maxLength").GetInt32());
        Assert.Equal(
            GuidPattern,
            guidSchema.GetProperty("pattern").GetString());
    }

    [Theory]
    [MemberData(nameof(UnsupportedLexicalContracts))]
    public void Generate_WhenLexicalAcceptanceHasNoBuiltInProjection_ReportsUnsupportedTypeInfo (
        Type contractType,
        Type scalarType)
    {
        JsonContractGenerationException exception =
            Assert.Throws<JsonContractGenerationException>(
                () => Generate(contractType, ClosedSerializerOptions()));

        Assert.Equal(
            JsonContractGenerationFailureKind.UnsupportedTypeInfo,
            exception.FailureKind);
        Assert.Equal(scalarType, exception.TargetType);
        Assert.Equal("Value", exception.JsonPropertyName);
    }

    [Theory]
    [MemberData(nameof(InvalidLexicalConstantContracts))]
    public void Generate_WhenLexicalConstantIsNotRoundTrippedBySerializer_ReportsInvalidMetadata (
        Type contractType,
        Type scalarType)
    {
        JsonContractGenerationException exception =
            Assert.Throws<JsonContractGenerationException>(
                () => Generate(contractType, ClosedSerializerOptions()));

        Assert.Equal(
            JsonContractGenerationFailureKind.InvalidMetadataValue,
            exception.FailureKind);
        Assert.Equal(scalarType, exception.TargetType);
        Assert.Equal("Value", exception.JsonPropertyName);
        Assert.Equal(JsonContractMetadataKind.Const, exception.MetadataKind);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenMappedBuiltInLexicalConstantDoesNotRoundTrip_ReportsInvalidMetadata ()
    {
        var mapper = new TestTypeMapper(
            "tests.mapper.date-time",
            static context => context.TargetType == typeof(DateTime),
            static _ => JsonContractTypeMapping.Scalar(
                JsonContractScalarKind.String));

        JsonContractGenerationException exception =
            Assert.Throws<JsonContractGenerationException>(
                () => GenerationTestHarness.Generate<
                    InvalidDateTimeConstantContract>(
                        "tests.mapped-invalid-date-time",
                        typeMappers: new[] { mapper }));

        Assert.Equal(
            JsonContractGenerationFailureKind.InvalidMetadataValue,
            exception.FailureKind);
        Assert.Equal(typeof(DateTime), exception.TargetType);
        Assert.Equal("Value", exception.JsonPropertyName);
        Assert.Equal(JsonContractMetadataKind.Const, exception.MetadataKind);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenLexicalConstantRoundTripsThroughSerializer_AcceptsIt ()
    {
        JsonContractGenerationResult result =
            GenerationTestHarness.Generate<ValidGuidConstantContract>(
                "tests.valid-guid-constant");

        JsonContractNode value = GenerationTestHarness
            .GetProperty(result.Model.Root, "Value")
            .Value;
        Assert.Equal(JsonContractNodeKind.Const, value.Kind);
        Assert.Equal(
            "00000000-0000-0000-0000-000000000000",
            value.Constant?.GetString());
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenAnyValueOverwritesKnownScalar_ReportsMetadataConflict ()
    {
        JsonContractGenerationException exception =
            Assert.Throws<JsonContractGenerationException>(
                () => GenerationTestHarness.Generate<
                    ArbitraryScalarContract>(
                        "tests.arbitrary-known-scalar"));

        Assert.Equal(
            JsonContractGenerationFailureKind.ConflictingMetadata,
            exception.FailureKind);
        Assert.Equal(typeof(int), exception.TargetType);
        Assert.Equal("Value", exception.JsonPropertyName);
        Assert.Equal(JsonContractMetadataKind.Arbitrary, exception.MetadataKind);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenAnyValueOverwritesKnownObject_ReportsMetadataConflict ()
    {
        JsonContractGenerationException exception =
            Assert.Throws<JsonContractGenerationException>(
                () => GenerationTestHarness.Generate<
                    ArbitraryObjectContract>(
                        "tests.arbitrary-known-object"));

        Assert.Equal(
            JsonContractGenerationFailureKind.ConflictingMetadata,
            exception.FailureKind);
        Assert.Equal(typeof(KnownObject), exception.TargetType);
        Assert.Equal("Value", exception.JsonPropertyName);
        Assert.Equal(JsonContractMetadataKind.Arbitrary, exception.MetadataKind);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenAnyValueDeclaresUnknownConverter_UsesArbitraryShape ()
    {
        JsonContractGenerationResult result =
            GenerationTestHarness.Generate<ArbitraryUnknownConverterContract>(
                "tests.arbitrary-unknown-converter");

        JsonContractNode value = GenerationTestHarness
            .GetProperty(result.Model.Root, "Value")
            .Value;
        Assert.Equal(JsonContractNodeKind.Arbitrary, value.Kind);
        Assert.True(value.IsNullable);
    }

    private static JsonSerializerOptions ClosedSerializerOptions ()
    {
        return new JsonSerializerOptions
        {
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        };
    }

    private static JsonContractGenerationResult Generate (
        Type contractType,
        JsonSerializerOptions serializerOptions)
    {
        IJsonTypeInfoResolver resolver =
            serializerOptions.TypeInfoResolver
            ?? new DefaultJsonTypeInfoResolver();
        var generator = new JsonContractGenerator(
            new JsonContractGeneratorOptions(
                JsonContractGenerationSettings.ClosedObjects));
        return generator.Generate(
            new JsonContractGenerationRequest(
                "tests.invalid-lexical-constant",
                contractType,
                serializerOptions,
                resolver,
                new JsonSchemaDocumentOptions(
                    JsonSchemaDocumentKind.Complete,
                    id: null,
                    logicalName: null)));
    }

    private sealed class OrderedPropertiesContract
    {
        [JsonPropertyOrder(4)]
        public int Zulu { get; set; }

        [JsonPropertyOrder(4)]
        public int Alpha { get; set; }
    }

    private sealed class ReadOnlyPropertiesContract
    {
        public int Mutable { get; set; }

        public int Scalar => 2;

        public List<int> Items { get; } = new() { 3 };

        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public int Explicit => 4;

        public int Predicate => 5;
    }

    private sealed class ReadOnlyFieldsContract
    {
        public int mutable;

        public readonly int scalar = 2;

        public readonly List<int> items = new() { 3 };

        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public readonly int explicitValue = 4;
    }

    private sealed class SimpleObjectContract
    {
        public int Value { get; set; }
    }

    private sealed class GloballyIgnoredRequiredContract
    {
        [JsonRequired]
        public int Value { get; set; }
    }

    private sealed class NullIgnoredRequiredContract
    {
        [JsonRequired]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Value { get; set; }
    }

    private sealed class LegacyNullIgnoredRequiredContract
    {
        [JsonRequired]
        public string? Value { get; set; }
    }

    private sealed class DefaultIgnoredRequiredContract
    {
        [JsonRequired]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int Value { get; set; }
    }

    private sealed class NumberContract
    {
        public int Value { get; set; }
    }

    private sealed class LexicalScalarContract
    {
        public char Character { get; set; }

        public Guid Guid { get; set; }
    }

    private sealed class DateTimeContract
    {
        public DateTime Value { get; set; }
    }

    private sealed class DateTimeOffsetContract
    {
        public DateTimeOffset Value { get; set; }
    }

    private sealed class TimeSpanContract
    {
        public TimeSpan Value { get; set; }
    }

    private sealed class UriContract
    {
        public Uri Value { get; set; } = new("relative", UriKind.Relative);
    }

    private sealed class VersionContract
    {
        public Version Value { get; set; } = new();
    }

    private sealed class ByteArrayContract
    {
        public byte[] Value { get; set; } = Array.Empty<byte>();
    }

    private sealed class DateOnlyContract
    {
        public DateOnly Value { get; set; }
    }

    private sealed class TimeOnlyContract
    {
        public TimeOnly Value { get; set; }
    }

    private sealed class InvalidCharacterConstantContract
    {
        [Const("\"ab\"")]
        public char Value { get; set; }
    }

    private sealed class InvalidGuidConstantContract
    {
        [Const("\"not-a-guid\"")]
        public Guid Value { get; set; }
    }

    private sealed class InvalidDateTimeConstantContract
    {
        [Const("\"not-a-date-time\"")]
        public DateTime Value { get; set; }
    }

    private sealed class ValidGuidConstantContract
    {
        [Const("\"00000000-0000-0000-0000-000000000000\"")]
        public Guid Value { get; set; }
    }

    private sealed class ArbitraryScalarContract
    {
        [AnyValue]
        public int Value { get; set; }
    }

    private sealed class ArbitraryObjectContract
    {
        [AnyValue]
        public KnownObject Value { get; set; } = new();
    }

    private sealed class KnownObject
    {
        public int Count { get; set; }
    }

    private sealed class ArbitraryUnknownConverterContract
    {
        [AnyValue]
        [JsonConverter(typeof(UnknownValueConverter))]
        public UnknownValue Value { get; set; }
    }

    private readonly record struct UnknownValue (string Text);

    private sealed class UnknownValueConverter : JsonConverter<UnknownValue>
    {
        public override UnknownValue Read (
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            using JsonDocument document = JsonDocument.ParseValue(ref reader);
            return new UnknownValue(document.RootElement.GetRawText());
        }

        public override void Write (
            Utf8JsonWriter writer,
            UnknownValue value,
            JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.Text);
        }
    }
}
