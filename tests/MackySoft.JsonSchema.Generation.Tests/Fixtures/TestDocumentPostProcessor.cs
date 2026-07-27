using MackySoft.JsonSchema.Generation.Extensibility;
namespace MackySoft.JsonSchema.Generation.Tests.Fixtures;

internal sealed class TestDocumentPostProcessor : IJsonSchemaDocumentPostProcessor
{
    private readonly Func<
        JsonSchemaDocumentContext,
        IReadOnlyList<JsonSchemaVendorExtension>> getVendorExtensions;

    internal TestDocumentPostProcessor (
        string stableId,
        Func<JsonSchemaDocumentContext, IReadOnlyList<JsonSchemaVendorExtension>>
            getVendorExtensions,
        string contractVersion = "1")
    {
        StableId = stableId;
        ContractVersion = contractVersion;
        this.getVendorExtensions = getVendorExtensions;
    }

    public string StableId { get; }

    public string ContractVersion { get; }

    public IReadOnlyList<JsonSchemaVendorExtension> GetVendorExtensions (
        JsonSchemaDocumentContext context)
    {
        return getVendorExtensions(context);
    }
}
