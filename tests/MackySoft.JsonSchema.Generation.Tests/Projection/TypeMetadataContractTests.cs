using System.Text.Json;
using System.Text.Json.Serialization;
using MackySoft.JsonSchema.Generation.ContractModel;
using MackySoft.JsonSchema.Generation.Tests.Fixtures;

namespace MackySoft.JsonSchema.Generation.Tests.Projection;

public sealed class TypeMetadataContractTests
{
    [Fact]
    [Trait("Size", "Small")]
    public void Generate_TypeMetadataUsesTheDocumentedRequiredShapeAndModelOrder ()
    {
        const string StableId = "tests.type-metadata.contributor";
        var contributor = new TestModelContributor(
            StableId,
            context =>
                new[]
                {
                    new JsonContractModelContribution(
                        context.RootTarget,
                        "productHint",
                        JsonSerializer.SerializeToElement("consumer"),
                        StableId),
                });

        JsonContractGenerationResult result =
            GenerationTestHarness.Generate<TypeMetadataContract>(
                "tests.type-metadata-contract",
                modelContributors: new[] { contributor });

        using JsonDocument document = JsonDocument.Parse(
            result.GetTypeMetadataUtf8());
        JsonElement metadata = document.RootElement;
        Assert.Equal(
            new[]
            {
                "contractId",
                "contractDigest",
                "schemaName",
                "root",
                "definitions",
                "contributions",
            },
            PropertyNames(metadata));
        Assert.Equal(JsonValueKind.Null, metadata.GetProperty("schemaName").ValueKind);
        AssertNode(result.Model.Root, metadata.GetProperty("root"));

        JsonElement[] definitions = metadata
            .GetProperty("definitions")
            .EnumerateArray()
            .ToArray();
        Assert.Equal(result.Model.Definitions.Count, definitions.Length);
        for (int index = 0; index < definitions.Length; index++)
        {
            Assert.Equal(new[] { "id", "value" }, PropertyNames(definitions[index]));
            Assert.Equal(
                result.Model.Definitions[index].Id,
                definitions[index].GetProperty("id").GetString());
            AssertNode(
                result.Model.Definitions[index].Value,
                definitions[index].GetProperty("value"));
        }

        JsonElement[] contributions = metadata
            .GetProperty("contributions")
            .EnumerateArray()
            .ToArray();
        Assert.Equal(result.Model.Contributions.Count, contributions.Length);
        for (int index = 0; index < contributions.Length; index++)
        {
            Assert.Equal(
                new[] { "targetPointer", "name", "value", "sourceId" },
                PropertyNames(contributions[index]));
            Assert.Equal(
                result.Model.Contributions[index].Target.Pointer,
                contributions[index].GetProperty("targetPointer").GetString());
            Assert.Equal(
                result.Model.Contributions[index].Name,
                contributions[index].GetProperty("name").GetString());
            Assert.Equal(
                result.Model.Contributions[index].SourceId,
                contributions[index].GetProperty("sourceId").GetString());
        }
    }

    private static void AssertNode (
        JsonContractNode model,
        JsonElement metadata)
    {
        Assert.Equal(
            new[]
            {
                "kind",
                "isNullable",
                "scalarKind",
                "annotations",
                "constraints",
                "constant",
                "allowedValues",
                "referenceId",
                "items",
                "additionalProperties",
                "properties",
                "variants",
                "discriminator",
            },
            PropertyNames(metadata));
        Assert.Equal(
            new[] { "title", "description", "examples" },
            PropertyNames(metadata.GetProperty("annotations")));
        Assert.Equal(
            new[]
            {
                "minimum",
                "exclusiveMinimum",
                "maximum",
                "exclusiveMaximum",
                "minimumLength",
                "maximumLength",
                "minimumItems",
                "maximumItems",
                "minimumProperties",
                "maximumProperties",
                "pattern",
                "format",
            },
            PropertyNames(metadata.GetProperty("constraints")));

        JsonElement[] properties = metadata
            .GetProperty("properties")
            .EnumerateArray()
            .ToArray();
        Assert.Equal(model.Properties.Count, properties.Length);
        for (int index = 0; index < properties.Length; index++)
        {
            Assert.Equal(
                new[] { "name", "isRequired", "value" },
                PropertyNames(properties[index]));
            Assert.Equal(
                model.Properties[index].Name,
                properties[index].GetProperty("name").GetString());
            AssertNode(
                model.Properties[index].Value,
                properties[index].GetProperty("value"));
        }

        JsonElement[] variants = metadata
            .GetProperty("variants")
            .EnumerateArray()
            .ToArray();
        Assert.Equal(model.Variants.Count, variants.Length);
        for (int index = 0; index < variants.Length; index++)
        {
            Assert.Equal(
                new[]
                {
                    "name",
                    "value",
                    "discriminatorValue",
                },
                PropertyNames(variants[index]));
            Assert.Equal(
                model.Variants[index].Name,
                variants[index].GetProperty("name").GetString());
            Assert.Equal(
                model.Variants[index].DiscriminatorValue.GetRawText(),
                variants[index]
                    .GetProperty("discriminatorValue")
                    .GetRawText());
            AssertNode(
                model.Variants[index].Value,
                variants[index].GetProperty("value"));
        }

        AssertOptionalNode(model.Items, metadata.GetProperty("items"));
        AssertOptionalNode(
            model.AdditionalProperties,
            metadata.GetProperty("additionalProperties"));

        JsonElement discriminator = metadata.GetProperty("discriminator");
        if (model.Discriminator is null)
        {
            Assert.Equal(JsonValueKind.Null, discriminator.ValueKind);
        }
        else
        {
            Assert.Equal(new[] { "propertyName" }, PropertyNames(discriminator));
            Assert.Equal(
                model.Discriminator.PropertyName,
                discriminator.GetProperty("propertyName").GetString());
        }
    }

    private static void AssertOptionalNode (
        JsonContractNode? model,
        JsonElement metadata)
    {
        if (model is null)
        {
            Assert.Equal(JsonValueKind.Null, metadata.ValueKind);
            return;
        }

        AssertNode(model, metadata);
    }

    private static string[] PropertyNames (JsonElement value)
    {
        return value
            .EnumerateObject()
            .Select(static property => property.Name)
            .ToArray();
    }

    [JsonPolymorphic(TypeDiscriminatorPropertyName = "$kind")]
    [JsonDerivedType(typeof(CountTypeMetadataContract), "count")]
    [JsonDerivedType(typeof(NestedTypeMetadataContract), "nested")]
    private abstract class TypeMetadataContract
    {
    }

    private sealed class CountTypeMetadataContract : TypeMetadataContract
    {
        public int Count { get; set; }
    }

    private sealed class NestedTypeMetadataContract : TypeMetadataContract
    {
        public NestedContract? Nested { get; set; }
    }

    private sealed class NestedContract
    {
        public int Value { get; set; }
    }
}
