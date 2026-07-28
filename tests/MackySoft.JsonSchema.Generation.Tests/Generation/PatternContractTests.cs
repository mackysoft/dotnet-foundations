using System.Text.RegularExpressions;
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
    [InlineData("(?=a)a")]
    [InlineData("(?!a)a")]
    [InlineData("a(?![\\s\\S])")]
    [InlineData("a$(?![\\s\\S])b")]
    [InlineData(@"\Aabc\z")]
    [Trait("Size", "Small")]
    public void Generate_WhenPatternUsesDotNetOnlySyntax_ReportsInvalidMetadata (
        string pattern)
    {
        JsonContractGenerationException exception =
            Assert.Throws<JsonContractGenerationException>(
                () => GenerationTestHarness.Generate<PatternContract>(
                    "tests.dotnet-only-pattern",
                    metadataRegistry: new JsonContractMetadataRegistry()
                        .RegisterProvider(
                            new PatternMetadataProvider(pattern))));

        Assert.Equal(
            JsonContractGenerationFailureKind.InvalidMetadataValue,
            exception.FailureKind);
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

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenPatternUsesStrictEndAssertion_RejectsFinalLineTerminators ()
    {
        JsonContractGenerationResult result =
            GenerationTestHarness.Generate<StrictEndPatternContract>(
                "tests.strict-end-pattern");
        string pattern = Assert.IsType<string>(
            result.Model.Root.Properties[0].Value.Constraints.Pattern);
        var expression = new Regex(
            pattern,
            RegexOptions.ECMAScript,
            TimeSpan.FromSeconds(1));

        Assert.Matches(expression, "value");
        Assert.DoesNotMatch(expression, "value\n");
        Assert.DoesNotMatch(expression, "value\r\n");
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenMetadataProviderUsesStrictEndAssertion_ProjectsPattern ()
    {
        const string Pattern = "^[a-z]+$(?![\\s\\S])";
        JsonContractGenerationResult result =
            GenerationTestHarness.Generate<PatternContract>(
                "tests.strict-end-provider-pattern",
                metadataRegistry: new JsonContractMetadataRegistry()
                    .RegisterProvider(
                        new PatternMetadataProvider(Pattern)));

        Assert.Equal(
            Pattern,
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

    private sealed class StrictEndPatternContract
    {
        [Pattern("^[a-z]+$(?![\\s\\S])")]
        public string Value { get; set; } = string.Empty;
    }

    private sealed class PatternMetadataProvider
        : IJsonContractMetadataProvider<string>
    {
        private readonly string pattern;

        public PatternMetadataProvider (string pattern)
        {
            this.pattern = pattern;
        }

        public string StableId => "tests.pattern-provider";

        public string ContractVersion => "1";

        public void ProvideMetadata (
            JsonContractMetadataContext<string> context,
            JsonContractMetadataBuilder<string> builder)
        {
            if (context.PropertyInfo?.Name == "Value")
            {
                builder.SetPattern(pattern);
            }
        }
    }
}
