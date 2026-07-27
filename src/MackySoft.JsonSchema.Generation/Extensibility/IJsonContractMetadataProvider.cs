using MackySoft.JsonSchema.Generation.Metadata;
namespace MackySoft.JsonSchema.Generation.Extensibility;

/// <summary> Supplies explicit metadata for one CLR type or serialized member before model construction. </summary>
public interface IJsonContractMetadataProvider : IJsonContractExtension
{
    /// <summary> Gets the complete finite metadata declaration for the requested source. </summary>
    /// <param name="context"> The source type and optional serialized member being inspected. </param>
    /// <returns> A finite snapshot of declarations. An empty list adds no metadata. </returns>
    IReadOnlyList<JsonContractMetadata> GetMetadata (JsonContractMetadataContext context);
}
