using System.Text.Json;
using System.Text.Json.Serialization;
using MackySoft.JsonSchema.Generation.ContractModel;
using MackySoft.JsonSchema.Generation.Tests.Fixtures;

namespace MackySoft.JsonSchema.Generation.Tests.Generation;

public sealed class ExtensionDataContractTests
{
    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenObjectHasExtensionData_UsesItAsTheAdditionalPropertyContract ()
    {
        JsonContractGenerationResult result =
            GenerationTestHarness.Generate<ExtensionDataContract>(
                "tests.extension-data-contract");

        Assert.Equal(
            new[] { "Name" },
            result.Model.Root.Properties.Select(static property => property.Name));
        JsonContractNode additionalProperties = Assert.IsType<JsonContractNode>(
            result.Model.Root.AdditionalProperties);
        Assert.Equal(
            JsonContractNodeKind.Arbitrary,
            additionalProperties.Kind);

        using JsonDocument schema = JsonDocument.Parse(
            result.GetJsonSchemaUtf8());
        Assert.Empty(
            schema.RootElement
                .GetProperty("additionalProperties")
                .EnumerateObject());
        Assert.False(
            schema.RootElement
                .GetProperty("properties")
                .TryGetProperty("Additional", out _));
    }

    private sealed class ExtensionDataContract
    {
        public string Name { get; set; } = string.Empty;

        [JsonExtensionData]
        public Dictionary<string, JsonElement> Additional { get; set; } =
            new();
    }
}
