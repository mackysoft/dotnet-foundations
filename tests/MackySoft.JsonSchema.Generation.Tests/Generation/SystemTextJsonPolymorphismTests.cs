using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using MackySoft.JsonSchema.Generation.ContractModel;
using MackySoft.JsonSchema.Generation.Tests.Fixtures;

namespace MackySoft.JsonSchema.Generation.Tests.Generation;

public sealed class SystemTextJsonPolymorphismTests
{
    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenPolymorphismIsClosed_InjectsEachSyntheticDiscriminatorIntoItsDefinition ()
    {
        JsonContractGenerationResult result =
            GenerationTestHarness.Generate<ShapeContract>(
                "tests.polymorphic-contract");

        Assert.Equal(JsonContractNodeKind.OneOf, result.Model.Root.Kind);
        Assert.Equal("$kind", result.Model.Root.Discriminator?.PropertyName);
        Assert.Equal(2, result.Model.Root.Variants.Count);
        Assert.Equal(2, result.Model.Definitions.Count);

        using JsonDocument schema = JsonDocument.Parse(
            result.GetJsonSchemaUtf8());
        JsonElement definitions = schema.RootElement.GetProperty("$defs");

        foreach (JsonContractVariant variant in result.Model.Root.Variants)
        {
            JsonContractNode reference = variant.Value;
            string definitionId = Assert.IsType<string>(reference.ReferenceId);
            JsonContractDefinition definition = Assert.Single(
                result.Model.Definitions,
                candidate => string.Equals(
                    candidate.Id,
                    definitionId,
                    StringComparison.Ordinal));
            JsonContractProperty discriminator = GenerationTestHarness
                .GetProperty(definition.Value, "$kind");

            Assert.True(discriminator.IsRequired);
            Assert.Equal(JsonContractNodeKind.Const, discriminator.Value.Kind);
            Assert.Equal(
                variant.DiscriminatorValue.GetString(),
                discriminator.Value.Constant?.GetString());

            JsonElement definitionSchema = definitions.GetProperty(definitionId);
            Assert.False(
                definitionSchema
                    .GetProperty("additionalProperties")
                    .GetBoolean());
            Assert.Contains(
                definitionSchema
                    .GetProperty("required")
                    .EnumerateArray(),
                propertyName => string.Equals(
                    propertyName.GetString(),
                    "$kind",
                    StringComparison.Ordinal));
            Assert.Equal(
                variant.DiscriminatorValue.GetString(),
                definitionSchema
                    .GetProperty("properties")
                    .GetProperty("$kind")
                    .GetProperty("const")
                    .GetString());
        }
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenPolymorphismIsClosed_AssociatesSerializerDiscriminatorsWithTheirDerivedContracts ()
    {
        JsonContractGenerationResult result =
            GenerationTestHarness.Generate<ShapeContract>(
                "tests.polymorphic-runtime-match");

        AssertSerializerBranchMatchesDefinition(
            result,
            new CircleContract
            {
                Radius = 3.5,
            });
        AssertSerializerBranchMatchesDefinition(
            result,
            new RectangleContract
            {
                Width = 4.5,
            });
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenDefaultTypeMetadataNameIsUsed_MatchesSerializerContract ()
    {
        static void Configure (JsonPolymorphismOptions options)
        {
            options.DerivedTypes.Add(
                new JsonDerivedType(
                    typeof(ConfiguredBranch),
                    "branch"));
        }

        string json = JsonSerializer.Serialize<ConfiguredBase>(
            new ConfiguredBranch(),
            CreateSerializerOptions<ConfiguredBase>(
                Configure,
                "$type"));
        JsonContractGenerationResult result =
            GenerationTestHarness.Generate<ConfiguredBase>(
                "tests.polymorphic-default-type-property",
                CreateSerializerOptions<ConfiguredBase>(
                    Configure,
                    "$type"));

        using JsonDocument document = JsonDocument.Parse(json);
        Assert.Equal(
            "branch",
            document.RootElement.GetProperty("$type").GetString());
        Assert.Equal(
            "$type",
            result.Model.Root.Discriminator?.PropertyName);
    }

    private static JsonSerializerOptions CreateSerializerOptions<TContract> (
        Action<JsonPolymorphismOptions> configure,
        string discriminatorPropertyName)
    {
        var resolver = new DefaultJsonTypeInfoResolver();
        resolver.Modifiers.Add(
            typeInfo =>
            {
                if (typeInfo.Type != typeof(TContract))
                {
                    return;
                }

                var polymorphism = new JsonPolymorphismOptions
                {
                    TypeDiscriminatorPropertyName =
                        discriminatorPropertyName,
                    UnknownDerivedTypeHandling =
                        JsonUnknownDerivedTypeHandling.FailSerialization,
                    IgnoreUnrecognizedTypeDiscriminators = false,
                };
                configure(polymorphism);
                typeInfo.PolymorphismOptions = polymorphism;
            });
        return new JsonSerializerOptions
        {
            TypeInfoResolver = resolver,
            UnmappedMemberHandling =
                JsonUnmappedMemberHandling.Disallow,
        };
    }

    private static void AssertSerializerBranchMatchesDefinition (
        JsonContractGenerationResult result,
        ShapeContract value)
    {
        string json = JsonSerializer.Serialize<ShapeContract>(value);
        using JsonDocument document = JsonDocument.Parse(json);
        string? discriminator = document.RootElement
            .GetProperty("$kind")
            .GetString();
        string serializedPropertyName = Assert.Single(document.RootElement
                .EnumerateObject()
, static property => property.Name != "$kind").Name;
        JsonContractVariant variant = Assert.Single(
            result.Model.Root.Variants,
            variant => string.Equals(
                variant.DiscriminatorValue.GetString(),
                discriminator,
                StringComparison.Ordinal));
        string definitionId = Assert.IsType<string>(
            variant.Value.ReferenceId);
        JsonContractDefinition definition = Assert.Single(
            result.Model.Definitions,
            candidate => string.Equals(
                candidate.Id,
                definitionId,
                StringComparison.Ordinal));

        Assert.Contains(
            definition.Value.Properties,
            property => string.Equals(
                property.Name,
                serializedPropertyName,
                StringComparison.Ordinal));
    }

    [JsonPolymorphic(TypeDiscriminatorPropertyName = "$kind")]
    [JsonDerivedType(typeof(CircleContract), "circle")]
    [JsonDerivedType(typeof(RectangleContract), "rectangle")]
    private abstract class ShapeContract
    {
    }

    private sealed class CircleContract : ShapeContract
    {
        public double Radius { get; set; }
    }

    private sealed class RectangleContract : ShapeContract
    {
        public double Width { get; set; }
    }

    private abstract class ConfiguredBase
    {
    }

    private sealed class ConfiguredBranch : ConfiguredBase
    {
    }

}
