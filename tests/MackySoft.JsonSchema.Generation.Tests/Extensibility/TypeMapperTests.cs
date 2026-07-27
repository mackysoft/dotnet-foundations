using System.Text.Json;
using System.Text.Json.Serialization;
using MackySoft.JsonSchema.Generation.ContractModel;
using MackySoft.JsonSchema.Generation.Diagnostics;
using MackySoft.JsonSchema.Generation.Extensibility;
using MackySoft.JsonSchema.Generation.Metadata;
using MackySoft.JsonSchema.Generation.Tests.Fixtures;
using MackySoft.Text.Vocabularies;

namespace MackySoft.JsonSchema.Generation.Tests.Extensibility;

public sealed class TypeMapperTests
{
    [Fact]
    [Trait("Size", "Small")]
    public void Scalar_WhenKindIsUndefined_RejectsDeclaration ()
    {
        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(
                () => JsonContractTypeMapping.Scalar(
                    (JsonContractScalarKind)int.MaxValue));

        Assert.Equal("scalarKind", exception.ParamName);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenOneMapperDeclaresOpaqueScalar_UsesMappedShape ()
    {
        var mapper = new TestTypeMapper(
            "tests.mapper.opaque",
            static context =>
                context.TypeInfo.Type == typeof(OpaqueValue),
            static _ => JsonContractTypeMapping.Scalar(
                JsonContractScalarKind.String));

        JsonContractGenerationResult result =
            GenerationTestHarness.Generate<OpaqueContract>(
                "tests.mapper-success",
                OpaqueSerializerOptions(),
                typeMappers: new[] { mapper });

        JsonContractNode valueNode = Assert.Single(
            result.Model.Root.Properties).Value;
        Assert.Equal(JsonContractNodeKind.Scalar, valueNode.Kind);
        Assert.Equal(JsonContractScalarKind.String, valueNode.ScalarKind);

        using JsonDocument schema = JsonDocument.Parse(
            result.GetJsonSchemaUtf8());
        Assert.Equal(
            "string",
            schema.RootElement
                .GetProperty("properties")
                .GetProperty("Value")
                .GetProperty("type")
                .GetString());
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenMapperTurnsNonVocabularyEnumIntoString_ReportsUnsupportedConverter ()
    {
        var mapper = new TestTypeMapper(
            "tests.mapper.handwritten-enum",
            static context =>
                context.TypeInfo.Type == typeof(HandwrittenState),
            static _ => JsonContractTypeMapping.Scalar(
                JsonContractScalarKind.String));

        JsonContractGenerationException exception =
            Assert.Throws<JsonContractGenerationException>(
                () => GenerationTestHarness.Generate<HandwrittenEnumContract>(
                    "tests.mapper-non-vocabulary-enum",
                    typeMappers: new[] { mapper }));

        Assert.Equal(
            JsonContractGenerationFailureKind.UnsupportedConverter,
            exception.FailureKind);
        Assert.Equal(typeof(HandwrittenState), exception.TargetType);
        Assert.Equal("State", exception.JsonPropertyName);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenNonVocabularyEnumBorrowsTextVocabularySurrogate_ReportsUnsupportedConverter ()
    {
        var mapper = new TestTypeMapper(
            "tests.mapper.borrowed-vocabulary",
            static context =>
                context.TypeInfo.Type == typeof(HandwrittenState)
                || context.TypeInfo.Type
                    == typeof(VocabularySurrogateState),
            static context =>
                context.TypeInfo.Type == typeof(HandwrittenState)
                    ? JsonContractTypeMapping.ContractType(
                        typeof(VocabularySurrogateState))
                    : JsonContractTypeMapping.TextVocabulary());

        JsonContractGenerationException exception =
            Assert.Throws<JsonContractGenerationException>(
                () => GenerationTestHarness.Generate<
                    HandwrittenEnumContract>(
                        "tests.mapper-borrowed-vocabulary",
                        typeMappers: new[] { mapper }));

        Assert.Equal(
            JsonContractGenerationFailureKind.UnsupportedConverter,
            exception.FailureKind);
        Assert.Equal(typeof(HandwrittenState), exception.TargetType);
        Assert.Equal("State", exception.JsonPropertyName);
        Assert.Equal(
            new[] { "tests.mapper.borrowed-vocabulary" },
            exception.SourceIds);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenTextVocabularyEnumDropsItsFiniteSurrogate_ReportsUnsupportedConverter ()
    {
        var mapper = new TestTypeMapper(
            "tests.mapper.flattened-vocabulary",
            static context =>
                context.TypeInfo.Type
                    == typeof(VocabularySurrogateState),
            static _ => JsonContractTypeMapping.ContractType(
                typeof(string)));

        JsonContractGenerationException exception =
            Assert.Throws<JsonContractGenerationException>(
                () => GenerationTestHarness.Generate<
                    VocabularyEnumContract>(
                        "tests.mapper-flattened-vocabulary",
                        typeMappers: new[] { mapper }));

        Assert.Equal(
            JsonContractGenerationFailureKind.UnsupportedConverter,
            exception.FailureKind);
        Assert.Equal(
            typeof(VocabularySurrogateState),
            exception.TargetType);
        Assert.Equal("State", exception.JsonPropertyName);
        Assert.Equal(
            new[] { "tests.mapper.flattened-vocabulary" },
            exception.SourceIds);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenMapperReplacesBuiltInSerializerShape_ReportsUnsupportedConverter ()
    {
        var mapper = new TestTypeMapper(
            "tests.mapper.replaces-built-in",
            static context =>
                context.TypeInfo.Type == typeof(int),
            static _ => JsonContractTypeMapping.Scalar(
                JsonContractScalarKind.String));

        JsonContractGenerationException exception =
            Assert.Throws<JsonContractGenerationException>(
                () => GenerationTestHarness.Generate<BuiltInContract>(
                    "tests.mapper-replaces-built-in",
                    typeMappers: new[] { mapper }));

        Assert.Equal(
            JsonContractGenerationFailureKind.UnsupportedConverter,
            exception.FailureKind);
        Assert.Equal(typeof(int), exception.TargetType);
        Assert.Equal("Value", exception.JsonPropertyName);
        Assert.Equal(
            new[] { "tests.mapper.replaces-built-in" },
            exception.SourceIds);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenMapperDeclaresUndefinedScalarKind_ReportsMapperFailure ()
    {
        var mapper = new TestTypeMapper(
            "tests.mapper.undefined-scalar",
            static context =>
                context.TypeInfo.Type == typeof(OpaqueValue),
            static _ => JsonContractTypeMapping.Scalar(
                (JsonContractScalarKind)int.MaxValue));

        JsonContractGenerationException exception =
            Assert.Throws<JsonContractGenerationException>(
                () => GenerationTestHarness.Generate<OpaqueContract>(
                    "tests.mapper-undefined-scalar",
                    OpaqueSerializerOptions(),
                    typeMappers: new[] { mapper }));

        Assert.Equal(
            JsonContractGenerationFailureKind.UnsupportedConverter,
            exception.FailureKind);
        Assert.Equal("tests.mapper-undefined-scalar", exception.ContractId);
        Assert.Equal(typeof(OpaqueValue), exception.TargetType);
        Assert.Equal("Value", exception.JsonPropertyName);
        Assert.Equal(
            new[] { "tests.mapper.undefined-scalar" },
            exception.SourceIds);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenMapperDelegatesToContractTypes_PreservesSurrogateRootSemantics ()
    {
        var metadata = new JsonContractMetadataRegistry()
            .RegisterProvider(
                new TestMetadataProvider<int>(
                    "tests.mapper.surrogate-number",
                    static (context, builder) =>
                    {
                        if (context.PropertyInfo is null)
                        {
                            builder.SetTitle("Surrogate number");
                            builder.SetDescription(
                                "An integer constrained by the surrogate contract.");
                            builder.AddExample(5);
                            builder.SetExclusiveMinimum(
                                JsonContractNumber.FromInt64(0));
                            builder.SetExclusiveMaximum(
                                JsonContractNumber.FromInt64(10));
                        }
                    }))
            .RegisterProvider(
                new TestMetadataProvider<int[]>(
                    "tests.mapper.surrogate-items",
                    static (context, builder) =>
                    {
                        if (context.PropertyInfo is null)
                        {
                            builder.SetMinimumItemCount(1);
                            builder.SetMaximumItemCount(3);
                        }
                    }))
            .RegisterProvider(
                new TestMetadataProvider<Dictionary<string, int>>(
                    "tests.mapper.surrogate-properties",
                    static (context, builder) =>
                    {
                        if (context.PropertyInfo is null)
                        {
                            builder.SetMinimumPropertyCount(2);
                            builder.SetMaximumPropertyCount(4);
                        }
                    }));

        JsonContractGenerationResult result =
            GenerationTestHarness.Generate<SurrogateMappingContract>(
                "tests.mapper-contract-type-semantics",
                SurrogateSerializerOptions(),
                metadataRegistry: metadata,
                typeMappers: new[] { SurrogateTypeMapper() });

        JsonContractNode number =
            GenerationTestHarness.GetProperty(result.Model.Root, "Number").Value;
        Assert.Equal("Surrogate number", number.Annotations.Title);
        Assert.Equal(
            "An integer constrained by the surrogate contract.",
            number.Annotations.Description);
        Assert.Equal(
            5,
            Assert.Single(number.Annotations.Examples).GetInt32());
        Assert.Equal(
            0,
            number.Constraints.ExclusiveMinimum?.GetInt32());
        Assert.Equal(
            10,
            number.Constraints.ExclusiveMaximum?.GetInt32());

        JsonContractNode items =
            GenerationTestHarness.GetProperty(result.Model.Root, "Items").Value;
        Assert.Equal(1, items.Constraints.MinimumItems);
        Assert.Equal(3, items.Constraints.MaximumItems);

        JsonContractNode properties =
            GenerationTestHarness.GetProperty(result.Model.Root, "Properties").Value;
        Assert.Equal(2, properties.Constraints.MinimumProperties);
        Assert.Equal(4, properties.Constraints.MaximumProperties);

        using JsonDocument schema = JsonDocument.Parse(
            result.GetJsonSchemaUtf8());
        JsonElement schemaProperties =
            schema.RootElement.GetProperty("properties");
        JsonElement numberSchema = schemaProperties.GetProperty("Number");
        Assert.Equal(0, numberSchema.GetProperty("exclusiveMinimum").GetInt32());
        Assert.Equal(10, numberSchema.GetProperty("exclusiveMaximum").GetInt32());
        Assert.Equal(
            1,
            schemaProperties
                .GetProperty("Items")
                .GetProperty("minItems")
                .GetInt32());
        Assert.Equal(
            4,
            schemaProperties
                .GetProperty("Properties")
                .GetProperty("maxProperties")
                .GetInt32());
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenTargetAnnotationConflictsWithSurrogate_ReportsTypedFailure ()
    {
        var metadata = new JsonContractMetadataRegistry()
            .RegisterProvider(
                new TestMetadataProvider<int>(
                    "tests.mapper.surrogate-title",
                    static (context, builder) =>
                    {
                        if (context.PropertyInfo is null)
                        {
                            builder.SetTitle("Surrogate title");
                        }
                    }))
            .RegisterProvider(
                new TestMetadataProvider<OpaqueNumber>(
                    "tests.mapper.target-title",
                    static (context, builder) =>
                    {
                        builder.SetTitle("Target title");
                    }));

        JsonContractGenerationException exception =
            Assert.Throws<JsonContractGenerationException>(
                () => GenerationTestHarness.Generate<NumberMappingContract>(
                    "tests.mapper-contract-type-title-conflict",
                    SurrogateSerializerOptions(),
                    metadataRegistry: metadata,
                    typeMappers: new[] { SurrogateTypeMapper() }));

        Assert.Equal(
            JsonContractGenerationFailureKind.ConflictingMetadata,
            exception.FailureKind);
        Assert.Equal(typeof(OpaqueNumber), exception.TargetType);
        Assert.Equal("Number", exception.JsonPropertyName);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenTargetConstraintWidensSurrogate_ReportsTypedFailure ()
    {
        var metadata = new JsonContractMetadataRegistry()
            .RegisterProvider(
                new TestMetadataProvider<int>(
                    "tests.mapper.surrogate-range",
                    static (context, builder) =>
                    {
                        if (context.PropertyInfo is null)
                        {
                            builder.SetExclusiveMinimum(
                                JsonContractNumber.FromInt64(0));
                        }
                    }))
            .RegisterProvider(
                new TestMetadataProvider<OpaqueNumber>(
                    "tests.mapper.target-range",
                    static (context, builder) =>
                    {
                        builder.SetExclusiveMinimum(
                            JsonContractNumber.FromInt64(-1));
                    }));

        JsonContractGenerationException exception =
            Assert.Throws<JsonContractGenerationException>(
                () => GenerationTestHarness.Generate<NumberMappingContract>(
                    "tests.mapper-contract-type-range-conflict",
                    SurrogateSerializerOptions(),
                    metadataRegistry: metadata,
                    typeMappers: new[] { SurrogateTypeMapper() }));

        Assert.Equal(
            JsonContractGenerationFailureKind.InvalidMetadataValue,
            exception.FailureKind);
        Assert.Equal(typeof(OpaqueNumber), exception.TargetType);
        Assert.Equal("Number", exception.JsonPropertyName);
    }

    private static TestTypeMapper SurrogateTypeMapper ()
    {
        return new TestTypeMapper(
            "tests.mapper.contract-type",
            static context =>
                context.TypeInfo.Type == typeof(OpaqueNumber)
                || context.TypeInfo.Type == typeof(OpaqueItems)
                || context.TypeInfo.Type == typeof(OpaqueProperties),
            static context =>
                context.TypeInfo.Type == typeof(OpaqueNumber)
                    ? JsonContractTypeMapping.ContractType(typeof(int))
                    : context.TypeInfo.Type == typeof(OpaqueItems)
                        ? JsonContractTypeMapping.ContractType(typeof(int[]))
                        : JsonContractTypeMapping.ContractType(
                            typeof(Dictionary<string, int>)));
    }

    private static JsonSerializerOptions SurrogateSerializerOptions ()
    {
        var options = new JsonSerializerOptions
        {
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        };
        options.Converters.Add(new OpaqueNumberConverter());
        options.Converters.Add(new OpaqueItemsConverter());
        options.Converters.Add(new OpaquePropertiesConverter());
        return options;
    }

    private static JsonSerializerOptions OpaqueSerializerOptions ()
    {
        var options = new JsonSerializerOptions
        {
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        };
        options.Converters.Add(new OpaqueValueConverter());
        return options;
    }

    private sealed class OpaqueContract
    {
        public OpaqueValue Value { get; set; }
    }

    private sealed class BuiltInContract
    {
        public int Value { get; set; }
    }

    private sealed class SurrogateMappingContract
    {
        public OpaqueNumber Number { get; set; }

        public OpaqueItems Items { get; set; }

        public OpaqueProperties Properties { get; set; }
    }

    private sealed class NumberMappingContract
    {
        public OpaqueNumber Number { get; set; }
    }

    private readonly record struct OpaqueValue (string Value);

    private readonly record struct OpaqueNumber (int Value);

    private readonly record struct OpaqueItems (int[] Value);

    private readonly record struct OpaqueProperties (
        Dictionary<string, int> Value);

    private sealed class OpaqueValueConverter : JsonConverter<OpaqueValue>
    {
        public override OpaqueValue Read (
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            return new OpaqueValue(reader.GetString() ?? string.Empty);
        }

        public override void Write (
            Utf8JsonWriter writer,
            OpaqueValue value,
            JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.Value);
        }
    }

    private sealed class OpaqueNumberConverter : JsonConverter<OpaqueNumber>
    {
        public override OpaqueNumber Read (
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            return new OpaqueNumber(reader.GetInt32());
        }

        public override void Write (
            Utf8JsonWriter writer,
            OpaqueNumber value,
            JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value.Value);
        }
    }

    private sealed class OpaqueItemsConverter : JsonConverter<OpaqueItems>
    {
        public override OpaqueItems Read (
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            int[] value = JsonSerializer.Deserialize<int[]>(
                ref reader,
                options)
                ?? Array.Empty<int>();
            return new OpaqueItems(value);
        }

        public override void Write (
            Utf8JsonWriter writer,
            OpaqueItems value,
            JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value.Value, options);
        }
    }

    private sealed class OpaquePropertiesConverter
        : JsonConverter<OpaqueProperties>
    {
        public override OpaqueProperties Read (
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            Dictionary<string, int> value =
                JsonSerializer.Deserialize<Dictionary<string, int>>(
                    ref reader,
                    options)
                ?? new Dictionary<string, int>();
            return new OpaqueProperties(value);
        }

        public override void Write (
            Utf8JsonWriter writer,
            OpaqueProperties value,
            JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value.Value, options);
        }
    }

    private sealed class HandwrittenEnumContract
    {
        public HandwrittenState State { get; set; }
    }

    private sealed class VocabularyEnumContract
    {
        public VocabularySurrogateState State { get; set; }
    }

    [JsonConverter(typeof(HandwrittenStateConverter))]
    private enum HandwrittenState
    {
        Ready,

        Done,
    }

    [JsonConverter(typeof(VocabularySurrogateStateConverter))]
    [VocabularyDefinition]
    private enum VocabularySurrogateState
    {
        [VocabularyText("ready")]
        Ready,

        [VocabularyText("done")]
        Done,
    }

    private sealed class HandwrittenStateConverter
        : LowercaseEnumConverter<HandwrittenState>
    {
    }

    private sealed class VocabularySurrogateStateConverter
        : LowercaseEnumConverter<VocabularySurrogateState>
    {
    }

    private abstract class LowercaseEnumConverter<TState>
        : JsonConverter<TState>
        where TState : struct, Enum
    {
        public override TState Read (
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            string? text = reader.GetString();
            if (text is not null
                && Enum.TryParse(
                    text,
                    ignoreCase: true,
                    out TState value))
            {
                return value;
            }

            throw new JsonException(
                $"'{text}' is not a declared {typeof(TState).Name} value.");
        }

        public override void Write (
            Utf8JsonWriter writer,
            TState value,
            JsonSerializerOptions options)
        {
            writer.WriteStringValue(
                value.ToString().ToLowerInvariant());
        }
    }
}
