namespace MackySoft.JsonSchema.Generation.Annotations;

/// <summary>
/// Asserts that the authoritative serializer contract requires a serialized object property to be present.
/// </summary>
/// <remarks>
/// This declaration does not add requiredness to an optional serializer
/// property. Generation fails when the assertion conflicts with the resolved
/// <c>System.Text.Json</c> contract.
/// </remarks>
[AttributeUsage(
    AttributeTargets.Property | AttributeTargets.Field,
    Inherited = true)]
public sealed class JsonContractRequiredAttribute : Attribute
{
}
