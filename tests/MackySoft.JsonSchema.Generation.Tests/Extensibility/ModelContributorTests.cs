using System.Text.Json;
using System.Text.Json.Serialization;
using MackySoft.JsonSchema.Generation.Annotations;
using MackySoft.JsonSchema.Generation.ContractModel;
using MackySoft.JsonSchema.Generation.Diagnostics;
using MackySoft.JsonSchema.Generation.Tests.Fixtures;

namespace MackySoft.JsonSchema.Generation.Tests.Extensibility;

public sealed class ModelContributorTests
{
    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenContributorsDisagree_ReportsConflict ()
    {
        TestModelContributor alpha = Contributor(
            "tests.contributor.alpha",
            "alpha");
        TestModelContributor beta = Contributor(
            "tests.contributor.beta",
            "beta");

        JsonContractGenerationException exception =
            Assert.Throws<JsonContractGenerationException>(
                () => GenerationTestHarness.Generate<SimpleContract>(
                    "tests.contributor-conflict",
                    modelContributors: new[] { beta, alpha }));

        Assert.Equal(
            JsonContractGenerationFailureKind.ModelContributionConflict,
            exception.FailureKind);
        Assert.Equal("tests.contributor-conflict", exception.ContractId);
        Assert.Equal(
            new[] { "tests.contributor.alpha", "tests.contributor.beta" },
            exception.SourceIds);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_ModelContributorCanResolveEverySemanticTargetFromTheContext ()
    {
        const string StableId = "tests.contributor.targets";
        IReadOnlyDictionary<string, JsonContractModelTarget>? targets = null;
        JsonContractModelTarget? rootNodeTarget = null;

        var contributor = new TestModelContributor(
            StableId,
            context =>
            {
                JsonContractProperty nestedProperty = context.Root.Properties.Single(
                    static property => property.Name == nameof(TargetNavigationContract.Nested));
                int propertyIndex = context.Root.Properties
                    .Select(static (property, index) => (property, index))
                    .Single(candidate => ReferenceEquals(candidate.property, nestedProperty))
                    .index;
                JsonContractVariant variant = context.Root.Variants[0];
                JsonContractDefinition definition = context.Definitions[0];

                rootNodeTarget = context.GetTarget(context.Root);
                targets = new Dictionary<string, JsonContractModelTarget>(
                    StringComparer.Ordinal)
                {
                    ["model"] = context.ModelTarget,
                    ["root"] = context.RootTarget,
                    ["node"] = context.GetTarget(nestedProperty.Value),
                    ["property"] = context.GetTarget(nestedProperty),
                    ["variant"] = context.GetTarget(variant),
                    ["definition"] = context.GetTarget(definition),
                };

                return targets
                    .Select(
                        pair =>
                            new JsonContractModelContribution(
                                pair.Value,
                                "productHint",
                                JsonSerializer.SerializeToElement(pair.Key),
                                StableId))
                    .ToArray();
            });

        JsonContractGenerationResult result =
            GenerationTestHarness.Generate<TargetNavigationContract>(
                "tests.contributor-targets",
                modelContributors: new[] { contributor });

        IReadOnlyDictionary<string, JsonContractModelTarget> observed =
            Assert.IsAssignableFrom<IReadOnlyDictionary<string, JsonContractModelTarget>>(
                targets);
        Assert.Same(observed["root"], rootNodeTarget);
        Assert.Equal(string.Empty, observed["model"].Pointer);
        Assert.Equal("/root", observed["root"].Pointer);

        JsonContractProperty nested = result.Model.Root.Properties.Single(
            static property => property.Name == nameof(TargetNavigationContract.Nested));
        int nestedIndex = result.Model.Root.Properties
            .Select(static (property, index) => (property, index))
            .Single(candidate => ReferenceEquals(candidate.property, nested))
            .index;
        Assert.Equal(
            $"/root/properties/{nestedIndex}/value",
            observed["node"].Pointer);
        Assert.Equal(
            $"/root/properties/{nestedIndex}",
            observed["property"].Pointer);
        Assert.Equal("/root/variants/0", observed["variant"].Pointer);
        Assert.Equal("/definitions/0", observed["definition"].Pointer);
        Assert.Equal(6, result.Model.Contributions.Count);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenContributorUsesTargetFromAnotherContext_ReportsInvalidContribution ()
    {
        const string StableId = "tests.contributor.foreign-target";
        JsonContractModelTarget? foreignTarget = null;
        var captureContributor = new TestModelContributor(
            StableId,
            context =>
            {
                foreignTarget = context.ModelTarget;
                return Array.Empty<JsonContractModelContribution>();
            });
        _ = GenerationTestHarness.Generate<SimpleContract>(
            "tests.contributor-target-source",
            modelContributors: new[] { captureContributor });

        var foreignTargetContributor = new TestModelContributor(
            StableId,
            _ =>
                new[]
                {
                    new JsonContractModelContribution(
                        foreignTarget
                            ?? throw new InvalidOperationException(
                                "The source context did not expose a target."),
                        "productHint",
                        JsonSerializer.SerializeToElement("value"),
                        StableId),
                });

        JsonContractGenerationException exception =
            Assert.Throws<JsonContractGenerationException>(
                () => GenerationTestHarness.Generate<SimpleContract>(
                    "tests.contributor-target-consumer",
                    modelContributors: new[] { foreignTargetContributor }));

        Assert.Equal(
            JsonContractGenerationFailureKind.InvalidModelContribution,
            exception.FailureKind);
    }

    private static TestModelContributor Contributor (
        string stableId,
        string value)
    {
        return new TestModelContributor(
            stableId,
            context =>
                new[]
                {
                    new JsonContractModelContribution(
                        context.ModelTarget,
                        "productHint",
                        JsonSerializer.SerializeToElement(value),
                        stableId),
                });
    }

    private sealed class SimpleContract
    {
        public int Value { get; set; }
    }

    [JsonContractDiscriminator(nameof(Kind))]
    [JsonContractOneOfBranch(
        "count",
        nameof(Count),
        DiscriminatorValueJson = "\"count\"")]
    [JsonContractOneOfBranch(
        "nested",
        nameof(Nested),
        DiscriminatorValueJson = "\"nested\"")]
    private sealed class TargetNavigationContract
    {
        [JsonRequired]
        public string Kind { get; set; } = string.Empty;

        public int? Count { get; set; }

        public NestedTargetContract? Nested { get; set; }
    }

    private sealed class NestedTargetContract
    {
        public int Value { get; set; }
    }
}
