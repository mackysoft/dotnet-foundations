using System.Text.Json;
using MackySoft.JsonSchema.Generation.Configuration;
using MackySoft.JsonSchema.Generation.ContractModel;
using MackySoft.JsonSchema.Generation.Extensibility;
using MackySoft.JsonSchema.Generation.Projection;
using MackySoft.JsonSchema.Generation.Tests.Fixtures;

namespace MackySoft.JsonSchema.Generation.Tests.Generation;

public sealed class DeterministicGenerationTests
{
    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WithEquivalentRegistrationSets_ProducesIdenticalModelDigestAndBytes ()
    {
        var alphaProvider = DescriptionProvider<string>(
            "tests.provider.alpha",
            "Alpha",
            "Alpha description.");
        var betaProvider = DescriptionProvider<int>(
            "tests.provider.beta",
            "Beta",
            "Beta description.");
        var firstRegistry = new JsonContractMetadataRegistry()
            .RegisterProvider(betaProvider)
            .RegisterProvider(alphaProvider);
        var secondRegistry = new JsonContractMetadataRegistry()
            .RegisterProvider(alphaProvider)
            .RegisterProvider(betaProvider);

        JsonContractGenerationResult first =
            GenerationTestHarness.Generate<DeterministicContract>(
                "tests.deterministic-contract",
                metadataRegistry: firstRegistry);
        JsonContractGenerationResult second =
            GenerationTestHarness.Generate<DeterministicContract>(
                "tests.deterministic-contract",
                metadataRegistry: secondRegistry);

        Assert.Equal(first.ContractDigest, second.ContractDigest);
        Assert.Equal(
            first.Model.Root.Properties.Select(static property => property.Name),
            second.Model.Root.Properties.Select(static property => property.Name));
        Assert.Equal(first.GetJsonSchemaUtf8(), second.GetJsonSchemaUtf8());
        Assert.Equal(first.GetTypeMetadataUtf8(), second.GetTypeMetadataUtf8());
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenOnlyArtifactOptionsChange_PreservesContractDigest ()
    {
        var completeOptions = new JsonSchemaDocumentOptions(
            JsonSchemaDocumentKind.Complete,
            "https://schemas.example.test/deterministic.json",
            "complete.logical-name");
        var fragmentOptions = new JsonSchemaDocumentOptions(
            JsonSchemaDocumentKind.Fragment,
            id: null,
            logicalName: "fragment.logical-name");

        JsonContractGenerationResult complete =
            GenerationTestHarness.Generate<DeterministicContract>(
                "tests.artifact-independent-contract",
                documentOptions: completeOptions);
        JsonContractGenerationResult fragment =
            GenerationTestHarness.Generate<DeterministicContract>(
                "tests.artifact-independent-contract",
                documentOptions: fragmentOptions);

        Assert.Equal(complete.ContractDigest, fragment.ContractDigest);
        Assert.NotEqual(
            complete.GetJsonSchemaUtf8(),
            fragment.GetJsonSchemaUtf8());

        using JsonDocument completeSchema = JsonDocument.Parse(
            complete.GetJsonSchemaUtf8());
        using JsonDocument fragmentSchema = JsonDocument.Parse(
            fragment.GetJsonSchemaUtf8());
        Assert.Equal(
            JsonContractGenerationSettings.Draft202012Dialect,
            completeSchema.RootElement.GetProperty("$schema").GetString());
        Assert.Equal(
            "https://schemas.example.test/deterministic.json",
            completeSchema.RootElement.GetProperty("$id").GetString());
        Assert.False(fragmentSchema.RootElement.TryGetProperty("$schema", out _));
        Assert.False(fragmentSchema.RootElement.TryGetProperty("$id", out _));

        using JsonDocument completeMetadata = JsonDocument.Parse(
            complete.GetTypeMetadataUtf8());
        using JsonDocument fragmentMetadata = JsonDocument.Parse(
            fragment.GetTypeMetadataUtf8());
        Assert.Equal(
            "complete.logical-name",
            completeMetadata.RootElement.GetProperty("schemaName").GetString());
        Assert.Equal(
            "fragment.logical-name",
            fragmentMetadata.RootElement.GetProperty("schemaName").GetString());
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenModelContributionChanges_ChangesContractDigest ()
    {
        TestModelContributor alpha = Contributor("alpha");
        TestModelContributor beta = Contributor("beta");

        JsonContractGenerationResult first =
            GenerationTestHarness.Generate<DeterministicContract>(
                "tests.contribution-digest",
                modelContributors: new[] { alpha });
        JsonContractGenerationResult second =
            GenerationTestHarness.Generate<DeterministicContract>(
                "tests.contribution-digest",
                modelContributors: new[] { beta });

        Assert.NotEqual(first.ContractDigest, second.ContractDigest);
        Assert.NotEqual(
            first.GetTypeMetadataUtf8(),
            second.GetTypeMetadataUtf8());
    }

    private static TestMetadataProvider<TValue> DescriptionProvider<TValue> (
        string stableId,
        string propertyName,
        string description)
    {
        return new TestMetadataProvider<TValue>(
            stableId,
            (context, builder) =>
            {
                if (string.Equals(
                    context.PropertyInfo?.Name,
                    propertyName,
                    StringComparison.Ordinal))
                {
                    builder.SetDescription(description);
                }
            });
    }

    private static TestModelContributor Contributor (string value)
    {
        const string StableId = "tests.contributor.digest";
        return new TestModelContributor(
            StableId,
            context =>
                new[]
                {
                    new JsonContractModelContribution(
                        context.ModelTarget,
                        "productHint",
                        JsonSerializer.SerializeToElement(value),
                        StableId),
                });
    }

    private sealed class DeterministicContract
    {
        public string Alpha { get; set; } = string.Empty;

        public int Beta { get; set; }
    }
}
