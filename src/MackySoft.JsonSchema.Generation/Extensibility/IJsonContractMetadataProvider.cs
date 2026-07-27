using MackySoft.JsonSchema.Generation.Metadata;

namespace MackySoft.JsonSchema.Generation.Extensibility;

/// <summary>
/// Contributes typed annotations and constraints for serializer contracts of
/// one CLR value type.
/// </summary>
/// <typeparam name="TValue"> The CLR value type handled by the provider. </typeparam>
public interface IJsonContractMetadataProvider<TValue> : IJsonContractExtension
{
    /// <summary>
    /// Adds the complete finite metadata snapshot for the requested target.
    /// </summary>
    /// <param name="context"> The effective serializer contract being inspected. </param>
    /// <param name="builder"> The scoped typed declaration builder. </param>
    void ProvideMetadata (
        JsonContractMetadataContext<TValue> context,
        JsonContractMetadataBuilder<TValue> builder);
}
