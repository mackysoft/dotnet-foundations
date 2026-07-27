namespace MackySoft.JsonSchema.Generation.Annotations;

/// <summary>
/// Asserts that the authoritative serializer contract allows JSON <see langword="null" /> for a value.
/// </summary>
/// <remarks>
/// This declaration does not widen serializer-derived nullability. Generation
/// fails when the assertion conflicts with the CLR and
/// <c>System.Text.Json</c> contract.
/// At type scope, a reference-type root can assert the serializer's acceptance
/// of a JSON <see langword="null" /> root. The same declaration does not make a
/// non-nullable serialized member of that type nullable.
/// </remarks>
[AttributeUsage(
    AttributeTargets.Class
    | AttributeTargets.Struct
    | AttributeTargets.Interface
    | AttributeTargets.Property
    | AttributeTargets.Field,
    Inherited = true)]
public sealed class AllowNullAttribute : Attribute
{
}
