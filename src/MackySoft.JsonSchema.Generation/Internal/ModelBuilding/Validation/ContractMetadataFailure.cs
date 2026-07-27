using MackySoft.JsonSchema.Generation.Diagnostics;
using MackySoft.JsonSchema.Generation.Internal.Metadata.Contracts;
using MackySoft.JsonSchema.Generation.Metadata;

namespace MackySoft.JsonSchema.Generation.Internal.ModelBuilding.Validation;

/// <summary> Creates the classified failures shared by model metadata validators. </summary>
internal static class ContractMetadataFailure
{
    public static JsonContractGenerationException Conflicting (
        string contractId,
        Type targetType,
        string? jsonPropertyName,
        JsonContractMetadataKind? metadataKind,
        IEnumerable<string> sourceIds,
        string message)
    {
        return new JsonContractGenerationException(
            JsonContractGenerationFailureKind.ConflictingMetadata,
            message,
            contractId,
            targetType,
            jsonPropertyName,
            metadataKind,
            MetadataFailure.SortSourceIds(sourceIds));
    }

    public static JsonContractGenerationException Invalid (
        string contractId,
        Type targetType,
        string? jsonPropertyName,
        JsonContractMetadataKind? metadataKind,
        string message)
    {
        return new JsonContractGenerationException(
            JsonContractGenerationFailureKind.InvalidMetadataValue,
            message,
            contractId,
            targetType,
            jsonPropertyName,
            metadataKind);
    }
}
