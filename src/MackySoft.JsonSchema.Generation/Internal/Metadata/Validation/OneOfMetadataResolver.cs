using MackySoft.JsonSchema.Generation.Diagnostics;
using MackySoft.JsonSchema.Generation.Internal.Determinism;
using MackySoft.JsonSchema.Generation.Internal.Metadata.Contracts;
using MackySoft.JsonSchema.Generation.Metadata;

namespace MackySoft.JsonSchema.Generation.Internal.Metadata.Validation;

internal static class OneOfMetadataResolver
{
    internal static IReadOnlyList<ResolvedOneOfBranch> ResolveBranches (
        IReadOnlyList<ResolvedContractMetadata.OneOfBranchProvenance> declarations,
        MetadataResolutionTarget target)
    {
        ResolvedContractMetadata.OneOfBranchProvenance[] sorted =
            declarations.ToArray();
        Array.Sort(
            sorted,
            static (left, right) => UnicodeCodePointComparer.Instance.Compare(
                left.Branch.Name,
                right.Branch.Name));

        var branches = new List<ResolvedOneOfBranch>(sorted.Length);
        for (int start = 0; start < sorted.Length;)
        {
            int end = start + 1;
            while (end < sorted.Length
                && string.Equals(
                    sorted[start].Branch.Name,
                    sorted[end].Branch.Name,
                    StringComparison.Ordinal))
            {
                end++;
            }

            ResolvedOneOfBranch branch = sorted[start].Branch;
            for (int index = start + 1; index < end; index++)
            {
                if (!BranchesEqual(branch, sorted[index].Branch))
                {
                    throw new JsonContractGenerationException(
                        JsonContractGenerationFailureKind.ConflictingMetadata,
                        $"The oneOf branch '{branch.Name}' has conflicting declarations.",
                        target.ContractId,
                        target.TargetType,
                        jsonPropertyName: null,
                        JsonContractMetadataKind.OneOfBranch,
                        MetadataFailure.SortSourceIds(
                            sorted
                                .Skip(start)
                                .Take(end - start)
                                .Select(
                                    static declaration =>
                                        declaration.SourceId)));
                }
            }

            branches.Add(branch);
            start = end;
        }

        return branches.AsReadOnly();
    }

    internal static string? ResolveDiscriminator (
        IReadOnlyList<ResolvedContractMetadata.DiscriminatorProvenance> declarations,
        MetadataResolutionTarget target)
    {
        if (declarations.Count == 0)
        {
            return null;
        }

        string propertyName = declarations[0].PropertyName;
        if (declarations.Any(
                declaration => !string.Equals(
                    propertyName,
                    declaration.PropertyName,
                    StringComparison.Ordinal)))
        {
            throw new JsonContractGenerationException(
                JsonContractGenerationFailureKind.ConflictingMetadata,
                "The discriminator property has conflicting declarations.",
                target.ContractId,
                target.TargetType,
                jsonPropertyName: null,
                JsonContractMetadataKind.Discriminator,
                MetadataFailure.SortSourceIds(
                    declarations.Select(
                        static declaration => declaration.SourceId)));
        }

        return propertyName;
    }

    private static bool BranchesEqual (
        ResolvedOneOfBranch left,
        ResolvedOneOfBranch right)
    {
        if (!string.Equals(left.Name, right.Name, StringComparison.Ordinal)
            || !left.RequiredProperties.SequenceEqual(
                right.RequiredProperties,
                StringComparer.Ordinal)
            || !string.Equals(
                left.Annotations.Title,
                right.Annotations.Title,
                StringComparison.Ordinal)
            || !string.Equals(
                left.Annotations.Description,
                right.Annotations.Description,
                StringComparison.Ordinal)
            || left.Annotations.Examples.Count != right.Annotations.Examples.Count
            || left.DiscriminatorValue.HasValue
                != right.DiscriminatorValue.HasValue)
        {
            return false;
        }

        if (left.DiscriminatorValue.HasValue
            && JsonElementUtility.CompareCanonical(
                left.DiscriminatorValue.Value,
                right.DiscriminatorValue!.Value) != 0)
        {
            return false;
        }

        for (int index = 0; index < left.Annotations.Examples.Count; index++)
        {
            if (JsonElementUtility.CompareCanonical(
                    left.Annotations.Examples[index],
                    right.Annotations.Examples[index]) != 0)
            {
                return false;
            }
        }

        return true;
    }
}
