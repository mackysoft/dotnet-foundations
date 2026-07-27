using MackySoft.JsonSchema.Generation.Metadata;

namespace MackySoft.JsonSchema.Generation.Extensibility;

/// <summary>
/// Interprets one explicitly registered consumer attribute for one CLR value
/// type.
/// </summary>
/// <typeparam name="TAttribute"> The consumer-owned attribute type. </typeparam>
/// <typeparam name="TValue"> The CLR value type handled by the interpreter. </typeparam>
public interface IJsonContractAttributeInterpreter<TAttribute, TValue>
    : IJsonContractExtension
    where TAttribute : Attribute
{
    /// <summary> Adds metadata represented by one attribute instance. </summary>
    /// <param name="attribute"> The consumer attribute being interpreted. </param>
    /// <param name="context"> The effective serializer contract being inspected. </param>
    /// <param name="builder"> The scoped typed declaration builder. </param>
    void InterpretAttribute (
        TAttribute attribute,
        JsonContractMetadataContext<TValue> context,
        JsonContractMetadataBuilder<TValue> builder);
}
