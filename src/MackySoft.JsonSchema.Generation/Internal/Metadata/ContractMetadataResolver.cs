using System.Reflection;
using MackySoft.JsonSchema.Generation.Extensibility;
using MackySoft.JsonSchema.Generation.Internal.Metadata.Contracts;
using MackySoft.JsonSchema.Generation.Internal.Metadata.Declarations;
using MackySoft.JsonSchema.Generation.Internal.Metadata.Validation;

namespace MackySoft.JsonSchema.Generation.Internal.Metadata;

internal sealed class ContractMetadataResolver
{
    private readonly ContractMetadataDeclarationCollector declarationCollector;

    internal ContractMetadataResolver (
        IReadOnlyList<IJsonContractMetadataProvider> metadataProviders)
    {
        declarationCollector =
            new ContractMetadataDeclarationCollector(metadataProviders);
    }

    internal ResolvedContractMetadata ResolveType (
        string contractId,
        Type type)
    {
        if (contractId is null)
        {
            throw new ArgumentNullException(nameof(contractId));
        }

        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        var target = new MetadataResolutionTarget(
            contractId,
            type,
            jsonPropertyName: null,
            isMember: false);
        MetadataDeclarationSet declarations =
            declarationCollector.Collect(target, member: null);
        return ContractMetadataValidator.Resolve(target, declarations);
    }

    internal ResolvedContractMetadata ResolveMember (
        string contractId,
        Type targetType,
        MemberInfo member,
        string jsonPropertyName)
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

        if (jsonPropertyName is null)
        {
            throw new ArgumentNullException(nameof(jsonPropertyName));
        }

        var target = new MetadataResolutionTarget(
            contractId,
            targetType,
            jsonPropertyName,
            isMember: true);
        MetadataDeclarationSet declarations =
            declarationCollector.Collect(target, member);
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
            jsonPropertyName,
            isMember: jsonPropertyName is not null);
        MetadataDeclarationSet declarations =
            MetadataDeclarationSet.Merge(baseline, overlay);
        return ContractMetadataValidator.Resolve(target, declarations);
    }
}
