using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using MackySoft.JsonSchema.Generation.ContractModel;
using MackySoft.JsonSchema.Generation.Diagnostics;
using MackySoft.JsonSchema.Generation.Metadata;
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
            JsonContractNode reference = Assert.IsType<JsonContractNode>(
                variant.Value);
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
                variant.DiscriminatorValue?.GetString(),
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
                variant.DiscriminatorValue?.GetString(),
                definitionSchema
                    .GetProperty("properties")
                    .GetProperty("$kind")
                    .GetProperty("const")
                    .GetString());
        }
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenPolymorphismIsClosed_MatchesSerializerRoundTrip ()
    {
        const string json = """{"$kind":"circle","Radius":3.5}""";

        ShapeContract deserialized =
            Assert.IsType<CircleContract>(
                JsonSerializer.Deserialize<ShapeContract>(json));
        JsonContractGenerationResult result =
            GenerationTestHarness.Generate<ShapeContract>(
                "tests.polymorphic-runtime-match");

        Assert.Equal(3.5, ((CircleContract)deserialized).Radius);
        Assert.Equal(
            new[] { "circle", "rectangle" },
            result.Model.Root.Variants
                .Select(
                    static variant =>
                        variant.DiscriminatorValue?.GetString())
                .ToArray());
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

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenDerivedTypeIsRegisteredTwice_RejectsTheSameInvalidRegistrationAsSerializer ()
    {
        AssertRuntimeAndGenerationReject(
            "tests.polymorphic-duplicate-derived-type",
            static options =>
            {
                options.DerivedTypes.Add(
                    new JsonDerivedType(
                        typeof(ConfiguredBranch),
                        "first"));
                options.DerivedTypes.Add(
                    new JsonDerivedType(
                        typeof(ConfiguredBranch),
                        "second"));
            });
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenDerivedTypeIsNotAssignable_RejectsTheSameInvalidRegistrationAsSerializer ()
    {
        AssertRuntimeAndGenerationReject(
            "tests.polymorphic-nonassignable-derived-type",
            static options =>
                options.DerivedTypes.Add(
                    new JsonDerivedType(typeof(string), "string")));
    }

    [Theory]
    [InlineData("$id")]
    [InlineData("$ref")]
    [InlineData("$values")]
    [Trait("Size", "Small")]
    public void Generate_WhenDiscriminatorUsesReservedMetadataName_RejectsTheSameInvalidRegistrationAsSerializer (
        string propertyName)
    {
        AssertRuntimeAndGenerationReject(
            $"tests.polymorphic-reserved-{propertyName.Substring(1)}",
            static options =>
                options.DerivedTypes.Add(
                    new JsonDerivedType(
                        typeof(ConfiguredBranch),
                        "branch")),
            propertyName);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenDerivedTypeIsTheBaseType_ReportsInvalidDiscriminatorRegistration ()
    {
        AssertGenerationRejects(
            "tests.polymorphic-base-as-derived",
            static options =>
                options.DerivedTypes.Add(
                    new JsonDerivedType(
                        typeof(ConfiguredBase),
                        "base")));
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenDerivedTypeIsAbstract_ReportsInvalidDiscriminatorRegistration ()
    {
        AssertGenerationRejects(
            "tests.polymorphic-abstract-derived",
            static options =>
                options.DerivedTypes.Add(
                    new JsonDerivedType(
                        typeof(AbstractConfiguredBranch),
                        "abstract")));
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenDerivedTypeIsAnInterface_ReportsInvalidDiscriminatorRegistration ()
    {
        AssertGenerationRejects<IConfiguredBase>(
            "tests.polymorphic-interface-derived",
            static options =>
                options.DerivedTypes.Add(
                    new JsonDerivedType(
                        typeof(IConfiguredBranch),
                        "interface")));
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenDerivedTypeIsOpenGeneric_ReportsInvalidDiscriminatorRegistration ()
    {
        AssertGenerationRejects(
            "tests.polymorphic-open-generic-derived",
            static options =>
                options.DerivedTypes.Add(
                    new JsonDerivedType(
                        typeof(GenericConfiguredBranch<>),
                        "generic")));
    }

    private static void AssertRuntimeAndGenerationReject (
        string contractId,
        Action<JsonPolymorphismOptions> configure,
        string discriminatorPropertyName = "$kind")
    {
        JsonSerializerOptions runtimeOptions =
            CreateSerializerOptions<ConfiguredBase>(
            configure,
            discriminatorPropertyName);
        Assert.Throws<InvalidOperationException>(
            () => JsonSerializer.Serialize<ConfiguredBase>(
                new ConfiguredBranch(),
                runtimeOptions));

        AssertGenerationRejects(
            contractId,
            configure,
            discriminatorPropertyName);
    }

    private static void AssertGenerationRejects (
        string contractId,
        Action<JsonPolymorphismOptions> configure,
        string discriminatorPropertyName = "$kind")
    {
        AssertGenerationRejects<ConfiguredBase>(
            contractId,
            configure,
            discriminatorPropertyName);
    }

    private static void AssertGenerationRejects<TContract> (
        string contractId,
        Action<JsonPolymorphismOptions> configure,
        string discriminatorPropertyName = "$kind")
    {
        JsonContractGenerationException exception =
            Assert.Throws<JsonContractGenerationException>(
                () => GenerationTestHarness.Generate<TContract>(
                    contractId,
                    CreateSerializerOptions<TContract>(
                        configure,
                        discriminatorPropertyName)));

        Assert.Equal(
            JsonContractGenerationFailureKind.UnsupportedTypeInfo,
            exception.FailureKind);
        Assert.Equal(contractId, exception.ContractId);
        Assert.Equal(typeof(TContract), exception.TargetType);
        Assert.Equal(
            discriminatorPropertyName,
            exception.JsonPropertyName);
        Assert.Equal(
            JsonContractMetadataKind.Discriminator,
            exception.MetadataKind);
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

    private abstract class AbstractConfiguredBranch : ConfiguredBase
    {
    }

    private interface IConfiguredBase
    {
    }

    private interface IConfiguredBranch : IConfiguredBase
    {
    }

    private sealed class GenericConfiguredBranch<T> : ConfiguredBase
    {
    }
}
