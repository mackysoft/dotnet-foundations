using System.Text.Json;
using MackySoft.JsonSchema.Generation.ContractModel;
using MackySoft.JsonSchema.Generation.Tests.Fixtures;

namespace MackySoft.JsonSchema.Generation.Tests.Generation;

public sealed class ObjectGraphContractTests
{
    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenObjectContainsNestedObject_ProjectsOneReusableDefinition ()
    {
        JsonContractGenerationResult result =
            GenerationTestHarness.Generate<NestedObjectContract>(
                "tests.nested-object-contract");

        JsonContractNode nestedReference = GenerationTestHarness
            .GetProperty(result.Model.Root, "Nested")
            .Value;
        Assert.Equal(JsonContractNodeKind.Reference, nestedReference.Kind);
        string definitionId = Assert.IsType<string>(nestedReference.ReferenceId);

        JsonContractDefinition definition = Assert.Single(
            result.Model.Definitions,
            candidate => string.Equals(
                candidate.Id,
                definitionId,
                StringComparison.Ordinal));
        Assert.Equal(JsonContractNodeKind.Object, definition.Value.Kind);
        Assert.Equal(
            JsonContractScalarKind.Integer,
            GenerationTestHarness
                .GetProperty(definition.Value, "Value")
                .Value
                .ScalarKind);

        using JsonDocument schema = JsonDocument.Parse(
            result.GetJsonSchemaUtf8());
        Assert.Equal(
            $"#/$defs/{definitionId}",
            schema.RootElement
                .GetProperty("properties")
                .GetProperty("Nested")
                .GetProperty("$ref")
                .GetString());
        Assert.Equal(
            "integer",
            schema.RootElement
                .GetProperty("$defs")
                .GetProperty(definitionId)
                .GetProperty("properties")
                .GetProperty("Value")
                .GetProperty("type")
                .GetString());
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenObjectIsRecursive_ClosesTheCycleThroughItsDefinition ()
    {
        JsonContractGenerationResult result =
            GenerationTestHarness.Generate<RecursiveContract>(
                "tests.recursive-contract");

        JsonContractNode rootReference = GenerationTestHarness
            .GetProperty(result.Model.Root, "Next")
            .Value;
        Assert.Equal(JsonContractNodeKind.Reference, rootReference.Kind);
        Assert.True(rootReference.IsNullable);
        string definitionId = Assert.IsType<string>(rootReference.ReferenceId);

        JsonContractDefinition definition = Assert.Single(
            result.Model.Definitions,
            candidate => string.Equals(
                candidate.Id,
                definitionId,
                StringComparison.Ordinal));
        JsonContractNode recursiveReference = GenerationTestHarness
            .GetProperty(definition.Value, "Next")
            .Value;
        Assert.Equal(JsonContractNodeKind.Reference, recursiveReference.Kind);
        Assert.Equal(definitionId, recursiveReference.ReferenceId);
        Assert.True(recursiveReference.IsNullable);

        using JsonDocument schema = JsonDocument.Parse(
            result.GetJsonSchemaUtf8());
        JsonElement recursiveProperty = schema.RootElement
            .GetProperty("$defs")
            .GetProperty(definitionId)
            .GetProperty("properties")
            .GetProperty("Next");
        Assert.Contains(
            recursiveProperty.GetProperty("anyOf").EnumerateArray(),
            candidate =>
                candidate.TryGetProperty("$ref", out JsonElement reference)
                && string.Equals(
                    reference.GetString(),
                    $"#/$defs/{definitionId}",
                    StringComparison.Ordinal));
    }

    private sealed class NestedObjectContract
    {
        public NestedValueContract Nested { get; set; } = new();
    }

    private sealed class NestedValueContract
    {
        public int Value { get; set; }
    }

    private sealed class RecursiveContract
    {
        public RecursiveContract? Next { get; set; }
    }
}
