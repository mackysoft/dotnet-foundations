using MackySoft.JsonSchema.Generation.Diagnostics;
using MackySoft.JsonSchema.Generation.Internal.Determinism;
using MackySoft.JsonSchema.Generation.Metadata;
using MackySoft.Text.Vocabularies;

namespace MackySoft.JsonSchema.Generation.Internal.Metadata.Contracts;

internal static class MetadataFailure
{
    internal static JsonContractGenerationException Conflicting (
        MetadataResolutionTarget target,
        JsonContractMetadataKind metadataKind,
        IEnumerable<string> sourceIds)
    {
        return new JsonContractGenerationException(
            JsonContractGenerationFailureKind.ConflictingMetadata,
            $"The {Vocabulary.GetText(metadataKind)} metadata has unequal declarations.",
            target.ContractId,
            target.TargetType,
            target.JsonPropertyName,
            metadataKind,
            SortSourceIds(sourceIds));
    }

    internal static JsonContractGenerationException Invalid (
        MetadataResolutionTarget target,
        JsonContractMetadataKind? metadataKind,
        IEnumerable<string> sourceIds,
        string message,
        Exception? innerException = null)
    {
        return new JsonContractGenerationException(
            JsonContractGenerationFailureKind.InvalidMetadataValue,
            message,
            target.ContractId,
            target.TargetType,
            target.JsonPropertyName,
            metadataKind,
            SortSourceIds(sourceIds),
            innerException);
    }

    internal static IReadOnlyList<string> SortSourceIds (
        IEnumerable<string> sourceIds)
    {
        string[] result = sourceIds
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        Array.Sort(result, UnicodeCodePointComparer.Instance);
        return Array.AsReadOnly(result);
    }
}
