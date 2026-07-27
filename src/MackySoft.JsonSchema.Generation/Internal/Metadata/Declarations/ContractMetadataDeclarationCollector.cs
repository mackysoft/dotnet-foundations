using System.Reflection;
using MackySoft.JsonSchema.Generation.Extensibility;
using MackySoft.JsonSchema.Generation.Internal.Metadata.Contracts;

namespace MackySoft.JsonSchema.Generation.Internal.Metadata.Declarations;

internal sealed class ContractMetadataDeclarationCollector
{
    private readonly ProviderMetadataDeclarationCollector providerCollector;

    internal ContractMetadataDeclarationCollector (
        IReadOnlyList<IJsonContractMetadataProvider> metadataProviders)
    {
        providerCollector =
            new ProviderMetadataDeclarationCollector(metadataProviders);
    }

    internal MetadataDeclarationSet Collect (
        MetadataResolutionTarget target,
        MemberInfo? member)
    {
        var declarations = new MetadataDeclarationSet();
        AttributeMetadataDeclarationCollector.Collect(
            target,
            member,
            declarations);
        providerCollector.Collect(target, member, declarations);
        return declarations;
    }
}
