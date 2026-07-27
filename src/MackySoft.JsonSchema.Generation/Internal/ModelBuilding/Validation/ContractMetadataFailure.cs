using MackySoft.JsonSchema.Generation.Diagnostics;
using MackySoft.JsonSchema.Generation.Internal.Metadata.Contracts;

namespace MackySoft.JsonSchema.Generation.Internal.ModelBuilding.Validation;

/// <summary> Creates the classified failures shared by model metadata validators. </summary>
internal static class ContractMetadataFailure
{
    public static JsonContractGenerationException Conflicting (
        string contractId,
        Type targetType,
        string? jsonPropertyName,
        IEnumerable<string> sourceIds,
        string message)
    {
        return new JsonContractGenerationException(
            JsonContractGenerationFailureKind.ConflictingMetadata,
            message,
            contractId,
            targetType,
            jsonPropertyName,
            MetadataFailure.SortSourceIds(sourceIds));
    }

    public static JsonContractGenerationException Invalid (
        string contractId,
        Type targetType,
        string? jsonPropertyName,
        string message)
    {
        return new JsonContractGenerationException(
            JsonContractGenerationFailureKind.InvalidMetadataValue,
            message,
            contractId,
            targetType,
            jsonPropertyName);
    }
}
