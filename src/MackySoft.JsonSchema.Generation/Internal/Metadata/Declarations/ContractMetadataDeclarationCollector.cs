using System.Reflection;
using System.Text.Json.Serialization.Metadata;
using MackySoft.JsonSchema.Generation.Extensibility;
using MackySoft.JsonSchema.Generation.Internal.Metadata.Contracts;

namespace MackySoft.JsonSchema.Generation.Internal.Metadata.Declarations;

internal sealed class ContractMetadataDeclarationCollector
{
    private readonly MetadataExtensionDeclarationCollector extensionCollector;

    internal ContractMetadataDeclarationCollector (
        IReadOnlyList<MetadataExtensionRegistration> metadataExtensions)
    {
        extensionCollector =
            new MetadataExtensionDeclarationCollector(metadataExtensions);
    }

    internal MetadataDeclarationSet Collect (
        MetadataResolutionTarget target,
        JsonTypeInfo valueTypeInfo,
        JsonTypeInfo declaringTypeInfo,
        JsonPropertyInfo? propertyInfo,
        MemberInfo attributeSource)
    {
        var declarations = new MetadataDeclarationSet();
        AttributeMetadataDeclarationCollector.Collect(
            target,
            propertyInfo is null ? null : attributeSource,
            declarations);
        var extensionRequest = new MetadataExtensionCollectionRequest(
            target,
            valueTypeInfo,
            declaringTypeInfo,
            propertyInfo,
            attributeSource);
        extensionCollector.Collect(
            extensionRequest,
            declarations);
        return declarations;
    }
}
