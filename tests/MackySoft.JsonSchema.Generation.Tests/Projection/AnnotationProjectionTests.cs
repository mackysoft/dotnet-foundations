using System.Text.Json;
using System.Text.Json.Serialization;
using MackySoft.JsonSchema.Generation.ContractModel;
using MackySoft.JsonSchema.Generation.Tests.Fixtures;
using Contract = MackySoft.JsonSchema.Generation.Annotations;

namespace MackySoft.JsonSchema.Generation.Tests.Projection;

public sealed class AnnotationProjectionTests
{
    [Fact]
    [Trait("Size", "Small")]
    public void Generate_NormalizesAnnotationsAndConstraintsIntoBothProjections ()
    {
        JsonContractGenerationResult result =
            GenerationTestHarness.Generate<AnnotatedContract>(
                "tests.annotated-contract",
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    UnmappedMemberHandling =
                        JsonUnmappedMemberHandling.Disallow,
                });

        JsonContractNode root = result.Model.Root;
        Assert.Equal("Annotated contract", root.Annotations.Title);
        Assert.Equal("Exercises shared annotations.", root.Annotations.Description);
        Assert.Equal(
            "fast",
            root.Annotations.Examples[0].GetProperty("mode").GetString());
        Assert.Equal(1, root.Constraints.MinimumProperties);
        Assert.Equal(3, root.Constraints.MaximumProperties);

        JsonContractNode mode = GenerationTestHarness
            .GetProperty(root, "mode")
            .Value;
        Assert.Equal(JsonContractNodeKind.Enum, mode.Kind);
        Assert.Equal("Execution mode.", mode.Annotations.Description);
        Assert.Equal("fast", mode.Annotations.Examples[0].GetString());
        Assert.Equal(
            new[] { "fast", "safe" },
            mode.AllowedValues.Select(static value => value.GetString()));
        Assert.Equal(2, mode.Constraints.MinimumLength);
        Assert.Equal(8, mode.Constraints.MaximumLength);
        Assert.Equal("^[a-z]+$", mode.Constraints.Pattern);

        JsonContractNode revision = GenerationTestHarness
            .GetProperty(root, "revision")
            .Value;
        Assert.Equal(JsonContractNodeKind.Const, revision.Kind);
        Assert.Equal(3, revision.Constant?.GetInt32());
        Assert.Equal(
            int.MinValue,
            revision.Constraints.Minimum?.GetInt32());
        Assert.Equal(0, revision.Constraints.ExclusiveMinimum?.GetInt32());
        Assert.Equal(10, revision.Constraints.Maximum?.GetInt32());

        JsonContractNode items = GenerationTestHarness
            .GetProperty(root, "items")
            .Value;
        Assert.Equal(1, items.Constraints.MinimumItems);
        Assert.Equal(3, items.Constraints.MaximumItems);

        using JsonDocument schema = JsonDocument.Parse(result.GetJsonSchemaUtf8());
        JsonElement schemaRoot = schema.RootElement;
        Assert.Equal("Annotated contract", schemaRoot.GetProperty("title").GetString());
        Assert.Equal(
            "Exercises shared annotations.",
            schemaRoot.GetProperty("description").GetString());
        Assert.Equal(1, schemaRoot.GetProperty("minProperties").GetInt32());
        Assert.Equal(3, schemaRoot.GetProperty("maxProperties").GetInt32());

        JsonElement schemaProperties = schemaRoot.GetProperty("properties");
        JsonElement modeSchema = schemaProperties.GetProperty("mode");
        Assert.Equal("Execution mode.", modeSchema.GetProperty("description").GetString());
        Assert.Equal(
            new[] { "fast", "safe" },
            modeSchema
                .GetProperty("enum")
                .EnumerateArray()
                .Select(static value => value.GetString()));
        Assert.Equal(2, modeSchema.GetProperty("minLength").GetInt32());
        Assert.Equal(8, modeSchema.GetProperty("maxLength").GetInt32());
        Assert.Equal("^[a-z]+$", modeSchema.GetProperty("pattern").GetString());

        JsonElement revisionSchema = schemaProperties.GetProperty("revision");
        Assert.Equal(3, revisionSchema.GetProperty("const").GetInt32());
        Assert.Equal(
            int.MinValue,
            revisionSchema.GetProperty("minimum").GetInt32());
        Assert.Equal(0, revisionSchema.GetProperty("exclusiveMinimum").GetInt32());
        Assert.Equal(10, revisionSchema.GetProperty("maximum").GetInt32());

        JsonElement itemsSchema = schemaProperties.GetProperty("items");
        Assert.Equal(1, itemsSchema.GetProperty("minItems").GetInt32());
        Assert.Equal(3, itemsSchema.GetProperty("maxItems").GetInt32());

        using JsonDocument typeMetadata = JsonDocument.Parse(
            result.GetTypeMetadataUtf8());
        JsonElement metadataRoot = typeMetadata.RootElement.GetProperty("root");
        JsonElement modeMetadata = GenerationTestHarness.GetTypeMetadataProperty(
            metadataRoot,
            "mode");
        JsonElement modeValue = modeMetadata.GetProperty("value");
        Assert.Equal(
            "enum",
            modeValue.GetProperty("kind").GetString());
        Assert.Equal(
            "Execution mode.",
            modeValue
                .GetProperty("annotations")
                .GetProperty("description")
                .GetString());
        Assert.Equal(
            2,
            modeValue
                .GetProperty("constraints")
                .GetProperty("minimumLength")
                .GetInt32());
    }

    [Contract.Title("Annotated contract")]
    [Contract.Description("Exercises shared annotations.")]
    [Contract.Example("""{"mode":"fast","revision":3,"items":[1]}""")]
    [Contract.PropertyCount(1, 3)]
    private sealed class AnnotatedContract
    {
        [Contract.Description("Execution mode.")]
        [Contract.Example("\"fast\"")]
        [Contract.Enum("\"fast\"", "\"safe\"")]
        [Contract.Length(2, 8)]
        [Contract.Pattern("^[a-z]+$")]
        public string Mode { get; set; } = string.Empty;

        [Contract.Const("3")]
        [Contract.Range(
            "0",
            "10",
            ExclusiveMinimum = true)]
        public int Revision { get; set; }

        [Contract.ItemCount(1, 3)]
        public int[] Items { get; set; } = Array.Empty<int>();
    }
}
