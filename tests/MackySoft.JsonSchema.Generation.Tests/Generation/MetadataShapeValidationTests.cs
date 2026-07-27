using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using MackySoft.JsonSchema.Generation.Annotations;
using MackySoft.JsonSchema.Generation.Configuration;
using MackySoft.JsonSchema.Generation.Diagnostics;
using MackySoft.JsonSchema.Generation.Extensibility;
using MackySoft.JsonSchema.Generation.Metadata;
using MackySoft.JsonSchema.Generation.Projection;
using MackySoft.JsonSchema.Generation.Tests.Fixtures;

namespace MackySoft.JsonSchema.Generation.Tests.Generation;

public sealed class MetadataShapeValidationTests
{
    [Theory]
    [InlineData(typeof(LengthOnIntegerContract), typeof(int))]
    [InlineData(typeof(PatternOnIntegerContract), typeof(int))]
    [InlineData(typeof(ItemCountOnStringContract), typeof(string))]
    [InlineData(typeof(PropertyCountOnArrayContract), typeof(int[]))]
    [Trait("Size", "Small")]
    public void Generate_WhenBuiltInConstraintDoesNotMatchEffectiveShape_ReportsInvalidMetadata (
        Type contractType,
        Type targetType)
    {
        JsonContractGenerationException exception =
            Assert.Throws<JsonContractGenerationException>(
                () => Generate(contractType));

        Assert.Equal(
            JsonContractGenerationFailureKind.InvalidMetadataValue,
            exception.FailureKind);
        Assert.Equal(targetType, exception.TargetType);
        Assert.Equal("Value", exception.JsonPropertyName);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenNumericBoundDoesNotMatchEffectiveShape_ReportsInvalidMetadata ()
    {
        const string ProviderId = "tests.invalid-numeric-shape";
        var registry = new JsonContractMetadataRegistry()
            .RegisterProvider(
                new TestMetadataProvider<string>(
                    ProviderId,
                    static (context, builder) =>
                    {
                        if (context.PropertyInfo?.Name == "Value")
                        {
                            builder.SetMinimum(
                                JsonContractNumber.FromInt64(0));
                        }
                    }));

        JsonContractGenerationException exception =
            Assert.Throws<JsonContractGenerationException>(
                () => Generate(
                    typeof(NumericBoundOnStringContract),
                    registry));

        Assert.Equal(
            JsonContractGenerationFailureKind.InvalidMetadataValue,
            exception.FailureKind);
        Assert.Equal(typeof(string), exception.TargetType);
        Assert.Equal("Value", exception.JsonPropertyName);
    }

    private static JsonContractGenerationResult Generate (
        Type contractType,
        JsonContractMetadataRegistry? metadataRegistry = null)
    {
        var serializerOptions = new JsonSerializerOptions
        {
            TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
            UnmappedMemberHandling =
                JsonUnmappedMemberHandling.Disallow,
        };
        serializerOptions.MakeReadOnly();
        var generator = new JsonContractGenerator(
            new JsonContractGeneratorOptions(
                JsonContractGenerationSettings.ClosedObjects,
                metadataRegistry));

        return generator.Generate(
            new JsonContractGenerationRequest(
                "tests.metadata-shape/" + contractType.Name,
                serializerOptions.GetTypeInfo(contractType),
                new JsonSchemaDocumentOptions(
                    JsonSchemaDocumentKind.Complete,
                    id: null,
                    logicalName: null)));
    }

    private sealed class LengthOnIntegerContract
    {
        [Length(1, 2)]
        public int Value { get; set; }
    }

    private sealed class PatternOnIntegerContract
    {
        [Pattern("^[0-9]+$")]
        public int Value { get; set; }
    }

    private sealed class ItemCountOnStringContract
    {
        [ItemCount(1, 2)]
        public string Value { get; set; } = string.Empty;
    }

    private sealed class PropertyCountOnArrayContract
    {
        [PropertyCount(1, 2)]
        public int[] Value { get; set; } = Array.Empty<int>();
    }

    private sealed class NumericBoundOnStringContract
    {
        public string Value { get; set; } = string.Empty;
    }
}
