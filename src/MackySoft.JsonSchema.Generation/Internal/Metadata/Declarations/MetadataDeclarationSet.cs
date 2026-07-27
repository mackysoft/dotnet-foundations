using MackySoft.JsonSchema.Generation.Internal.Metadata.Contracts;
using MackySoft.JsonSchema.Generation.Metadata;

namespace MackySoft.JsonSchema.Generation.Internal.Metadata.Declarations;

internal sealed class MetadataDeclarationSet
{
    private readonly List<ResolvedContractMetadata.MetadataProvenance> metadata =
        new();
    private readonly List<ResolvedContractMetadata.OneOfBranchProvenance>
        oneOfBranches =
            new();
    private readonly List<ResolvedContractMetadata.DiscriminatorProvenance>
        discriminators =
            new();

    internal IReadOnlyList<ResolvedContractMetadata.MetadataProvenance> Metadata =>
        metadata;

    internal IReadOnlyList<ResolvedContractMetadata.OneOfBranchProvenance>
        OneOfBranches =>
            oneOfBranches;

    internal IReadOnlyList<ResolvedContractMetadata.DiscriminatorProvenance>
        Discriminators =>
            discriminators;

    internal void Add (
        string sourceId,
        JsonContractMetadata metadataValue)
    {
        metadata.Add(
            new ResolvedContractMetadata.MetadataProvenance(
                sourceId,
                metadataValue));
    }

    internal void Add (ResolvedContractMetadata.MetadataProvenance declaration)
    {
        metadata.Add(declaration);
    }

    internal void Add (
        ResolvedContractMetadata.OneOfBranchProvenance declaration)
    {
        oneOfBranches.Add(declaration);
    }

    internal void Add (
        ResolvedContractMetadata.DiscriminatorProvenance declaration)
    {
        discriminators.Add(declaration);
    }

    internal static MetadataDeclarationSet Merge (
        ResolvedContractMetadata baseline,
        ResolvedContractMetadata overlay)
    {
        var result = new MetadataDeclarationSet();
        result.metadata.AddRange(baseline.MetadataDeclarations);
        result.metadata.AddRange(overlay.MetadataDeclarations);
        result.oneOfBranches.AddRange(baseline.OneOfBranchDeclarations);
        result.oneOfBranches.AddRange(overlay.OneOfBranchDeclarations);
        result.discriminators.AddRange(baseline.DiscriminatorDeclarations);
        result.discriminators.AddRange(overlay.DiscriminatorDeclarations);
        return result;
    }
}
