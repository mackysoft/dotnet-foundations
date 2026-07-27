using System.Text.Json;
using System.Text.Json.Serialization;
using MackySoft.JsonSchema.Generation.ContractModel;
using MackySoft.JsonSchema.Generation.Diagnostics;
using MackySoft.JsonSchema.Generation.Extensibility;
using MackySoft.JsonSchema.Generation.Metadata;
using MackySoft.JsonSchema.Generation.Projection;
using MackySoft.JsonSchema.Generation.Tests.Fixtures;
using Contract = MackySoft.JsonSchema.Generation.Annotations;

namespace MackySoft.JsonSchema.Generation.Tests.Generation;

public sealed class ContractValueValidationTests
{
    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenObjectConstantOmitsSerializerRequiredProperty_ReportsInvalidMetadata ()
    {
        JsonContractGenerationException exception =
            Assert.Throws<JsonContractGenerationException>(
                () => GenerationTestHarness.Generate<InvalidObjectConstant>(
                    "tests.invalid-object-constant"));

        Assert.Equal(
            JsonContractGenerationFailureKind.InvalidMetadataValue,
            exception.FailureKind);
        Assert.Equal(JsonContractMetadataKind.Const, exception.MetadataKind);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenConstantViolatesDeclaredConstraint_ReportsInvalidMetadata ()
    {
        JsonContractGenerationException exception =
            Assert.Throws<JsonContractGenerationException>(
                () => GenerationTestHarness.Generate<InvalidPatternConstant>(
                    "tests.invalid-pattern-constant"));

        Assert.Equal(
            JsonContractGenerationFailureKind.InvalidMetadataValue,
            exception.FailureKind);
        Assert.Equal(JsonContractMetadataKind.Const, exception.MetadataKind);
        Assert.Equal("Value", exception.JsonPropertyName);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenPatternIsNotEcmaScriptSyntax_ReportsInvalidMetadata ()
    {
        JsonContractGenerationException exception =
            Assert.Throws<JsonContractGenerationException>(
                () => GenerationTestHarness.Generate<InvalidPattern>(
                    "tests.invalid-pattern"));

        Assert.Equal(
            JsonContractGenerationFailureKind.InvalidMetadataValue,
            exception.FailureKind);
        Assert.Equal(JsonContractMetadataKind.Pattern, exception.MetadataKind);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenMetadataNumberWouldLoseRfc8785Identity_ReportsInvalidMetadata ()
    {
        JsonContractGenerationException exception =
            Assert.Throws<JsonContractGenerationException>(
                () => GenerationTestHarness.Generate<UnsafeNumberConstant>(
                    "tests.unsafe-number-constant"));

        Assert.Equal(
            JsonContractGenerationFailureKind.InvalidMetadataValue,
            exception.FailureKind);
        Assert.Equal(JsonContractMetadataKind.Const, exception.MetadataKind);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenMappedNumbersDifferBeyondBinary64Precision_ProducesDifferentDigests ()
    {
        JsonContractGenerationResult lower =
            GenerationTestHarness.Generate<MappedNumberContract>(
                "tests.exact-number-digest",
                typeMappers: new[]
                {
                    ExactNumberMapper(9_007_199_254_740_992L),
                });
        JsonContractGenerationResult upper =
            GenerationTestHarness.Generate<MappedNumberContract>(
                "tests.exact-number-digest",
                typeMappers: new[]
                {
                    ExactNumberMapper(9_007_199_254_740_993L),
                });

        Assert.NotEqual(lower.ContractDigest, upper.ContractDigest);
        Assert.NotEqual(
            lower.GetJsonSchemaUtf8(),
            upper.GetJsonSchemaUtf8());
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenLongConstantExceedsSerializerRange_ReportsInvalidMetadata ()
    {
        JsonContractGenerationException exception =
            Assert.Throws<JsonContractGenerationException>(
                () => GenerationTestHarness.Generate<OutOfRangeLongConstant>(
                    "tests.out-of-range-long-constant"));

        Assert.Equal(
            JsonContractGenerationFailureKind.InvalidMetadataValue,
            exception.FailureKind);
        Assert.Equal(JsonContractMetadataKind.Const, exception.MetadataKind);
        Assert.Equal("Value", exception.JsonPropertyName);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenUInt64ConstantExceedsSerializerRange_ReportsInvalidMetadata ()
    {
        JsonContractGenerationException exception =
            Assert.Throws<JsonContractGenerationException>(
                () => GenerationTestHarness.Generate<OutOfRangeUInt64Constant>(
                    "tests.out-of-range-uint64-constant"));

        Assert.Equal(
            JsonContractGenerationFailureKind.InvalidMetadataValue,
            exception.FailureKind);
        Assert.Equal(JsonContractMetadataKind.Const, exception.MetadataKind);
        Assert.Equal("Value", exception.JsonPropertyName);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenDecimalConstantExceedsSerializerRange_ReportsInvalidMetadata ()
    {
        JsonContractGenerationException exception =
            Assert.Throws<JsonContractGenerationException>(
                () => GenerationTestHarness.Generate<OutOfRangeDecimalConstant>(
                    "tests.out-of-range-decimal-constant"));

        Assert.Equal(
            JsonContractGenerationFailureKind.InvalidMetadataValue,
            exception.FailureKind);
        Assert.Equal(typeof(decimal), exception.TargetType);
        Assert.Equal(JsonContractMetadataKind.Const, exception.MetadataKind);
        Assert.Equal("Value", exception.JsonPropertyName);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenDecimalEnumExtendsBelowSerializerRange_ReportsInvalidMetadata ()
    {
        JsonContractGenerationException exception =
            Assert.Throws<JsonContractGenerationException>(
                () => GenerationTestHarness.Generate<OutOfRangeDecimalEnum>(
                    "tests.out-of-range-decimal-enum"));

        Assert.Equal(
            JsonContractGenerationFailureKind.InvalidMetadataValue,
            exception.FailureKind);
        Assert.Equal(typeof(decimal), exception.TargetType);
        Assert.Equal(JsonContractMetadataKind.EnumValue, exception.MetadataKind);
        Assert.Equal("Value", exception.JsonPropertyName);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenFiniteIntegerMetadataUsesNonIntegerLexemes_ReportsInvalidMetadata ()
    {
        JsonContractGenerationException constantException =
            Assert.Throws<JsonContractGenerationException>(
                () => GenerationTestHarness.Generate<FractionalIntegerConstant>(
                    "tests.fractional-integer-constant"));
        JsonContractGenerationException enumException =
            Assert.Throws<JsonContractGenerationException>(
                () => GenerationTestHarness.Generate<ExponentIntegerEnum>(
                    "tests.exponent-integer-enum"));

        Assert.Equal(
            JsonContractGenerationFailureKind.InvalidMetadataValue,
            constantException.FailureKind);
        Assert.Equal(
            JsonContractMetadataKind.Const,
            constantException.MetadataKind);
        Assert.Equal(
            JsonContractGenerationFailureKind.InvalidMetadataValue,
            enumException.FailureKind);
        Assert.Equal(
            JsonContractMetadataKind.EnumValue,
            enumException.MetadataKind);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenStructuredConstantsTraverseRecursiveDefinitions_ProjectsExactValues ()
    {
        JsonContractGenerationResult result =
            GenerationTestHarness.Generate<StructuredConstantContract>(
                "tests.structured-constants");

        JsonContractNode head = GenerationTestHarness
            .GetProperty(result.Model.Root, "Head")
            .Value;
        JsonContractNode items = GenerationTestHarness
            .GetProperty(result.Model.Root, "Items")
            .Value;
        Assert.Equal(JsonContractNodeKind.Const, head.Kind);
        Assert.Equal(JsonContractNodeKind.Const, items.Kind);
        Assert.Equal(
            2,
            head.Constant?.GetProperty("Next")
                .GetProperty("Value")
                .GetInt32());
        Assert.Equal(
            3,
            items.Constant?[0]
                .GetProperty("Value")
                .GetInt32());

        using JsonDocument schema = JsonDocument.Parse(
            result.GetJsonSchemaUtf8());
        Assert.Equal(
            2,
            schema.RootElement
                .GetProperty("properties")
                .GetProperty("Head")
                .GetProperty("const")
                .GetProperty("Next")
                .GetProperty("Value")
                .GetInt32());

        using JsonDocument typeMetadata = JsonDocument.Parse(
            result.GetTypeMetadataUtf8());
        JsonElement metadataRoot = typeMetadata.RootElement.GetProperty("root");
        Assert.Equal(
            3,
            GenerationTestHarness.GetTypeMetadataProperty(
                    metadataRoot,
                    "Items")
                .GetProperty("value")
                .GetProperty("constant")[0]
                .GetProperty("Value")
                .GetInt32());
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenRootConstantContainsReferencedObject_ProjectsConstant ()
    {
        JsonContractGenerationResult result =
            GenerationTestHarness.Generate<RootStructuredConstant>(
                "tests.root-structured-constant");

        Assert.Equal(JsonContractNodeKind.Const, result.Model.Root.Kind);
        Assert.Equal(
            1,
            result.Model.Root.Constant?
                .GetProperty("Head")
                .GetProperty("Value")
                .GetInt32());

        using JsonDocument schema = JsonDocument.Parse(
            result.GetJsonSchemaUtf8());
        Assert.Equal(
            1,
            schema.RootElement
                .GetProperty("const")
                .GetProperty("Head")
                .GetProperty("Value")
                .GetInt32());
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenStructuredAndMixedEnumsMatchSerializerShapes_ProjectsFiniteValues ()
    {
        JsonContractGenerationResult result =
            GenerationTestHarness.Generate<StructuredEnumContract>(
                "tests.structured-enums");

        JsonContractNode choice = GenerationTestHarness
            .GetProperty(result.Model.Root, "Choice")
            .Value;
        JsonContractNode mixed = GenerationTestHarness
            .GetProperty(result.Model.Root, "Mixed")
            .Value;
        Assert.Equal(JsonContractNodeKind.Enum, choice.Kind);
        Assert.Null(choice.ScalarKind);
        Assert.Equal(2, choice.AllowedValues.Count);
        Assert.Equal(JsonContractNodeKind.Enum, mixed.Kind);
        Assert.Null(mixed.ScalarKind);
        Assert.Equal(4, mixed.AllowedValues.Count);

        using JsonDocument schema = JsonDocument.Parse(
            result.GetJsonSchemaUtf8());
        JsonElement properties = schema.RootElement.GetProperty("properties");
        Assert.False(
            properties.GetProperty("Choice").TryGetProperty("type", out _));
        Assert.Equal(
            2,
            properties.GetProperty("Choice")
                .GetProperty("enum")
                .GetArrayLength());
        Assert.False(
            properties.GetProperty("Mixed").TryGetProperty("type", out _));
        Assert.Equal(
            4,
            properties.GetProperty("Mixed")
                .GetProperty("enum")
                .GetArrayLength());

        using JsonDocument typeMetadata = JsonDocument.Parse(
            result.GetTypeMetadataUtf8());
        JsonElement choiceMetadata = GenerationTestHarness
            .GetTypeMetadataProperty(
                typeMetadata.RootElement.GetProperty("root"),
                "Choice")
            .GetProperty("value");
        Assert.Equal(JsonValueKind.Null, choiceMetadata
            .GetProperty("scalarKind")
            .ValueKind);
        Assert.Equal(
            2,
            choiceMetadata.GetProperty("allowedValues").GetArrayLength());
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenStructuredEnumViolatesSerializerShape_ReportsInvalidMetadata ()
    {
        JsonContractGenerationException exception =
            Assert.Throws<JsonContractGenerationException>(
                () => GenerationTestHarness.Generate<InvalidStructuredEnum>(
                    "tests.invalid-structured-enum"));

        Assert.Equal(
            JsonContractGenerationFailureKind.InvalidMetadataValue,
            exception.FailureKind);
        Assert.Equal(JsonContractMetadataKind.EnumValue, exception.MetadataKind);
        Assert.Equal("Choice", exception.JsonPropertyName);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenConstAndOneOfAreDeclaredTogether_ReportsMetadataConflict ()
    {
        JsonContractGenerationException exception =
            Assert.Throws<JsonContractGenerationException>(
                () => GenerationTestHarness.Generate<ConstOneOfConflict>(
                    "tests.const-oneof-conflict"));

        Assert.Equal(
            JsonContractGenerationFailureKind.ConflictingMetadata,
            exception.FailureKind);
        Assert.Equal(typeof(ConstOneOfConflict), exception.TargetType);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenEnumAndDiscriminatorAreDeclaredTogether_ReportsMetadataConflict ()
    {
        JsonContractGenerationException exception =
            Assert.Throws<JsonContractGenerationException>(
                () => GenerationTestHarness.Generate<EnumDiscriminatorConflict>(
                    "tests.enum-discriminator-conflict"));

        Assert.Equal(
            JsonContractGenerationFailureKind.ConflictingMetadata,
            exception.FailureKind);
        Assert.Equal(typeof(EnumDiscriminatorConflict), exception.TargetType);
    }

    [Theory]
    [InlineData("not a uri")]
    [InlineData("https://schemas.example.test/contract#branch")]
    public void CreateDocumentOptions_WhenIdIsNotValidSchemaResourceIdentifier_Throws (
        string id)
    {
        Assert.Throws<ArgumentException>(
            () => new JsonSchemaDocumentOptions(
                JsonSchemaDocumentKind.Complete,
                id,
                logicalName: null));
    }

    private static TestTypeMapper ExactNumberMapper (long value)
    {
        return new TestTypeMapper(
            "tests.mapper.exact-number",
            static context => context.TargetType == typeof(MappedNumber),
            _ => JsonContractTypeMapping.Enum(
                JsonContractScalarKind.Integer,
                JsonSerializer.SerializeToElement(value)));
    }

    [Contract.Const("{}")]
    private sealed class InvalidObjectConstant
    {
        [JsonRequired]
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    private sealed class InvalidPatternConstant
    {
        [Contract.Const("\"123\"")]
        [Contract.Pattern("^[a-z]+$")]
        public string Value { get; set; } = string.Empty;
    }

    private sealed class InvalidPattern
    {
        [Contract.Pattern("[")]
        public string Value { get; set; } = string.Empty;
    }

    private sealed class UnsafeNumberConstant
    {
        [Contract.Const("9007199254740993")]
        public long Value { get; set; }
    }

    private sealed class OutOfRangeLongConstant
    {
        [Contract.Const("9223372036854776000")]
        public long Value { get; set; }
    }

    private sealed class OutOfRangeUInt64Constant
    {
        [Contract.Const("18446744073709552000")]
        public ulong Value { get; set; }
    }

    private sealed class OutOfRangeDecimalConstant
    {
        [Contract.Const("8e28")]
        public decimal Value { get; set; }
    }

    private sealed class OutOfRangeDecimalEnum
    {
        [Contract.Enum("-8e28", "0")]
        public decimal Value { get; set; }
    }

    private sealed class FractionalIntegerConstant
    {
        [Contract.Const("1.0")]
        public int Value { get; set; }
    }

    private sealed class ExponentIntegerEnum
    {
        [Contract.Enum("1e0", "2")]
        public int Value { get; set; }
    }

    private sealed class StructuredConstantContract
    {
        [Contract.Const(
            """{"Value":1,"Next":{"Value":2,"Next":null}}""")]
        public RecursiveValue Head { get; set; } = new();

        [Contract.Const("""[{"Value":3,"Next":null}]""")]
        public RecursiveValue[] Items { get; set; } =
            Array.Empty<RecursiveValue>();
    }

    [Contract.Const("""{"Head":{"Value":1,"Next":null}}""")]
    private sealed class RootStructuredConstant
    {
        public RecursiveValue Head { get; set; } = new();
    }

    private sealed class StructuredEnumContract
    {
        [Contract.Enum(
            """{"Value":1,"Next":null}""",
            """{"Value":2,"Next":null}""")]
        public RecursiveValue Choice { get; set; } = new();

        [Contract.Enum(
            "1",
            "\"one\"",
            """{"kind":"object"}""",
            "[true]")]
        public object Mixed { get; set; } = new();
    }

    private sealed class InvalidStructuredEnum
    {
        [Contract.Enum("""{"Unknown":1}""")]
        public RecursiveValue Choice { get; set; } = new();
    }

    private sealed class RecursiveValue
    {
        public int Value { get; set; }

        public RecursiveValue? Next { get; set; }
    }

    [Contract.Const("""{"Value":1}""")]
    [Contract.OneOfBranch("value", "Value")]
    private sealed class ConstOneOfConflict
    {
        public int Value { get; set; }
    }

    [Contract.Enum("""{"Kind":"alpha"}""")]
    [Contract.Discriminator("Kind")]
    [Contract.OneOfBranch(
        "alpha",
        "Kind",
        DiscriminatorValueJson = "\"alpha\"")]
    private sealed class EnumDiscriminatorConflict
    {
        public string Kind { get; set; } = string.Empty;
    }

    private sealed class MappedNumberContract
    {
        public MappedNumber Value { get; set; }
    }

    [JsonConverter(typeof(MappedNumberConverter))]
    private readonly record struct MappedNumber (long Value);

    private sealed class MappedNumberConverter
        : JsonConverter<MappedNumber>
    {
        public override MappedNumber Read (
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            return new MappedNumber(reader.GetInt64());
        }

        public override void Write (
            Utf8JsonWriter writer,
            MappedNumber value,
            JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value.Value);
        }
    }
}
