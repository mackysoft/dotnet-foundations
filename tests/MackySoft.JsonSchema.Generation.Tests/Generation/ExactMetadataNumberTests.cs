using System.Globalization;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using MackySoft.JsonSchema.Generation.ContractModel;
using MackySoft.JsonSchema.Generation.Extensibility;
using MackySoft.JsonSchema.Generation.Metadata;
using MackySoft.JsonSchema.Generation.Tests.Fixtures;

namespace MackySoft.JsonSchema.Generation.Tests.Generation;

public sealed class ExactMetadataNumberTests
{
    [Fact]
    [Trait("Size", "Small")]
    public void Generate_AdjacentSafeIntegerBoundaryBounds_HaveDistinctSchemaBytesAndDigests ()
    {
        JsonContractGenerationResult safeInteger = Generate(
            static builder => builder.SetMinimum(
                JsonContractNumber.FromInt64(9_007_199_254_740_992)));
        JsonContractGenerationResult adjacentInteger = Generate(
            static builder => builder.SetMinimum(
                JsonContractNumber.FromInt64(9_007_199_254_740_993)));

        Assert.Equal(
            "9007199254740992",
            GetSchemaKeyword(safeInteger, "minimum").GetRawText());
        Assert.Equal(
            "9007199254740993",
            GetSchemaKeyword(adjacentInteger, "minimum").GetRawText());
        Assert.NotEqual(
            safeInteger.ContractDigest,
            adjacentInteger.ContractDigest);
        Assert.False(
            safeInteger
                .GetJsonSchemaUtf8()
                .SequenceEqual(adjacentInteger.GetJsonSchemaUtf8()));
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_AdjacentUInt64Bounds_HaveDistinctSchemaBytesAndDigests ()
    {
        JsonContractGenerationResult lower = Generate(
            static builder => builder.SetMaximum(
                JsonContractNumber.FromUInt64(ulong.MaxValue - 1)));
        JsonContractGenerationResult upper = Generate(
            static builder => builder.SetMaximum(
                JsonContractNumber.FromUInt64(ulong.MaxValue)));

        Assert.Equal(
            "18446744073709551614",
            GetSchemaKeyword(lower, "maximum").GetRawText());
        Assert.Equal(
            "18446744073709551615",
            GetSchemaKeyword(upper, "maximum").GetRawText());
        Assert.NotEqual(lower.ContractDigest, upper.ContractDigest);
        Assert.False(
            lower
                .GetJsonSchemaUtf8()
                .SequenceEqual(upper.GetJsonSchemaUtf8()));
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_EquivalentDecimalScaleBounds_HaveIdenticalSchemaBytesAndDigests ()
    {
        JsonContractGenerationResult decimalScale = Generate(
            static builder => builder.SetMinimum(
                JsonContractNumber.FromDecimal(123.4500m)));
        JsonContractGenerationResult normalizedToken = Generate(
            static builder => builder.SetMinimum(
                JsonContractNumber.Parse("123.45")));

        Assert.Equal(
            "123.45",
            GetSchemaKeyword(decimalScale, "minimum").GetRawText());
        Assert.Equal(
            decimalScale.ContractDigest,
            normalizedToken.ContractDigest);
        Assert.True(
            decimalScale
                .GetJsonSchemaUtf8()
                .SequenceEqual(normalizedToken.GetJsonSchemaUtf8()));
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_AdjacentDecimalBounds_HaveDistinctSchemaBytesAndDigests ()
    {
        JsonContractGenerationResult lower = Generate(
            static builder => builder.SetMinimum(
                JsonContractNumber.FromDecimal(
                    1234567890.123456788m)));
        JsonContractGenerationResult upper = Generate(
            static builder => builder.SetMinimum(
                JsonContractNumber.FromDecimal(
                    1234567890.123456789m)));

        Assert.Equal(
            "1234567890.123456788",
            GetSchemaKeyword(lower, "minimum").GetRawText());
        Assert.Equal(
            "1234567890.123456789",
            GetSchemaKeyword(upper, "minimum").GetRawText());
        Assert.NotEqual(lower.ContractDigest, upper.ContractDigest);
        Assert.False(
            lower
                .GetJsonSchemaUtf8()
                .SequenceEqual(upper.GetJsonSchemaUtf8()));
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_DecimalAndBigIntegerBounds_PreserveEverySignificantDigit ()
    {
        const decimal lowerBound = 1234567890.123456789m;
        BigInteger upperBound = BigInteger.Parse(
            "1234567890123456789012345678901234567890",
            CultureInfo.InvariantCulture);
        JsonContractGenerationResult exact = Generate(
            builder =>
            {
                builder.SetMinimum(
                    JsonContractNumber.FromDecimal(lowerBound));
                builder.SetMaximum(
                    JsonContractNumber.FromBigInteger(upperBound));
            });
        JsonContractGenerationResult adjacent = Generate(
            builder =>
            {
                builder.SetMinimum(
                    JsonContractNumber.FromDecimal(lowerBound));
                builder.SetMaximum(
                    JsonContractNumber.FromBigInteger(upperBound + 1));
            });

        Assert.Equal(
            "1234567890.123456789",
            GetSchemaKeyword(exact, "minimum").GetRawText());
        Assert.Equal(
            "1234567890123456789012345678901234567890",
            GetSchemaKeyword(exact, "maximum").GetRawText());
        Assert.Equal(
            "1234567890123456789012345678901234567891",
            GetSchemaKeyword(adjacent, "maximum").GetRawText());
        Assert.NotEqual(exact.ContractDigest, adjacent.ContractDigest);
        Assert.False(
            exact
                .GetJsonSchemaUtf8()
                .SequenceEqual(adjacent.GetJsonSchemaUtf8()));
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_HugeExponentInclusiveAndExclusiveBounds_UseDifferentKeywordsBytesAndDigests ()
    {
        JsonContractGenerationResult inclusive = Generate(
            static builder => builder.SetMaximum(
                JsonContractNumber.Parse("1e1000")));
        JsonContractGenerationResult exclusive = Generate(
            static builder => builder.SetExclusiveMaximum(
                JsonContractNumber.Parse("1e1000")));

        Assert.Equal(
            "1e1000",
            GetSchemaKeyword(inclusive, "maximum").GetRawText());
        Assert.Equal(
            "1e1000",
            GetSchemaKeyword(exclusive, "exclusiveMaximum").GetRawText());
        Assert.NotEqual(inclusive.ContractDigest, exclusive.ContractDigest);
        Assert.False(
            inclusive
                .GetJsonSchemaUtf8()
                .SequenceEqual(exclusive.GetJsonSchemaUtf8()));
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_AdjacentHugeExponentBounds_HaveDistinctSchemaBytesAndDigests ()
    {
        JsonContractGenerationResult lower = Generate(
            static builder => builder.SetMaximum(
                JsonContractNumber.Parse("1e1000")));
        JsonContractGenerationResult upper = Generate(
            static builder => builder.SetMaximum(
                JsonContractNumber.Parse(
                    "1.0000000000000001e1000")));

        Assert.Equal(
            "1e1000",
            GetSchemaKeyword(lower, "maximum").GetRawText());
        Assert.Equal(
            "1.0000000000000001e1000",
            GetSchemaKeyword(upper, "maximum").GetRawText());
        Assert.NotEqual(lower.ContractDigest, upper.ContractDigest);
        Assert.False(
            lower
                .GetJsonSchemaUtf8()
                .SequenceEqual(upper.GetJsonSchemaUtf8()));
    }

    private static JsonContractGenerationResult Generate (
        Action<JsonContractMetadataBuilder<ExactNumberValue>>
            declareMetadata)
    {
        var metadata = new JsonContractMetadataRegistry()
            .RegisterProvider(
                new TestMetadataProvider<ExactNumberValue>(
                    "tests.exact-number.metadata",
                    (context, builder) =>
                    {
                        if (context.PropertyInfo is null)
                        {
                            declareMetadata(builder);
                        }
                    }));
        var mapper = new TestTypeMapper(
            "tests.exact-number.mapper",
            static context =>
                context.TypeInfo.Type == typeof(ExactNumberValue),
            static _ => JsonContractTypeMapping.Scalar(
                JsonContractScalarKind.Number));
        var serializerOptions = new JsonSerializerOptions
        {
            UnmappedMemberHandling =
                JsonUnmappedMemberHandling.Disallow,
        };
        serializerOptions.Converters.Add(new ExactNumberValueConverter());

        return GenerationTestHarness.Generate<ExactNumberValue>(
            "tests.exact-number-bound",
            serializerOptions,
            metadataRegistry: metadata,
            typeMappers: new[] { mapper });
    }

    private static JsonElement GetSchemaKeyword (
        JsonContractGenerationResult result,
        string keyword)
    {
        using JsonDocument schema = JsonDocument.Parse(
            result.GetJsonSchemaUtf8());
        return schema.RootElement.GetProperty(keyword).Clone();
    }

    private readonly record struct ExactNumberValue (string Token);

    private sealed class ExactNumberValueConverter
        : JsonConverter<ExactNumberValue>
    {
        public override ExactNumberValue Read (
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            using JsonDocument document = JsonDocument.ParseValue(ref reader);
            return new ExactNumberValue(
                document.RootElement.GetRawText());
        }

        public override void Write (
            Utf8JsonWriter writer,
            ExactNumberValue value,
            JsonSerializerOptions options)
        {
            writer.WriteRawValue(
                value.Token,
                skipInputValidation: false);
        }
    }
}
