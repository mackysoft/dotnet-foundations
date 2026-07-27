namespace MackySoft.JsonSchema.Generation.Extensibility;

/// <summary> Declares delivery-only <c>x-</c> vendor annotations for a completed JSON Schema document. </summary>
public interface IJsonSchemaDocumentPostProcessor : IJsonContractExtension
{
    /// <summary> Gets the complete finite vendor-extension snapshot for a completed base document. </summary>
    /// <returns> Additive delivery annotations only. An empty list makes no change. </returns>
    IReadOnlyList<JsonSchemaVendorExtension> GetVendorExtensions (JsonSchemaDocumentContext context);
}
