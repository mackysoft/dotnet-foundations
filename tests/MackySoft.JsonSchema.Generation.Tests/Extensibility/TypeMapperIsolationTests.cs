using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using MackySoft.JsonSchema.Generation.ContractModel;
using MackySoft.JsonSchema.Generation.Diagnostics;
using MackySoft.JsonSchema.Generation.Extensibility;
using MackySoft.JsonSchema.Generation.Tests.Fixtures;

namespace MackySoft.JsonSchema.Generation.Tests.Extensibility;

public sealed class TypeMapperIsolationTests
{
    [Fact]
    [Trait("Size", "Small")]
    public void Generate_MapperContextExposesExactReadOnlySerializerContracts ()
    {
        bool canMapObservedReadOnlyOptions = false;
        bool canMapObservedReadOnlyTypeInfo = false;
        bool mapObservedReadOnlyOptions = false;
        bool mapObservedReadOnlyTypeInfo = false;
        bool observedTypeContractAuthority = false;
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
                    observedTypeContractAuthority = true;
                    return false;
                }

                if (context.TypeInfo.Type != typeof(OpaqueValue))
                {
                    return false;
                }

                canMapObservedReadOnlyOptions =
                    context.TypeInfo.Options.IsReadOnly;
                canMapObservedReadOnlyTypeInfo =
                    context.TypeInfo.IsReadOnly;
                observedTypeInfo = context.TypeInfo;
                observedDeclaringTypeInfo = context.DeclaringTypeInfo;
                observedPropertyInfo = context.PropertyInfo;
                return true;
            },
            context =>
            {
                mapObservedReadOnlyOptions =
                    context.TypeInfo.Options.IsReadOnly;
                mapObservedReadOnlyTypeInfo =
                    context.TypeInfo.IsReadOnly;
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

        Assert.True(canMapObservedReadOnlyOptions);
        Assert.True(canMapObservedReadOnlyTypeInfo);
        Assert.True(mapObservedReadOnlyOptions);
        Assert.True(mapObservedReadOnlyTypeInfo);
        Assert.True(observedTypeContractAuthority);
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
        Assert.Throws<InvalidOperationException>(
            () => observedPropertyInfo.Name = "tampered");
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenCanMapMutatesSerializerOptions_ReportsMapperFailure ()
    {
        const string MapperId = "tests.mapper.mutating-can-map";
        JsonSerializerOptions callerOptions = OpaqueSerializerOptions();
        var mapper = new TestTypeMapper(
            MapperId,
            context =>
            {
                if (context.TypeInfo.Type != typeof(OpaqueValue))
                {
                    return false;
                }

                context.TypeInfo.Options.PropertyNameCaseInsensitive = true;
                return true;
            },
            static _ => JsonContractTypeMapping.Scalar(
                JsonContractScalarKind.String));

        JsonContractGenerationException exception =
            Assert.Throws<JsonContractGenerationException>(
                () => GenerationTestHarness.Generate<OpaqueContract>(
                    "tests.mapper-mutating-can-map",
                    callerOptions,
                    typeMappers: new[] { mapper }));

        Assert.Equal(
            JsonContractGenerationFailureKind.UnsupportedConverter,
            exception.FailureKind);
        Assert.Equal(typeof(OpaqueValue), exception.TargetType);
        Assert.Equal("Value", exception.JsonPropertyName);
        Assert.Equal(new[] { MapperId }, exception.SourceIds);
        Assert.False(callerOptions.PropertyNameCaseInsensitive);
        Assert.True(callerOptions.IsReadOnly);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenMapMutatesTypeInfo_ReportsMapperFailure ()
    {
        const string MapperId = "tests.mapper.mutating-map";
        JsonSerializerOptions callerOptions = OpaqueSerializerOptions();
        var mapper = new TestTypeMapper(
            MapperId,
            static context =>
                context.TypeInfo.Type == typeof(OpaqueValue),
            context =>
            {
                context.TypeInfo.NumberHandling =
                    JsonNumberHandling.AllowReadingFromString;
                return JsonContractTypeMapping.Scalar(
                    JsonContractScalarKind.String);
            });

        JsonContractGenerationException exception =
            Assert.Throws<JsonContractGenerationException>(
                () => GenerationTestHarness.Generate<OpaqueContract>(
                    "tests.mapper-mutating-map",
                    callerOptions,
                    typeMappers: new[] { mapper }));

        Assert.Equal(
            JsonContractGenerationFailureKind.UnsupportedConverter,
            exception.FailureKind);
        Assert.Equal(typeof(OpaqueValue), exception.TargetType);
        Assert.Equal("Value", exception.JsonPropertyName);
        Assert.Equal(new[] { MapperId }, exception.SourceIds);
        Assert.True(callerOptions.IsReadOnly);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenMapperAttemptsNestedContractMutation_PreservesSerializerContract ()
    {
        var resolver = new DefaultJsonTypeInfoResolver();
        var callerOptions = new JsonSerializerOptions
        {
            TypeInfoResolver = resolver,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        };
        var mapper = new TestTypeMapper(
            "tests.mapper.nested-contract-mutation",
            context =>
            {
                if (context.TypeInfo.Type != typeof(NestedContract))
                {
                    return false;
                }

                try
                {
                    Assert.Single(context.TypeInfo.Properties).Name =
                        "tampered";
                }
                catch (InvalidOperationException)
                {
                    // A read-only serializer snapshot owns this failure. The
                    // mapper may ignore it, but no partial mutation can remain.
                }

                return false;
            },
            static _ => throw new InvalidOperationException(
                "The mapper must not claim the contract."));

        JsonContractGenerationResult result =
            GenerationTestHarness.Generate<OuterContract>(
                "tests.mapper-nested-contract-isolation",
                callerOptions,
                typeMappers: new[] { mapper });

        JsonContractNode nestedNode =
            GenerationTestHarness.GetProperty(
                result.Model.Root,
                "Nested").Value;
        JsonContractNode nestedDefinition = Assert.Single(
            result.Model.Definitions,
            definition => string.Equals(
                definition.Id,
                nestedNode.ReferenceId,
                StringComparison.Ordinal)).Value;
        Assert.Equal(
            "OriginalName",
            Assert.Single(nestedDefinition.Properties).Name);
        Assert.Equal(
            "OriginalName",
            Assert.Single(
                callerOptions.GetTypeInfo(
                    typeof(NestedContract)).Properties).Name);
        Assert.True(callerOptions.IsReadOnly);
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

    private sealed class OuterContract
    {
        public NestedContract Nested { get; set; } = new();
    }

    private sealed class NestedContract
    {
        public string OriginalName { get; set; } = string.Empty;
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
