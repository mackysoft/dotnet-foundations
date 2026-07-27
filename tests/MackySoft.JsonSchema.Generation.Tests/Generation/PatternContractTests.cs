using MackySoft.JsonSchema.Generation.Annotations;
using MackySoft.JsonSchema.Generation.Diagnostics;
using MackySoft.JsonSchema.Generation.Extensibility;
using MackySoft.JsonSchema.Generation.Metadata;
using MackySoft.JsonSchema.Generation.Tests.Fixtures;

namespace MackySoft.JsonSchema.Generation.Tests.Generation;

public sealed class PatternContractTests
{
    [Theory]
    [InlineData("(?i)a")]
    [InlineData("(?#comment)a")]
    [InlineData("(?>a)")]
    [InlineData("(?'name'a)")]
    [InlineData(@"\Aabc\z")]
    [Trait("Size", "Small")]
    public void Generate_WhenPatternUsesDotNetOnlySyntax_ReportsInvalidMetadata (
        string pattern)
    {
        JsonContractGenerationException exception =
            Assert.Throws<JsonContractGenerationException>(
                () => GenerationTestHarness.Generate<PatternContract>(
                    "tests.dotnet-only-pattern",
                    metadataProviders: new[]
                    {
                        new PatternMetadataProvider(pattern),
                    }));

        Assert.Equal(
            JsonContractGenerationFailureKind.InvalidMetadataValue,
            exception.FailureKind);
        Assert.Equal(JsonContractMetadataKind.Pattern, exception.MetadataKind);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenPatternUsesPortableDraftTokens_ProjectsPattern ()
    {
        JsonContractGenerationResult result =
            GenerationTestHarness.Generate<PortablePatternContract>(
                "tests.portable-pattern");

        Assert.Equal(
            "^[A-Za-z0-9._:/@-]+$",
            result.Model.Root.Properties[0].Value.Constraints.Pattern);
    }

    private sealed class PatternContract
    {
        public string Value { get; set; } = string.Empty;
    }

    private sealed class PortablePatternContract
    {
        [Pattern("^[A-Za-z0-9._:/@-]+$")]
        public string Value { get; set; } = string.Empty;
    }

    private sealed class PatternMetadataProvider : IJsonContractMetadataProvider
    {
        private readonly string pattern;

        public PatternMetadataProvider (string pattern)
        {
            this.pattern = pattern;
        }

        public string StableId => "tests.pattern-provider";

        public string ContractVersion => "1";

        public IReadOnlyList<JsonContractMetadata> GetMetadata (
            JsonContractMetadataContext context)
        {
            return context.JsonPropertyName == "Value"
                ? new[] { JsonContractMetadata.Pattern(pattern) }
                : Array.Empty<JsonContractMetadata>();
        }
    }
}
