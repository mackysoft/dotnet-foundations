using System.Reflection;

namespace MackySoft.JsonSchema.Generation.Metadata;

/// <summary> Describes the CLR source for which a metadata provider is being queried. </summary>
public sealed class JsonContractMetadataContext
{
    internal JsonContractMetadataContext (
        Type targetType,
        MemberInfo? member,
        string? jsonPropertyName)
    {
        TargetType = targetType ?? throw new ArgumentNullException(nameof(targetType));
        Member = member;
        JsonPropertyName = jsonPropertyName;
    }

    /// <summary> Gets the CLR value type described by the queried contract source. </summary>
    public Type TargetType { get; }

    /// <summary> Gets the queried member, or <see langword="null" /> when metadata is requested for the type. </summary>
    public MemberInfo? Member { get; }

    /// <summary> Gets the exact serialized property name when the source represents a JSON property. </summary>
    public string? JsonPropertyName { get; }
}
