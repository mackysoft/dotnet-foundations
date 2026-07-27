namespace MackySoft.JsonSchema.Generation.Annotations;

/// <summary> Declares that a contract type or member accepts any JSON value without shape constraints. </summary>
[AttributeUsage(
    AttributeTargets.Class
    | AttributeTargets.Struct
    | AttributeTargets.Interface
    | AttributeTargets.Property
    | AttributeTargets.Field,
    Inherited = true)]
public sealed class AnyValueAttribute : Attribute
{
}
