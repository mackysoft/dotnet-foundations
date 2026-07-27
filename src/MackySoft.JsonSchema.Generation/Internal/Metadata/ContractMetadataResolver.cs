using System.Reflection;
using System.Text.Json.Serialization.Metadata;
using MackySoft.JsonSchema.Generation.Extensibility;
using MackySoft.JsonSchema.Generation.Internal.Metadata.Contracts;
using MackySoft.JsonSchema.Generation.Internal.Metadata.Declarations;
using MackySoft.JsonSchema.Generation.Internal.Metadata.Validation;

namespace MackySoft.JsonSchema.Generation.Internal.Metadata;

internal sealed class ContractMetadataResolver
{
    private readonly ContractMetadataDeclarationCollector declarationCollector;

    internal ContractMetadataResolver (
        IReadOnlyList<MetadataExtensionRegistration> metadataExtensions)
    {
        declarationCollector =
            new ContractMetadataDeclarationCollector(metadataExtensions);
    }

    internal ResolvedContractMetadata ResolveType (
        string contractId,
        JsonTypeInfo typeInfo)
    {
        if (contractId is null)
        {
            throw new ArgumentNullException(nameof(contractId));
        }

        if (typeInfo is null)
        {
            throw new ArgumentNullException(nameof(typeInfo));
        }

        var target = new MetadataResolutionTarget(
            contractId,
            typeInfo.Type,
            jsonPropertyName: null);
        MetadataDeclarationSet declarations =
            declarationCollector.Collect(
                target,
                typeInfo,
                typeInfo,
                propertyInfo: null,
                typeInfo.Type);
        return ContractMetadataValidator.Resolve(target, declarations);
    }

    internal ResolvedContractMetadata ResolveMember (
        string contractId,
        Type targetType,
        JsonTypeInfo valueTypeInfo,
        JsonTypeInfo declaringTypeInfo,
        JsonPropertyInfo propertyInfo,
        MemberInfo member)
    {
        if (contractId is null)
        {
            throw new ArgumentNullException(nameof(contractId));
        }

        if (targetType is null)
        {
            throw new ArgumentNullException(nameof(targetType));
        }

        if (member is null)
        {
            throw new ArgumentNullException(nameof(member));
        }

        if (valueTypeInfo is null)
        {
            throw new ArgumentNullException(nameof(valueTypeInfo));
        }

        if (declaringTypeInfo is null)
        {
            throw new ArgumentNullException(nameof(declaringTypeInfo));
        }

        if (propertyInfo is null)
        {
            throw new ArgumentNullException(nameof(propertyInfo));
        }

        var target = new MetadataResolutionTarget(
            contractId,
            targetType,
            propertyInfo.Name);
        MetadataDeclarationSet declarations =
            declarationCollector.Collect(
                target,
                valueTypeInfo,
                declaringTypeInfo,
                propertyInfo,
                member);
        return ContractMetadataValidator.Resolve(target, declarations);
    }

    internal static ResolvedContractMetadata Merge (
        ResolvedContractMetadata baseline,
        ResolvedContractMetadata overlay,
        string contractId,
        Type targetType,
        string? jsonPropertyName)
    {
        if (baseline is null)
        {
            throw new ArgumentNullException(nameof(baseline));
        }

        if (overlay is null)
        {
            throw new ArgumentNullException(nameof(overlay));
        }

        if (contractId is null)
        {
            throw new ArgumentNullException(nameof(contractId));
        }

        if (targetType is null)
        {
            throw new ArgumentNullException(nameof(targetType));
        }

        var target = new MetadataResolutionTarget(
            contractId,
            targetType,
            jsonPropertyName);
        MetadataDeclarationSet declarations =
            MetadataDeclarationSet.Merge(baseline, overlay);
        return ContractMetadataValidator.Resolve(target, declarations);
    }
}
