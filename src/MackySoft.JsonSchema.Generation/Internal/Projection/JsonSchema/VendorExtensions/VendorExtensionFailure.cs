using MackySoft.JsonSchema.Generation.Diagnostics;

namespace MackySoft.JsonSchema.Generation.Internal.Projection.JsonSchema.VendorExtensions;

internal static class VendorExtensionFailure
{
    public static JsonContractGenerationException Invalid (
        string contractId,
        string sourceId,
        string message,
        Exception? innerException = null)
    {
        return new JsonContractGenerationException(
            JsonContractGenerationFailureKind.InvalidDocumentExtension,
            message,
            contractId: contractId,
            sourceIds: new[] { sourceId },
            innerException: innerException);
    }

    public static JsonContractGenerationException Conflict (
        string contractId,
        string message,
        IReadOnlyList<string> sourceIds)
    {
        return new JsonContractGenerationException(
            JsonContractGenerationFailureKind.DocumentExtensionConflict,
            message,
            contractId: contractId,
            sourceIds: sourceIds);
    }
}
