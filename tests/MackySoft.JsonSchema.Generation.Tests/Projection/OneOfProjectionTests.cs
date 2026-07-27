using System.Text.Json;
using System.Text.Json.Serialization;
using MackySoft.JsonSchema.Generation.Annotations;
using MackySoft.JsonSchema.Generation.ContractModel;
using MackySoft.JsonSchema.Generation.Metadata;
using MackySoft.JsonSchema.Generation.Tests.Fixtures;

namespace MackySoft.JsonSchema.Generation.Tests.Projection;

public sealed class OneOfProjectionTests
{
    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenBranchesDeclareDiscriminator_ProjectsTheSameSelectionRules ()
    {
        JsonContractGenerationResult result =
            GenerationTestHarness.Generate<TaggedContract>(
                "tests.tagged-contract");

        JsonContractNode root = result.Model.Root;
        Assert.Equal("kind", root.Discriminator?.PropertyName);
        Assert.Collection(
            root.Variants,
            left =>
            {
                Assert.Equal("left", left.Name);
                Assert.Equal(new[] { "leftValue" }, left.RequiredProperties);
                Assert.Equal("left", left.DiscriminatorValue?.GetString());
                Assert.Equal("Selects the left value.", left.Annotations.Description);
                Assert.Equal(
                    1,
                    left.Annotations.Examples[0]
                        .GetProperty("leftValue")
                        .GetInt32());
            },
            right =>
            {
                Assert.Equal("right", right.Name);
                Assert.Equal(new[] { "rightValue" }, right.RequiredProperties);
                Assert.Equal("right", right.DiscriminatorValue?.GetString());
                Assert.Equal("Selects the right value.", right.Annotations.Description);
            });

        using JsonDocument schema = JsonDocument.Parse(result.GetJsonSchemaUtf8());
        JsonElement variants = schema.RootElement.GetProperty("oneOf");
        Assert.Equal(2, variants.GetArrayLength());
        JsonElement leftSchema = variants[0];
        Assert.Equal(
            "left",
            leftSchema
                .GetProperty("properties")
                .GetProperty("kind")
                .GetProperty("const")
                .GetString());
        Assert.Equal(
            new[] { "leftValue", "kind" },
            leftSchema
                .GetProperty("required")
                .EnumerateArray()
                .Select(static value => value.GetString()));
        Assert.Equal(
            "Selects the left value.",
            leftSchema.GetProperty("description").GetString());

        using JsonDocument typeMetadata = JsonDocument.Parse(
            result.GetTypeMetadataUtf8());
        JsonElement metadataVariants = typeMetadata.RootElement
            .GetProperty("root")
            .GetProperty("variants");
        Assert.Equal(2, metadataVariants.GetArrayLength());
        Assert.Equal(
            "left",
            metadataVariants[0]
                .GetProperty("discriminatorValue")
                .GetString());
        Assert.Equal(
            "leftValue",
            metadataVariants[0]
                .GetProperty("requiredProperties")[0]
                .GetString());
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenProviderDeclaresBranches_UsesTheSameResolutionModel ()
    {
        var provider = new TestMetadataProvider(
            "tests.metadata.branches",
            static context =>
                context.Member is null
                    && context.TargetType == typeof(ProviderTaggedContract)
                    ? new[]
                    {
                        JsonContractMetadata.Discriminator("kind"),
                        JsonContractMetadata.OneOfBranch(
                            new JsonContractBranchMetadata(
                                "alpha",
                                new[] { "alphaValue" },
                                JsonSerializer.SerializeToElement("alpha"),
                                "Selects alpha.")),
                        JsonContractMetadata.OneOfBranch(
                            new JsonContractBranchMetadata(
                                "beta",
                                new[] { "betaValue" },
                                JsonSerializer.SerializeToElement("beta"),
                                "Selects beta.")),
                    }
                    : Array.Empty<JsonContractMetadata>());

        JsonContractGenerationResult result =
            GenerationTestHarness.Generate<ProviderTaggedContract>(
                "tests.provider-tagged-contract",
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    UnmappedMemberHandling =
                        JsonUnmappedMemberHandling.Disallow,
                },
                metadataProviders: new[] { provider });

        Assert.Equal("kind", result.Model.Root.Discriminator?.PropertyName);
        Assert.Equal(
            new[] { "alpha", "beta" },
            result.Model.Root.Variants.Select(static variant => variant.Name));
        Assert.Equal(
            "Selects alpha.",
            result.Model.Root.Variants[0].Annotations.Description);
    }

    [JsonContractDiscriminator("kind")]
    [JsonContractOneOfBranch(
        "left",
        "leftValue",
        DiscriminatorValueJson = "\"left\"",
        Description = "Selects the left value.",
        ExampleJson = """{"kind":"left","leftValue":1}""")]
    [JsonContractOneOfBranch(
        "right",
        "rightValue",
        DiscriminatorValueJson = "\"right\"",
        Description = "Selects the right value.",
        ExampleJson = """{"kind":"right","rightValue":"value"}""")]
    private sealed class TaggedContract
    {
        [JsonRequired]
        [JsonContractRequired]
        [JsonPropertyName("kind")]
        public string Kind { get; set; } = string.Empty;

        [JsonPropertyName("leftValue")]
        public int? LeftValue { get; set; }

        [JsonPropertyName("rightValue")]
        public string? RightValue { get; set; }
    }

    private sealed class ProviderTaggedContract
    {
        [JsonRequired]
        public string Kind { get; set; } = string.Empty;

        public int? AlphaValue { get; set; }

        public string? BetaValue { get; set; }
    }
}
