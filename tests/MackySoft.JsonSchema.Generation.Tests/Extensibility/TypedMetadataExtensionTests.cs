using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using MackySoft.JsonSchema.Generation.Configuration;
using MackySoft.JsonSchema.Generation.ContractModel;
using MackySoft.JsonSchema.Generation.Diagnostics;
using MackySoft.JsonSchema.Generation.Extensibility;
using MackySoft.JsonSchema.Generation.Metadata;
using MackySoft.JsonSchema.Generation.Projection;
using MackySoft.JsonSchema.Generation.Tests.Fixtures;

namespace MackySoft.JsonSchema.Generation.Tests.Extensibility;

public sealed class TypedMetadataExtensionTests
{
    [Fact]
    [Trait("Size", "Small")]
    public void Generate_RegisteredTypedExtensions_ReceiveTheEffectiveSerializerContractObjects ()
    {
        JsonSerializerOptions serializerOptions = CreateSerializerOptions();
        serializerOptions.MakeReadOnly();
        JsonTypeInfo<ContextContract> rootTypeInfo =
            Assert.IsType<JsonTypeInfo<ContextContract>>(
                serializerOptions.GetTypeInfo(typeof(ContextContract)));
        JsonTypeInfo<string> stringTypeInfo =
            Assert.IsType<JsonTypeInfo<string>>(
                serializerOptions.GetTypeInfo(typeof(string)));
        JsonPropertyInfo wireNameProperty = Assert.Single(
            rootTypeInfo.Properties,
            static property => property.Name == "wire_name");
        var provider = new ContextMetadataProvider();
        var interpreter = new ConsumerDescriptionInterpreter();
        var registry = new JsonContractMetadataRegistry()
            .RegisterProvider(provider)
            .RegisterAttributeInterpreter<
                ConsumerDescriptionAttribute,
                string>(interpreter);

        JsonContractGenerationResult result = Generate(
            "tests.typed-metadata-context",
            rootTypeInfo,
            registry);

        JsonContractMetadataContext<ContextContract> rootContext =
            Assert.IsType<JsonContractMetadataContext<ContextContract>>(
                provider.Context);
        Assert.Same(rootTypeInfo, rootContext.TypeInfo);
        Assert.Same(rootTypeInfo, rootContext.DeclaringTypeInfo);
        Assert.Null(rootContext.PropertyInfo);

        JsonContractMetadataContext<string> propertyContext =
            Assert.Single(interpreter.Contexts);
        JsonPropertyInfo propertyInfo =
            Assert.IsAssignableFrom<JsonPropertyInfo>(
                propertyContext.PropertyInfo);
        Assert.Same(stringTypeInfo, propertyContext.TypeInfo);
        Assert.Same(rootTypeInfo, propertyContext.DeclaringTypeInfo);
        Assert.Same(wireNameProperty, propertyInfo);
        Assert.Equal("wire_name", propertyInfo.Name);

        Assert.Equal("Effective root contract", result.Model.Root.Annotations.Title);
        JsonContractNode wireName = GenerationTestHarness
            .GetProperty(result.Model.Root, "wire_name")
            .Value;
        Assert.Equal(
            "Consumer-owned description",
            wireName.Annotations.Description);
        Assert.Equal(2, wireName.Constraints.MinimumLength);
        Assert.Equal(32, wireName.Constraints.MaximumLength);
        Assert.Null(
            GenerationTestHarness
                .GetProperty(result.Model.Root, "revision")
                .Value
                .Annotations
                .Description);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_TypedExamplesAndConstants_UseObjectArrayAndPolymorphicSerialization ()
    {
        var registry = new JsonContractMetadataRegistry()
            .RegisterProvider(
                new TestMetadataProvider<Payload>(
                    "tests.typed-values.object",
                    static (context, builder) =>
                    {
                        if (context.PropertyInfo?.Name == "objectValue")
                        {
                            builder.AddExample(
                                new Payload
                                {
                                    Count = 1,
                                    Label = "example",
                                });
                            builder.SetConst(
                                new Payload
                                {
                                    Count = 2,
                                    Label = "constant",
                                });
                        }
                    }))
            .RegisterProvider(
                new TestMetadataProvider<int[]>(
                    "tests.typed-values.array",
                    static (context, builder) =>
                    {
                        if (context.PropertyInfo?.Name == "arrayValue")
                        {
                            builder.AddExample(new[] { 1, 2 });
                            builder.SetConst(new[] { 3, 4 });
                        }
                    }))
            .RegisterProvider(
                new TestMetadataProvider<Shape>(
                    "tests.typed-values.polymorphic",
                    static (context, builder) =>
                    {
                        if (context.PropertyInfo?.Name == "shapeValue")
                        {
                            builder.AddExample(
                                new Square
                                {
                                    Edge = 2,
                                });
                            builder.SetConst(
                                new Circle
                                {
                                    Radius = 3,
                                });
                        }
                    }));

        JsonContractGenerationResult result =
            GenerationTestHarness.Generate<TypedValueContract>(
                "tests.typed-metadata-values",
                CreateSerializerOptions(),
                metadataRegistry: registry);

        using JsonDocument schema = JsonDocument.Parse(
            result.GetJsonSchemaUtf8());
        JsonElement properties =
            schema.RootElement.GetProperty("properties");

        JsonElement objectSchema = properties.GetProperty("objectValue");
        Assert.Equal(
            "example",
            objectSchema
                .GetProperty("examples")[0]
                .GetProperty("wire_label")
                .GetString());
        Assert.Equal(
            1,
            objectSchema
                .GetProperty("examples")[0]
                .GetProperty("count")
                .GetInt32());
        Assert.Equal(
            "constant",
            objectSchema
                .GetProperty("const")
                .GetProperty("wire_label")
                .GetString());
        Assert.Equal(
            2,
            objectSchema
                .GetProperty("const")
                .GetProperty("count")
                .GetInt32());

        JsonElement arraySchema = properties.GetProperty("arrayValue");
        Assert.Equal(
            new[] { 1, 2 },
            arraySchema
                .GetProperty("examples")[0]
                .EnumerateArray()
                .Select(static value => value.GetInt32())
                .ToArray());
        Assert.Equal(
            new[] { 3, 4 },
            arraySchema
                .GetProperty("const")
                .EnumerateArray()
                .Select(static value => value.GetInt32())
                .ToArray());

        JsonElement shapeSchema = properties.GetProperty("shapeValue");
        Assert.Equal(
            "square",
            shapeSchema
                .GetProperty("examples")[0]
                .GetProperty("$kind")
                .GetString());
        Assert.Equal(
            2,
            shapeSchema
                .GetProperty("examples")[0]
                .GetProperty("edge")
                .GetInt32());
        Assert.Equal(
            "circle",
            shapeSchema
                .GetProperty("const")
                .GetProperty("$kind")
                .GetString());
        Assert.Equal(
            3,
            shapeSchema
                .GetProperty("const")
                .GetProperty("radius")
                .GetInt32());
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_PropertyNumberHandlingCannotBeReproducedByTypeInfo_RejectsTypedConst ()
    {
        var resolver = new DefaultJsonTypeInfoResolver();
        resolver.Modifiers.Add(
            static typeInfo =>
            {
                if (typeInfo.Type == typeof(PropertyOverrideContract))
                {
                    Assert.Single(typeInfo.Properties).NumberHandling =
                        JsonNumberHandling.WriteAsString;
                }
            });
        JsonSerializerOptions serializerOptions = CreateSerializerOptions();
        serializerOptions.TypeInfoResolver = resolver;
        var registry = new JsonContractMetadataRegistry()
            .RegisterProvider(
                new TestMetadataProvider<int>(
                    "tests.typed-values.property-number-handling",
                    static (context, builder) =>
                    {
                        if (context.PropertyInfo is not null)
                        {
                            builder.SetConst(5);
                        }
                    }));

        JsonContractGenerationException exception =
            Assert.Throws<JsonContractGenerationException>(
                () => GenerationTestHarness.Generate<PropertyOverrideContract>(
                    "tests.typed-metadata-property-number-handling",
                    serializerOptions,
                    metadataRegistry: registry));

        Assert.Equal(
            JsonContractGenerationFailureKind.InvalidMetadataValue,
            exception.FailureKind);
        Assert.Equal("count", exception.JsonPropertyName);
        Assert.Equal(
            new[] { "tests.typed-values.property-number-handling" },
            exception.SourceIds);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_UnregisteredConsumerAttribute_IsNotMaterialized ()
    {
        JsonContractGenerationResult result =
            GenerationTestHarness.Generate<UnregisteredAttributeContract>(
                "tests.unregistered-consumer-attribute",
                CreateSerializerOptions());

        Assert.Equal(
            JsonContractNodeKind.Scalar,
            GenerationTestHarness
                .GetProperty(result.Model.Root, "value")
                .Value
                .Kind);
    }

    private static JsonContractGenerationResult Generate<TContract> (
        string contractId,
        JsonTypeInfo<TContract> typeInfo,
        JsonContractMetadataRegistry metadataRegistry)
    {
        var generator = new JsonContractGenerator(
            new JsonContractGeneratorOptions(
                JsonContractGenerationSettings.ClosedObjects,
                metadataRegistry));
        return generator.Generate(
            new JsonContractGenerationRequest(
                contractId,
                typeInfo,
                new JsonSchemaDocumentOptions(
                    JsonSchemaDocumentKind.Complete,
                    id: null,
                    logicalName: null)));
    }

    private static JsonSerializerOptions CreateSerializerOptions ()
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
            UnmappedMemberHandling =
                JsonUnmappedMemberHandling.Disallow,
        };
    }

    private sealed class ContextMetadataProvider
        : IJsonContractMetadataProvider<ContextContract>
    {
        public string StableId => "tests.typed-context.provider";

        public string ContractVersion => "1";

        internal JsonContractMetadataContext<ContextContract>? Context
        {
            get;
            private set;
        }

        public void ProvideMetadata (
            JsonContractMetadataContext<ContextContract> context,
            JsonContractMetadataBuilder<ContextContract> builder)
        {
            if (context.PropertyInfo is not null)
            {
                return;
            }

            Context = context;
            builder.SetTitle("Effective root contract");
        }
    }

    private sealed class ConsumerDescriptionInterpreter
        : IJsonContractAttributeInterpreter<
            ConsumerDescriptionAttribute,
            string>
    {
        private readonly List<JsonContractMetadataContext<string>> contexts =
            new();

        public string StableId => "tests.typed-context.interpreter";

        public string ContractVersion => "1";

        internal IReadOnlyList<JsonContractMetadataContext<string>> Contexts =>
            contexts;

        public void InterpretAttribute (
            ConsumerDescriptionAttribute attribute,
            JsonContractMetadataContext<string> context,
            JsonContractMetadataBuilder<string> builder)
        {
            contexts.Add(context);
            builder.SetDescription(attribute.Description);
            builder.SetMinimumLength(2);
            builder.SetMaximumLength(32);
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    private sealed class ConsumerDescriptionAttribute : Attribute
    {
        internal ConsumerDescriptionAttribute (string description)
        {
            Description = description;
        }

        internal string Description { get; }
    }

    private sealed class ContextContract
    {
        [JsonPropertyName("wire_name")]
        [ConsumerDescription("Consumer-owned description")]
        public string DisplayName { get; set; } = string.Empty;

        [ConsumerDescription("Wrong value type")]
        public int Revision { get; set; }
    }

    private sealed class TypedValueContract
    {
        public Payload ObjectValue { get; set; } = new();

        public int[] ArrayValue { get; set; } = Array.Empty<int>();

        public Shape ShapeValue { get; set; } = new Circle();
    }

    private sealed class Payload
    {
        [JsonPropertyName("wire_label")]
        public string Label { get; set; } = string.Empty;

        public int Count { get; set; }
    }

    private sealed class PropertyOverrideContract
    {
        public int Count { get; set; }
    }

    private sealed class UnregisteredAttributeContract
    {
        [ThrowingConsumer]
        public string Value { get; set; } = string.Empty;
    }

    [AttributeUsage(AttributeTargets.Property)]
    private sealed class ThrowingConsumerAttribute : Attribute
    {
        public ThrowingConsumerAttribute ()
        {
            throw new InvalidOperationException(
                "An unregistered consumer attribute must not be materialized.");
        }
    }

    [JsonPolymorphic(TypeDiscriminatorPropertyName = "$kind")]
    [JsonDerivedType(typeof(Circle), "circle")]
    [JsonDerivedType(typeof(Square), "square")]
    private abstract class Shape
    {
    }

    private sealed class Circle : Shape
    {
        public int Radius { get; set; }
    }

    private sealed class Square : Shape
    {
        public int Edge { get; set; }
    }
}
