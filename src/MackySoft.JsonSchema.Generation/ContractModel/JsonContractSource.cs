using System.Reflection;

namespace MackySoft.JsonSchema.Generation.ContractModel;

/// <summary> Identifies the CLR declaration from which a contract model element was derived. </summary>
public sealed class JsonContractSource
{
    internal JsonContractSource (
        Type targetType,
        MemberInfo? member)
    {
        TargetType = targetType ?? throw new ArgumentNullException(nameof(targetType));
        Member = member;
    }

    /// <summary> Gets the CLR value or contract type represented by this source. </summary>
    public Type TargetType { get; }

    /// <summary>
    /// Gets the declaring member from which the value contract was derived, or
    /// <see langword="null" /> when the source is the contract type itself.
    /// </summary>
    public MemberInfo? Member { get; }
}
