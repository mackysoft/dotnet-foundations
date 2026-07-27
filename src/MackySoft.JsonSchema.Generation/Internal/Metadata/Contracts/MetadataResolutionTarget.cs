namespace MackySoft.JsonSchema.Generation.Internal.Metadata.Contracts;

internal readonly struct MetadataResolutionTarget
{
    internal MetadataResolutionTarget (
        string contractId,
        Type targetType,
        string? jsonPropertyName,
        bool isMember)
    {
        ContractId = contractId;
        TargetType = targetType;
        JsonPropertyName = jsonPropertyName;
        IsMember = isMember;
    }

    internal string ContractId { get; }

    internal Type TargetType { get; }

    internal string? JsonPropertyName { get; }

    internal bool IsMember { get; }
}
