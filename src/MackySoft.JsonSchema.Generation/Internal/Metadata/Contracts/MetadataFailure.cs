using MackySoft.JsonSchema.Generation.Diagnostics;
using MackySoft.JsonSchema.Generation.Internal.Determinism;

namespace MackySoft.JsonSchema.Generation.Internal.Metadata.Contracts;

internal static class MetadataFailure
{
    internal static bool IsGenerationFailure (Exception exception)
    {
        return exception is JsonContractGenerationException;
    }

    internal static JsonContractGenerationException Conflicting (
        MetadataResolutionTarget target,
        string declarationName,
        IEnumerable<string> sourceIds)
    {
        return new JsonContractGenerationException(
            JsonContractGenerationFailureKind.ConflictingMetadata,
            $"The {declarationName} metadata has unequal declarations.",
            target.ContractId,
            target.TargetType,
            target.JsonPropertyName,
            SortSourceIds(sourceIds));
    }

    internal static JsonContractGenerationException Invalid (
        MetadataResolutionTarget target,
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
