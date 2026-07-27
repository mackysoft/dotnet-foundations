using MackySoft.JsonSchema.Generation.Internal.Common;
using MackySoft.JsonSchema.Generation.Metadata;

namespace MackySoft.JsonSchema.Generation.Diagnostics;

/// <summary> Reports a typed, product-independent failure to construct or project a JSON contract. </summary>
public sealed class JsonContractGenerationException : Exception
{
    internal JsonContractGenerationException (
        JsonContractGenerationFailureKind failureKind,
        string message,
        string? contractId = null,
        Type? targetType = null,
        string? jsonPropertyName = null,
        JsonContractMetadataKind? metadataKind = null,
        IEnumerable<string>? sourceIds = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        FailureKind = failureKind;
        ContractId = contractId;
        TargetType = targetType;
        JsonPropertyName = jsonPropertyName;
        MetadataKind = metadataKind;
        SourceIds = sourceIds is null
            ? Array.AsReadOnly(Array.Empty<string>())
            : JsonContractCollections.Copy(sourceIds, nameof(sourceIds));
    }

    /// <summary> Gets the stable failure classification. </summary>
    public JsonContractGenerationFailureKind FailureKind { get; }

    /// <summary> Gets the requested contract identifier when the failure belongs to one generation request. </summary>
    public string? ContractId { get; }

    /// <summary> Gets the CLR type associated with the failure. </summary>
    public Type? TargetType { get; }

    /// <summary> Gets the exact serialized JSON property name associated with the failure. </summary>
    public string? JsonPropertyName { get; }

    /// <summary> Gets the metadata category associated with the failure. </summary>
    public JsonContractMetadataKind? MetadataKind { get; }

    /// <summary> Gets stable extension or built-in source identifiers relevant to the failure. </summary>
    public IReadOnlyList<string> SourceIds { get; }
}
