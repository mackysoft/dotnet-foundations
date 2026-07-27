using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using MackySoft.JsonSchema.Generation.ContractModel;
using MackySoft.JsonSchema.Generation.Extensibility;
using MackySoft.JsonSchema.Generation.Tests.Fixtures;

namespace MackySoft.JsonSchema.Generation.Tests.Extensibility;

public sealed class TypeMapperIsolationTests
{
    [Fact]
    [Trait("Size", "Small")]
    public void Generate_MapperReceivesTheSameEffectiveReadOnlyContractObjectsInCanMapAndMap ()
    {
        bool observedRootContext = false;
        JsonTypeInfo? observedTypeInfo = null;
        JsonTypeInfo? observedDeclaringTypeInfo = null;
        JsonPropertyInfo? observedPropertyInfo = null;
        JsonSerializerOptions callerOptions = OpaqueSerializerOptions();
        var mapper = new TestTypeMapper(
            "tests.mapper.read-only-context",
            context =>
            {
                if (context.TypeInfo.Type == typeof(OpaqueContract))
                {
                    Assert.Same(
                        context.TypeInfo,
                        context.DeclaringTypeInfo);
                    Assert.Null(context.PropertyInfo);
                    Assert.True(context.TypeInfo.IsReadOnly);
                    observedRootContext = true;
                    return false;
                }

                if (context.TypeInfo.Type != typeof(OpaqueValue))
                {
                    return false;
                }

                Assert.True(context.TypeInfo.IsReadOnly);
                Assert.True(context.DeclaringTypeInfo.IsReadOnly);
                observedTypeInfo = context.TypeInfo;
                observedDeclaringTypeInfo = context.DeclaringTypeInfo;
                observedPropertyInfo = context.PropertyInfo;
                return true;
            },
            context =>
            {
                Assert.Same(observedTypeInfo, context.TypeInfo);
                Assert.Same(
                    observedDeclaringTypeInfo,
                    context.DeclaringTypeInfo);
                Assert.Same(observedPropertyInfo, context.PropertyInfo);
                return JsonContractTypeMapping.Scalar(
                    JsonContractScalarKind.String);
            });

        GenerationTestHarness.Generate<OpaqueContract>(
            "tests.mapper-read-only-context",
            callerOptions,
            typeMappers: new[] { mapper });

        Assert.True(observedRootContext);
        Assert.True(callerOptions.IsReadOnly);
        Assert.Same(
            callerOptions.GetTypeInfo(typeof(OpaqueValue)),
            observedTypeInfo);
        JsonTypeInfo declaringTypeInfo =
            callerOptions.GetTypeInfo(typeof(OpaqueContract));
        Assert.Same(
            declaringTypeInfo,
            observedDeclaringTypeInfo);
        Assert.True(observedDeclaringTypeInfo!.IsReadOnly);
        Assert.Same(
            Assert.Single(declaringTypeInfo.Properties),
            observedPropertyInfo);
        Assert.Equal("Value", observedPropertyInfo!.Name);
    }

    private static JsonSerializerOptions OpaqueSerializerOptions ()
    {
        var options = new JsonSerializerOptions
        {
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        };
        options.Converters.Add(new OpaqueValueConverter());
        return options;
    }

    private sealed class OpaqueContract
    {
        public OpaqueValue Value { get; set; }
    }

    private readonly record struct OpaqueValue (string Value);

    private sealed class OpaqueValueConverter : JsonConverter<OpaqueValue>
    {
        public override OpaqueValue Read (
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            return new OpaqueValue(reader.GetString() ?? string.Empty);
        }

        public override void Write (
            Utf8JsonWriter writer,
            OpaqueValue value,
            JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.Value);
        }
    }
}
