using System.Text.Json;
using System.Text.Json.Serialization;
using MackySoft.JsonSchema.Generation.ContractModel;
using MackySoft.JsonSchema.Generation.Tests.Fixtures;

namespace MackySoft.JsonSchema.Generation.Tests.Generation;

public sealed class SourceGeneratedJsonContractTests
{
    [Fact]
    [Trait("Size", "Small")]
    public void Generate_UsesSourceGeneratedSerializerContract ()
    {
        var serializerOptions = new JsonSerializerOptions
        {
            TypeInfoResolver = SourceGeneratedContractContext.Default,
            UnmappedMemberHandling =
                JsonUnmappedMemberHandling.Disallow,
        };

        JsonContractGenerationResult result =
            GenerationTestHarness.Generate<SourceGeneratedContract>(
                "tests.source-generated-contract",
                serializerOptions);

        JsonContractProperty property = Assert.Single(
            result.Model.Root.Properties);
        Assert.Equal("display_name", property.Name);
        Assert.True(property.Value.IsNullable);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_UsesNamingPolicyEmbeddedInSourceGeneratedContext ()
    {
        var serializerOptions = new JsonSerializerOptions(
            CamelCaseSourceGeneratedContext.Default.Options)
        {
            TypeInfoResolver = CamelCaseSourceGeneratedContext.Default,
            UnmappedMemberHandling =
                JsonUnmappedMemberHandling.Disallow,
        };

        JsonContractGenerationResult result =
            GenerationTestHarness.Generate<CamelCaseSourceGeneratedContract>(
                "tests.source-generated-camel-case",
                serializerOptions);

        Assert.Equal(
            new[] { "displayName" },
            result.Model.Root.Properties.Select(
                static property => property.Name));
    }
}

[JsonSerializable(typeof(SourceGeneratedContract))]
internal partial class SourceGeneratedContractContext : JsonSerializerContext
{
}

internal sealed class SourceGeneratedContract
{
    [JsonPropertyName("display_name")]
    public string? DisplayName { get; set; }

    [JsonIgnore]
    public string Ignored { get; set; } = string.Empty;
}

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(CamelCaseSourceGeneratedContract))]
internal partial class CamelCaseSourceGeneratedContext : JsonSerializerContext
{
}

internal sealed class CamelCaseSourceGeneratedContract
{
    public string DisplayName { get; set; } = string.Empty;
}
