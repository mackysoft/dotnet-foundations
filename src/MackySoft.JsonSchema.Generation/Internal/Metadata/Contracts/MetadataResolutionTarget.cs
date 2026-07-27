namespace MackySoft.JsonSchema.Generation.Internal.Metadata.Contracts;

internal readonly struct MetadataResolutionTarget
{
    internal MetadataResolutionTarget (
        string contractId,
        Type targetType,
        string? jsonPropertyName)
    {
        ContractId = contractId;
        TargetType = targetType;
        JsonPropertyName = jsonPropertyName;
    }

    internal string ContractId { get; }

    internal Type TargetType { get; }

    internal string? JsonPropertyName { get; }
}
