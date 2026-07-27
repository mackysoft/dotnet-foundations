using System.Text.Json;
using System.Text.Json.Serialization;
using MackySoft.JsonSchema.Generation.Configuration;
using MackySoft.JsonSchema.Generation.ContractModel;
using MackySoft.JsonSchema.Generation.Diagnostics;
using MackySoft.JsonSchema.Generation.Extensibility;
using MackySoft.JsonSchema.Generation.Projection;
using MackySoft.JsonSchema.Generation.Tests.Fixtures;
using MackySoft.Text.Vocabularies;
using MackySoft.Text.Vocabularies.Json;

namespace MackySoft.JsonSchema.Generation.Tests.Generation;

public sealed class JsonContractVocabularyTests
{
    [Fact]
    [Trait("Size", "Small")]
    public void PublicWireEnums_DeclareCanonicalTextsThroughVocabulary ()
    {
        Assert.Equal(
            new[]
            {
                "arbitrary",
                "scalar",
                "array",
                "object",
                "dictionary",
                "enum",
                "const",
                "reference",
                "oneOf",
            },
            Vocabulary.GetTexts<JsonContractNodeKind>());
        Assert.Equal(
            new[] { "null", "boolean", "integer", "number", "string" },
            Vocabulary.GetTexts<JsonContractScalarKind>());
        Assert.Equal(
            new[]
            {
                "arbitrary",
                "scalar",
                "textVocabulary",
                "contractType",
            },
            Vocabulary.GetTexts<JsonContractTypeMappingKind>());
        Assert.Equal(
            new[]
            {
                "allowAdditionalProperties",
                "disallowAdditionalProperties",
                "disallowUnevaluatedProperties",
            },
            Vocabulary.GetTexts<JsonObjectClosure>());
        Assert.Equal(
            new[] { "typeUnion" },
            Vocabulary.GetTexts<JsonNullabilityProjection>());
        Assert.Equal(
            new[] { "localDefinitions" },
            Vocabulary.GetTexts<JsonReferenceProjection>());
        Assert.Equal(
            new[] { "complete", "fragment" },
            Vocabulary.GetTexts<JsonSchemaDocumentKind>());
        Assert.False(
            Vocabulary.IsVocabulary(typeof(JsonContractGenerationFailureKind)));
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenEnumsUseDifferentSerializerContracts_PreservesNumericAcceptanceAndCanonicalTextValues ()
    {
        var serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            Converters =
            {
                new VocabularyJsonConverterFactory(),
            },
        };

        JsonContractGenerationResult result =
            GenerationTestHarness.Generate<EnumContract>(
                "tests.enum-contract",
                serializerOptions,
                typeMappers: new[]
                {
                    new DeclaredTextVocabularyTypeMapper(),
                });

        JsonContractNode numeric = GenerationTestHarness
            .GetProperty(result.Model.Root, "numeric")
            .Value;
        Assert.Equal(JsonContractNodeKind.Scalar, numeric.Kind);
        Assert.Equal(JsonContractScalarKind.Integer, numeric.ScalarKind);
        Assert.Empty(numeric.AllowedValues);
        Assert.Equal(
            int.MinValue.ToString(System.Globalization.CultureInfo.InvariantCulture),
            numeric.Constraints.Minimum?.GetRawText());
        Assert.Equal(
            int.MaxValue.ToString(System.Globalization.CultureInfo.InvariantCulture),
            numeric.Constraints.Maximum?.GetRawText());

        JsonContractNode flags = GenerationTestHarness
            .GetProperty(result.Model.Root, "flags")
            .Value;
        Assert.Equal(JsonContractNodeKind.Scalar, flags.Kind);
        Assert.Equal(JsonContractScalarKind.Integer, flags.ScalarKind);
        Assert.Empty(flags.AllowedValues);
        Assert.Equal("0", flags.Constraints.Minimum?.GetRawText());
        Assert.Equal("255", flags.Constraints.Maximum?.GetRawText());

        JsonContractNode textual = GenerationTestHarness
            .GetProperty(result.Model.Root, "textual")
            .Value;
        Assert.Equal(JsonContractNodeKind.Enum, textual.Kind);
        Assert.Equal(JsonContractScalarKind.String, textual.ScalarKind);
        Assert.Equal(
            new[] { "doneValue", "waiting" },
            textual.AllowedValues.Select(static value => value.GetString()));

        using JsonDocument schema = JsonDocument.Parse(result.GetJsonSchemaUtf8());
        JsonElement properties = schema.RootElement.GetProperty("properties");
        Assert.False(
            properties.GetProperty("numeric").TryGetProperty("enum", out _));
        Assert.Equal(
            int.MinValue,
            properties
                .GetProperty("numeric")
                .GetProperty("minimum")
                .GetInt32());
        Assert.Equal(
            int.MaxValue,
            properties
                .GetProperty("numeric")
                .GetProperty("maximum")
                .GetInt32());
        Assert.Equal(
            new[] { "doneValue", "waiting" },
            properties
                .GetProperty("textual")
                .GetProperty("enum")
                .EnumerateArray()
                .Select(static value => value.GetString()));
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenPropertyConverterWritesNonVocabularyEnumText_ReportsUnsupportedConverter ()
    {
        JsonContractGenerationException exception =
            Assert.Throws<JsonContractGenerationException>(
                () => GenerationTestHarness.Generate<
                    PropertyStringEnumContract>(
                        "tests.property-string-enum"));

        Assert.Equal(
            JsonContractGenerationFailureKind.UnsupportedConverter,
            exception.FailureKind);
        Assert.Equal(typeof(NumericState), exception.TargetType);
        Assert.Equal("Value", exception.JsonPropertyName);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenPropertyConverterWritesVocabularyCanonicalText_UsesFiniteTextContract ()
    {
        JsonContractGenerationResult result =
            GenerationTestHarness.Generate<
                PropertyVocabularyEnumContract>(
                    "tests.property-vocabulary-enum",
                    typeMappers: new[]
                    {
                        new DeclaredTextVocabularyTypeMapper(),
                    });

        JsonContractNode value = GenerationTestHarness
            .GetProperty(result.Model.Root, "Value")
            .Value;
        Assert.Equal(JsonContractNodeKind.Enum, value.Kind);
        Assert.Equal(JsonContractScalarKind.String, value.ScalarKind);
        Assert.Equal(
            new[] { "doneValue", "waiting" },
            value.AllowedValues.Select(static item => item.GetString()));
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenTextVocabularyHasOneEntry_ProjectsCanonicalTextAsConstant ()
    {
        var serializerOptions = new JsonSerializerOptions
        {
            UnmappedMemberHandling =
                JsonUnmappedMemberHandling.Disallow,
            Converters =
            {
                new VocabularyJsonConverterFactory(),
            },
        };

        JsonContractGenerationResult result =
            GenerationTestHarness.Generate<SingleTextContract>(
                "tests.single-text-vocabulary",
                serializerOptions,
                typeMappers: new[]
                {
                    new DeclaredTextVocabularyTypeMapper(),
                });

        JsonContractNode value = GenerationTestHarness
            .GetProperty(result.Model.Root, "Value")
            .Value;
        Assert.Equal(JsonContractNodeKind.Const, value.Kind);
        Assert.Equal(JsonContractScalarKind.String, value.ScalarKind);
        Assert.Equal("only", value.Constant?.GetString());
        Assert.Empty(value.AllowedValues);

        using JsonDocument schema = JsonDocument.Parse(
            result.GetJsonSchemaUtf8());
        JsonElement valueSchema = schema.RootElement
            .GetProperty("properties")
            .GetProperty("Value");
        Assert.Equal("only", valueSchema.GetProperty("const").GetString());
        Assert.False(valueSchema.TryGetProperty("enum", out _));
    }

    [Theory]
    [InlineData(typeof(ReadAllVocabularyConverter))]
    [InlineData(typeof(ReadNoneVocabularyConverter))]
    public void Generate_WhenUnknownCustomConverterWritesVocabularyTexts_ReportsUnsupportedConverter (
        Type converterType)
    {
        var serializerOptions = new JsonSerializerOptions
        {
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        };
        serializerOptions.Converters.Add(
            Assert.IsAssignableFrom<JsonConverter>(
                Activator.CreateInstance(converterType)));

        JsonContractGenerationException exception =
            Assert.Throws<JsonContractGenerationException>(
                () => GenerationTestHarness.Generate<
                    VocabularyWithoutAdapterContract>(
                        "tests.unknown-vocabulary-converter",
                        serializerOptions));

        Assert.Equal(
            JsonContractGenerationFailureKind.UnsupportedConverter,
            exception.FailureKind);
        Assert.Equal(typeof(TextState), exception.TargetType);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenPropertyConverterOverridesGlobalVocabularyAdapter_DoesNotUseVocabularyMapper ()
    {
        var serializerOptions = new JsonSerializerOptions
        {
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            Converters =
            {
                new VocabularyJsonConverterFactory(),
            },
        };

        JsonContractGenerationException exception =
            Assert.Throws<JsonContractGenerationException>(
                () => GenerationTestHarness.Generate<
                    PropertyUnknownVocabularyOverrideContract>(
                        "tests.property-vocabulary-override",
                        serializerOptions,
                        typeMappers: new[]
                        {
                            new DeclaredTextVocabularyTypeMapper(),
                        }));

        Assert.Equal(
            JsonContractGenerationFailureKind.UnsupportedConverter,
            exception.FailureKind);
        Assert.Equal(typeof(TextState), exception.TargetType);
        Assert.Equal("Value", exception.JsonPropertyName);
        Assert.Empty(exception.SourceIds);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenCustomNumericEnumConverterIsGlobal_ReportsUnsupportedConverter ()
    {
        var serializerOptions = new JsonSerializerOptions();
        serializerOptions.Converters.Add(new NumericStateConverter());

        JsonContractGenerationException exception =
            Assert.Throws<JsonContractGenerationException>(
                () => GenerationTestHarness.Generate<
                    NonVocabularyStringEnumContract>(
                        "tests.global-numeric-enum-converter",
                        serializerOptions));

        Assert.Equal(
            JsonContractGenerationFailureKind.UnsupportedConverter,
            exception.FailureKind);
        Assert.Equal(typeof(NumericState), exception.TargetType);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenCustomNumericEnumConverterIsOnProperty_ReportsUnsupportedConverter ()
    {
        JsonContractGenerationException exception =
            Assert.Throws<JsonContractGenerationException>(
                () => GenerationTestHarness.Generate<
                    PropertyNumericEnumContract>(
                        "tests.property-numeric-enum-converter"));

        Assert.Equal(
            JsonContractGenerationFailureKind.UnsupportedConverter,
            exception.FailureKind);
        Assert.Equal(typeof(NumericState), exception.TargetType);
        Assert.Equal("Value", exception.JsonPropertyName);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenNonVocabularyEnumUsesStrings_ReportsUnsupportedConverter ()
    {
        var serializerOptions = new JsonSerializerOptions();
        serializerOptions.Converters.Add(
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

        JsonContractGenerationException exception =
            Assert.Throws<JsonContractGenerationException>(
                () => GenerationTestHarness.Generate<
                    NonVocabularyStringEnumContract>(
                        "tests.non-vocabulary-string-enum",
                        serializerOptions));

        Assert.Equal(
            JsonContractGenerationFailureKind.UnsupportedConverter,
            exception.FailureKind);
        Assert.Equal(typeof(NumericState), exception.TargetType);
        Assert.Contains(
            "MackySoft.Text.Vocabularies",
            exception.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenVocabularyConverterHasNoExplicitMapper_ReportsUnsupportedConverter ()
    {
        JsonContractGenerationException exception =
            Assert.Throws<JsonContractGenerationException>(
                () => GenerationTestHarness.Generate<
                    VocabularyWithoutAdapterContract>(
                        "tests.vocabulary-without-adapter"));

        Assert.Equal(
            JsonContractGenerationFailureKind.UnsupportedConverter,
            exception.FailureKind);
        Assert.Equal(typeof(TextState), exception.TargetType);
        Assert.Contains(
            "explicitly registered type mapper",
            exception.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenTextVocabularyMapperReplacesNumericSerializerContract_ReportsUnsupportedConverter ()
    {
        JsonContractGenerationException exception =
            Assert.Throws<JsonContractGenerationException>(
                () => GenerationTestHarness.Generate<
                    VocabularyWithoutAdapterContract>(
                        "tests.vocabulary-mapper-without-adapter",
                        typeMappers: new[]
                        {
                            new UnconditionalTextVocabularyTypeMapper(),
                        }));

        Assert.Equal(
            JsonContractGenerationFailureKind.UnsupportedConverter,
            exception.FailureKind);
        Assert.Equal(typeof(TextState), exception.TargetType);
        Assert.Equal("Value", exception.JsonPropertyName);
        Assert.Equal(
            new[] { "tests.unconditional-text-vocabulary" },
            exception.SourceIds);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenTextVocabularyMapperClaimsMismatchedCustomConverter_ReportsUnsupportedConverter ()
    {
        var serializerOptions = new JsonSerializerOptions
        {
            UnmappedMemberHandling =
                JsonUnmappedMemberHandling.Disallow,
            Converters =
            {
                new ReadAllVocabularyConverter(),
            },
        };

        JsonContractGenerationException exception =
            Assert.Throws<JsonContractGenerationException>(
                () => GenerationTestHarness.Generate<
                    VocabularyWithoutAdapterContract>(
                        "tests.mismatched-vocabulary-converter",
                        serializerOptions,
                        typeMappers: new[]
                        {
                            new UnconditionalTextVocabularyTypeMapper(),
                        }));

        Assert.Equal(
            JsonContractGenerationFailureKind.UnsupportedConverter,
            exception.FailureKind);
        Assert.Equal(typeof(TextState), exception.TargetType);
        Assert.Equal("Value", exception.JsonPropertyName);
        Assert.Equal(
            new[] { "tests.unconditional-text-vocabulary" },
            exception.SourceIds);
    }

    private sealed class EnumContract
    {
        public NumericState Numeric { get; set; }

        public NumericFlags Flags { get; set; }

        public TextState Textual { get; set; }
    }

    private sealed class NonVocabularyStringEnumContract
    {
        public NumericState Value { get; set; }
    }

    private sealed class VocabularyWithoutAdapterContract
    {
        public TextState Value { get; set; }
    }

    private sealed class SingleTextContract
    {
        public SingleTextState Value { get; set; }
    }

    private sealed class PropertyStringEnumContract
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public NumericState Value { get; set; }
    }

    private sealed class PropertyVocabularyEnumContract
    {
        [JsonConverter(typeof(VocabularyJsonConverterFactory))]
        public TextState Value { get; set; }
    }

    private sealed class PropertyNumericEnumContract
    {
        [JsonConverter(typeof(NumericStateConverter))]
        public NumericState Value { get; set; }
    }

    private sealed class PropertyUnknownVocabularyOverrideContract
    {
        [JsonConverter(typeof(ReadAllVocabularyConverter))]
        public TextState Value { get; set; }
    }

    private enum NumericState
    {
        Waiting = 3,

        Done = 7,
    }

    [Flags]
    private enum NumericFlags : byte
    {
        Read = 1,

        Write = 2,
    }

    [VocabularyDefinition]
    private enum TextState
    {
        [VocabularyText("waiting")]
        Waiting,

        [VocabularyText("doneValue")]
        Done,
    }

    [VocabularyDefinition]
    private enum SingleTextState
    {
        [VocabularyText("only")]
        Only,
    }

    private sealed class NumericStateConverter : JsonConverter<NumericState>
    {
        public override NumericState Read (
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            return (NumericState)reader.GetInt32();
        }

        public override void Write (
            Utf8JsonWriter writer,
            NumericState value,
            JsonSerializerOptions options)
        {
            writer.WriteNumberValue((int)value);
        }
    }

    private sealed class ReadAllVocabularyConverter : JsonConverter<TextState>
    {
        public override TextState Read (
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            reader.Skip();
            return TextState.Waiting;
        }

        public override void Write (
            Utf8JsonWriter writer,
            TextState value,
            JsonSerializerOptions options)
        {
            writer.WriteStringValue(Vocabulary.GetText(value));
        }
    }

    private sealed class ReadNoneVocabularyConverter : JsonConverter<TextState>
    {
        public override TextState Read (
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            throw new JsonException("No vocabulary text is accepted.");
        }

        public override void Write (
            Utf8JsonWriter writer,
            TextState value,
            JsonSerializerOptions options)
        {
            writer.WriteStringValue(Vocabulary.GetText(value));
        }
    }

    private sealed class DeclaredTextVocabularyTypeMapper
        : IJsonContractTypeMapper
    {
        public string StableId => "tests.text-vocabulary";

        public string ContractVersion => "1";

        public bool CanMap (JsonContractTypeMapperContext context)
        {
            return Vocabulary.IsVocabulary(context.TypeInfo.Type)
                && IsVocabularyAdapter(
                    context.PropertyInfo?.CustomConverter
                        ?? context.TypeInfo.Converter);
        }

        public JsonContractTypeMapping Map (
            JsonContractTypeMapperContext context)
        {
            return JsonContractTypeMapping.TextVocabulary();
        }

        private static bool IsVocabularyAdapter (JsonConverter? converter)
        {
            return converter is VocabularyJsonConverterFactory
                || converter?.GetType().DeclaringType
                    == typeof(VocabularyJsonConverterFactory);
        }
    }

    private sealed class UnconditionalTextVocabularyTypeMapper
        : IJsonContractTypeMapper
    {
        public string StableId =>
            "tests.unconditional-text-vocabulary";

        public string ContractVersion => "1";

        public bool CanMap (JsonContractTypeMapperContext context)
        {
            return Vocabulary.IsVocabulary(context.TypeInfo.Type);
        }

        public JsonContractTypeMapping Map (
            JsonContractTypeMapperContext context)
        {
            return JsonContractTypeMapping.TextVocabulary();
        }
    }
}
