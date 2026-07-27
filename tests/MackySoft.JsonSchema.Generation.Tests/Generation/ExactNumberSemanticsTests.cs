using System.Text.Json;
using System.Text.Json.Serialization;
using MackySoft.JsonSchema.Generation.Annotations;
using MackySoft.JsonSchema.Generation.ContractModel;
using MackySoft.JsonSchema.Generation.Diagnostics;
using MackySoft.JsonSchema.Generation.Extensibility;
using MackySoft.JsonSchema.Generation.Tests.Fixtures;

namespace MackySoft.JsonSchema.Generation.Tests.Generation;

public sealed class ExactNumberSemanticsTests
{
    [Theory]
    [InlineData("-0", "0")]
    [InlineData(
        "0e999999999999999999999999999999",
        "0.000e-999999999999999999999999999999")]
    [InlineData("1.23000e+2", "123")]
    [InlineData("-0.0012300E+5", "-123")]
    [Trait("Size", "Small")]
    public void Generate_WhenNumberSpellingsHaveSameExactValue_ProducesSameContractDigest (
        string leftJson,
        string rightJson)
    {
        JsonContractGenerationResult left =
            GenerationTestHarness.Generate<MappedNumberContract>(
                "tests.equivalent-number-spellings",
                typeMappers: new[] { ExactNumberMapper(leftJson) });
        JsonContractGenerationResult right =
            GenerationTestHarness.Generate<MappedNumberContract>(
                "tests.equivalent-number-spellings",
                typeMappers: new[] { ExactNumberMapper(rightJson) });

        Assert.Equal(left.ContractDigest, right.ContractDigest);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenDecimalConstantHasArbitrarilyLargeExponent_ReportsInvalidMetadata ()
    {
        JsonContractGenerationException exception =
            Assert.Throws<JsonContractGenerationException>(
                () => GenerationTestHarness.Generate<
                    HugeExponentDecimalConstant>(
                        "tests.huge-exponent-decimal"));

        Assert.Equal(
            JsonContractGenerationFailureKind.InvalidMetadataValue,
            exception.FailureKind);
        Assert.Equal(typeof(decimal), exception.TargetType);
    }

    private static TestTypeMapper ExactNumberMapper (string valueJson)
    {
        using JsonDocument document = JsonDocument.Parse(valueJson);
        JsonElement value = document.RootElement.Clone();
        return new TestTypeMapper(
            "tests.mapper.equivalent-number-spellings",
            static context => context.TargetType == typeof(MappedNumber),
            _ => JsonContractTypeMapping.Enum(
                JsonContractScalarKind.Number,
                value));
    }

    private sealed class HugeExponentDecimalConstant
    {
        [Const("1e999999999999999999999999999999")]
        public decimal Value { get; set; }
    }

    private sealed class MappedNumberContract
    {
        public MappedNumber Value { get; set; }
    }

    [JsonConverter(typeof(MappedNumberConverter))]
    private readonly record struct MappedNumber (decimal Value);

    private sealed class MappedNumberConverter
        : JsonConverter<MappedNumber>
    {
        public override MappedNumber Read (
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            return new MappedNumber(reader.GetDecimal());
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
