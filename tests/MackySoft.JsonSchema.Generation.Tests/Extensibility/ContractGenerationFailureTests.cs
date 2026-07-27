using System.Text.Json;
using System.Text.Json.Serialization;
using MackySoft.JsonSchema.Generation.Configuration;
using MackySoft.JsonSchema.Generation.ContractModel;
using MackySoft.JsonSchema.Generation.Diagnostics;
using MackySoft.JsonSchema.Generation.Extensibility;
using MackySoft.JsonSchema.Generation.Metadata;
using MackySoft.JsonSchema.Generation.Tests.Fixtures;

namespace MackySoft.JsonSchema.Generation.Tests.Extensibility;

public sealed class ContractGenerationFailureTests
{
    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenConverterHasNoDeclaredMapping_ReportsUnsupportedConverter ()
    {
        JsonSerializerOptions serializerOptions = OpaqueSerializerOptions();

        JsonContractGenerationException exception =
            Assert.Throws<JsonContractGenerationException>(
                () => GenerationTestHarness.Generate<OpaqueContract>(
                    "tests.unknown-converter",
                    serializerOptions));

        Assert.Equal(
            JsonContractGenerationFailureKind.UnsupportedConverter,
            exception.FailureKind);
        Assert.Equal("tests.unknown-converter", exception.ContractId);
        Assert.Equal(typeof(OpaqueValue), exception.TargetType);
        Assert.Equal("Value", exception.JsonPropertyName);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenMetadataSourcesDisagree_ReportsBothSourcesAndMetadataKind ()
    {
        var alpha = DescriptionProvider(
            "tests.metadata.alpha",
            "Alpha description.");
        var beta = DescriptionProvider(
            "tests.metadata.beta",
            "Beta description.");

        JsonContractGenerationException exception =
            Assert.Throws<JsonContractGenerationException>(
                () => GenerationTestHarness.Generate<MetadataContract>(
                    "tests.metadata-conflict",
                    metadataProviders: new[] { beta, alpha }));

        Assert.Equal(
            JsonContractGenerationFailureKind.ConflictingMetadata,
            exception.FailureKind);
        Assert.Equal("tests.metadata-conflict", exception.ContractId);
        Assert.Equal(typeof(string), exception.TargetType);
        Assert.Equal("Value", exception.JsonPropertyName);
        Assert.Equal(JsonContractMetadataKind.Description, exception.MetadataKind);
        Assert.Equal(
            new[] { "tests.metadata.alpha", "tests.metadata.beta" },
            exception.SourceIds);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void CreateOptions_WhenStableIdIsDuplicatedWithinOneExtensionKind_ReportsTypedFailure ()
    {
        var first = DescriptionProvider(
            "tests.metadata.duplicate",
            "First description.");
        var second = DescriptionProvider(
            "tests.metadata.duplicate",
            "Second description.");

        JsonContractGenerationException exception =
            Assert.Throws<JsonContractGenerationException>(
                () => new JsonContractGeneratorOptions(
                    JsonContractGenerationSettings.ClosedObjects,
                    metadataProviders: new[] { first, second }));

        Assert.Equal(
            JsonContractGenerationFailureKind.DuplicateExtensionId,
            exception.FailureKind);
        Assert.Equal(
            new[] { "tests.metadata.duplicate" },
            exception.SourceIds);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenMultipleTypeMappersClaimOneValue_ReportsAllClaimants ()
    {
        var alpha = OpaqueMapper("tests.mapper.alpha");
        var beta = OpaqueMapper("tests.mapper.beta");

        JsonContractGenerationException exception =
            Assert.Throws<JsonContractGenerationException>(
                () => GenerationTestHarness.Generate<OpaqueContract>(
                    "tests.mapper-conflict",
                    OpaqueSerializerOptions(),
                    typeMappers: new[] { beta, alpha }));

        Assert.Equal(
            JsonContractGenerationFailureKind.MultipleTypeMappers,
            exception.FailureKind);
        Assert.Equal("tests.mapper-conflict", exception.ContractId);
        Assert.Equal(typeof(OpaqueValue), exception.TargetType);
        Assert.Equal("Value", exception.JsonPropertyName);
        Assert.Equal(
            new[] { "tests.mapper.alpha", "tests.mapper.beta" },
            exception.SourceIds);
    }

    private static TestMetadataProvider DescriptionProvider (
        string stableId,
        string description)
    {
        return new TestMetadataProvider(
            stableId,
            context =>
                string.Equals(
                    context.JsonPropertyName,
                    "Value",
                    StringComparison.Ordinal)
                    ? new[] { JsonContractMetadata.Description(description) }
                    : Array.Empty<JsonContractMetadata>());
    }

    private static TestTypeMapper OpaqueMapper (string stableId)
    {
        return new TestTypeMapper(
            stableId,
            static context => context.TargetType == typeof(OpaqueValue),
            static _ => JsonContractTypeMapping.Scalar(
                JsonContractScalarKind.String));
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

    private sealed class MetadataContract
    {
        public string Value { get; set; } = string.Empty;
    }

    private sealed class OpaqueContract
    {
        public OpaqueValue Value { get; set; }
    }

    private readonly record struct OpaqueValue (string Value);

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
}
