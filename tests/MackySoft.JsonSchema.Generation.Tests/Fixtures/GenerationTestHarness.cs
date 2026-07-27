using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using MackySoft.JsonSchema.Generation.Configuration;
using MackySoft.JsonSchema.Generation.ContractModel;
using MackySoft.JsonSchema.Generation.Extensibility;
using MackySoft.JsonSchema.Generation.Projection;

namespace MackySoft.JsonSchema.Generation.Tests.Fixtures;

internal static class GenerationTestHarness
{
    internal static JsonContractGenerationResult Generate<TContract> (
        string contractId,
        JsonSerializerOptions? serializerOptions = null,
        JsonSchemaDocumentOptions? documentOptions = null,
        JsonContractMetadataRegistry? metadataRegistry = null,
        IEnumerable<IJsonContractTypeMapper>? typeMappers = null,
        IEnumerable<IJsonContractModelContributor>? modelContributors = null,
        IEnumerable<IJsonSchemaDocumentPostProcessor>? documentPostProcessors = null,
        JsonContractGenerationSettings? settings = null)
    {
        serializerOptions ??= new JsonSerializerOptions
        {
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        };
        serializerOptions.TypeInfoResolver ??=
            new DefaultJsonTypeInfoResolver();
        serializerOptions.MakeReadOnly();
        JsonTypeInfo typeInfo =
            serializerOptions.GetTypeInfo(typeof(TContract));
        documentOptions ??= new JsonSchemaDocumentOptions(
            JsonSchemaDocumentKind.Complete,
            id: null,
            logicalName: null);
        settings ??= JsonContractGenerationSettings.ClosedObjects;

        var generator = new JsonContractGenerator(
            new JsonContractGeneratorOptions(
                settings,
                metadataRegistry,
                typeMappers,
                modelContributors,
                documentPostProcessors));
        var request = new JsonContractGenerationRequest(
            contractId,
            typeInfo,
            documentOptions);

        return generator.Generate(request);
    }

    internal static JsonContractProperty GetProperty (
        JsonContractNode objectNode,
        string propertyName)
    {
        return Assert.Single(
            objectNode.Properties,
            property => string.Equals(
                property.Name,
                propertyName,
                StringComparison.Ordinal));
    }

    internal static JsonElement GetTypeMetadataProperty (
        JsonElement rootMetadata,
        string propertyName)
    {
        return rootMetadata
            .GetProperty("properties")
            .EnumerateArray()
            .Single(
                property => string.Equals(
                    property.GetProperty("name").GetString(),
                    propertyName,
                    StringComparison.Ordinal));
    }
}
