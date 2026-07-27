using System.Text.Json;
using MackySoft.JsonSchema.Generation.ContractModel;
using MackySoft.JsonSchema.Generation.Extensibility;
using MackySoft.JsonSchema.Generation.Tests.Fixtures;

namespace MackySoft.JsonSchema.Generation.Tests.ContractModel;

public sealed class JsonContractModelImmutabilityTests
{
    [Fact]
    [Trait("Size", "Small")]
    public void Generate_DefensivelyOwnsContributionValuesCollectionsAndResultBytes ()
    {
        var contributor = new RetainedModelContributor();
        JsonContractGenerationResult result;
        using (JsonDocument source = JsonDocument.Parse(
            """{"audience":"consumer"}"""))
        {
            contributor.SetValue(source.RootElement);
            result = GenerationTestHarness.Generate<SimpleContract>(
                "tests.immutable-model",
                modelContributors: new[] { contributor });
        }

        contributor.ReplaceDeclaration(
            JsonSerializer.SerializeToElement(
                new { audience = "mutated" }));

        Assert.Single(result.Model.Contributions);
        Assert.Equal(
            "consumer",
            result.Model.Contributions[0]
                .Value
                .GetProperty("audience")
                .GetString());

        byte[] schemaSnapshot = result.GetJsonSchemaUtf8();
        byte[] expectedSchema = (byte[])schemaSnapshot.Clone();
        schemaSnapshot[0] ^= byte.MaxValue;
        Assert.Equal(expectedSchema, result.GetJsonSchemaUtf8());

        byte[] metadataSnapshot = result.GetTypeMetadataUtf8();
        byte[] expectedMetadata = (byte[])metadataSnapshot.Clone();
        metadataSnapshot[0] ^= byte.MaxValue;
        Assert.Equal(expectedMetadata, result.GetTypeMetadataUtf8());
    }

    private sealed class RetainedModelContributor : IJsonContractModelContributor
    {
        private JsonElement value;

        private JsonContractModelContribution[]? declarations;

        public string StableId => "tests.model.contributor";

        public string ContractVersion => "1";

        public IReadOnlyList<JsonContractModelContribution> GetContributions (
            JsonContractModelContext context)
        {
            declarations =
                new[]
                {
                    new JsonContractModelContribution(
                        context.ModelTarget,
                        "productHint",
                        value,
                        StableId),
                };
            return declarations;
        }

        internal void SetValue (JsonElement newValue)
        {
            value = newValue;
        }

        internal void ReplaceDeclaration (JsonElement replacement)
        {
            JsonContractModelContribution declaration =
                declarations?[0]
                ?? throw new InvalidOperationException(
                    "The contributor has not produced a declaration.");
            declarations[0] =
                new JsonContractModelContribution(
                    declaration.Target,
                    declaration.Name,
                    replacement,
                    declaration.SourceId);
        }
    }

    private sealed class SimpleContract
    {
        public int Value { get; set; }
    }
}
