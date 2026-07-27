using MackySoft.JsonSchema.Generation.ContractModel;
using MackySoft.JsonSchema.Generation.Tests.Fixtures;

namespace MackySoft.JsonSchema.Generation.Tests.Generation;

public sealed class CollectionContractTests
{
    [Fact]
    [Trait("Size", "Small")]
    public void Generate_PreservesArrayItemAndDictionaryValueNullability ()
    {
        JsonContractGenerationResult result =
            GenerationTestHarness.Generate<CollectionContract>(
                "tests.collection-contract");

        JsonContractNode nonNullableArray = GenerationTestHarness
            .GetProperty(result.Model.Root, "NonNullableArray")
            .Value;
        JsonContractNode nullableArray = GenerationTestHarness
            .GetProperty(result.Model.Root, "NullableArray")
            .Value;
        Assert.Equal(JsonContractNodeKind.Array, nonNullableArray.Kind);
        Assert.Equal(JsonContractNodeKind.Array, nullableArray.Kind);
        Assert.False(Assert.IsType<JsonContractNode>(nonNullableArray.Items).IsNullable);
        Assert.True(Assert.IsType<JsonContractNode>(nullableArray.Items).IsNullable);

        JsonContractNode nonNullableDictionary = GenerationTestHarness
            .GetProperty(result.Model.Root, "NonNullableDictionary")
            .Value;
        JsonContractNode nullableDictionary = GenerationTestHarness
            .GetProperty(result.Model.Root, "NullableDictionary")
            .Value;
        Assert.Equal(
            JsonContractNodeKind.Dictionary,
            nonNullableDictionary.Kind);
        Assert.Equal(JsonContractNodeKind.Dictionary, nullableDictionary.Kind);
        Assert.False(
            Assert.IsType<JsonContractNode>(
                    nonNullableDictionary.AdditionalProperties)
                .IsNullable);
        Assert.True(
            Assert.IsType<JsonContractNode>(
                    nullableDictionary.AdditionalProperties)
                .IsNullable);
    }

    private sealed class CollectionContract
    {
        public string[] NonNullableArray { get; set; } = Array.Empty<string>();

        public Dictionary<string, string> NonNullableDictionary { get; set; } =
            new();

        public string?[] NullableArray { get; set; } = Array.Empty<string?>();

        public Dictionary<string, string?> NullableDictionary { get; set; } =
            new();
    }
}
