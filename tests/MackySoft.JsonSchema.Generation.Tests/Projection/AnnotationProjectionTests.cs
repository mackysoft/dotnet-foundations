using System.Text.Json;
using System.Text.Json.Serialization;
using MackySoft.JsonSchema.Generation.Annotations;
using MackySoft.JsonSchema.Generation.ContractModel;
using MackySoft.JsonSchema.Generation.Extensibility;
using MackySoft.JsonSchema.Generation.Metadata;
using MackySoft.JsonSchema.Generation.Tests.Fixtures;

namespace MackySoft.JsonSchema.Generation.Tests.Projection;

public sealed class AnnotationProjectionTests
{
    [Fact]
    [Trait("Size", "Small")]
    public void Generate_NormalizesTypedAnnotationsAndConstraintsIntoBothProjections ()
    {
        var metadata = new JsonContractMetadataRegistry()
            .RegisterProvider(
                new TestMetadataProvider<AnnotatedContract>(
                    "tests.annotated-root",
                    static (context, builder) =>
                    {
                        if (context.PropertyInfo is null)
                        {
                            builder.AddExample(
                                new AnnotatedContract
                                {
                                    Mode = "fast",
                                    Revision = 3,
                                    Items = new[] { 1 },
                                });
                        }
                    }))
            .RegisterProvider(
                new TestMetadataProvider<string>(
                    "tests.annotated-string",
                    static (context, builder) =>
                    {
                        if (context.PropertyInfo?.Name == "mode")
                        {
                            builder.AddExample("fast");
                        }
                    }))
            .RegisterProvider(
                new TestMetadataProvider<int>(
                    "tests.annotated-integer",
                    static (context, builder) =>
                    {
                        if (context.PropertyInfo?.Name == "revision")
                        {
                            builder.SetConst(3);
                            builder.SetExclusiveMinimum(
                                JsonContractNumber.FromInt64(0));
                            builder.SetMaximum(
                                JsonContractNumber.FromInt64(10));
                        }
                    }))
            .RegisterProvider(
                new TestMetadataProvider<int[]>(
                    "tests.annotated-array",
                    static (context, builder) =>
                    {
                        if (context.PropertyInfo?.Name == "items")
                        {
                            builder.AddExample(new[] { 1, 2 });
                        }
                    }));

        JsonContractGenerationResult result =
            GenerationTestHarness.Generate<AnnotatedContract>(
                "tests.annotated-contract",
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    UnmappedMemberHandling =
                        JsonUnmappedMemberHandling.Disallow,
                },
                metadataRegistry: metadata);

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
        Assert.Equal(JsonContractNodeKind.Scalar, mode.Kind);
        Assert.Equal("Execution mode.", mode.Annotations.Description);
        Assert.Equal("fast", mode.Annotations.Examples[0].GetString());
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
        Assert.Equal(
            2,
            items.Annotations.Examples[0].GetArrayLength());

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
        Assert.Equal("fast", modeSchema.GetProperty("examples")[0].GetString());
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

        using JsonDocument typeMetadata = JsonDocument.Parse(
            result.GetTypeMetadataUtf8());
        JsonElement metadataRoot = typeMetadata.RootElement.GetProperty("root");
        JsonElement modeMetadata = GenerationTestHarness.GetTypeMetadataProperty(
            metadataRoot,
            "mode");
        JsonElement modeValue = modeMetadata.GetProperty("value");
        Assert.Equal("scalar", modeValue.GetProperty("kind").GetString());
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

    [Title("Annotated contract")]
    [Description("Exercises shared annotations.")]
    [PropertyCount(1, 3)]
    private sealed class AnnotatedContract
    {
        [Description("Execution mode.")]
        [Length(2, 8)]
        [Pattern("^[a-z]+$")]
        public string Mode { get; set; } = string.Empty;

        public int Revision { get; set; }

        [ItemCount(1, 3)]
        public int[] Items { get; set; } = Array.Empty<int>();
    }
}
