using MackySoft.JsonSchema.Generation.Extensibility;
using MackySoft.JsonSchema.Generation.Metadata;
namespace MackySoft.JsonSchema.Generation.Tests.Fixtures;

internal sealed class TestMetadataProvider : IJsonContractMetadataProvider
{
    private readonly Func<
        JsonContractMetadataContext,
        IReadOnlyList<JsonContractMetadata>> getMetadata;

    internal TestMetadataProvider (
        string stableId,
        Func<JsonContractMetadataContext, IReadOnlyList<JsonContractMetadata>> getMetadata,
        string contractVersion = "1")
    {
        StableId = stableId;
        ContractVersion = contractVersion;
        this.getMetadata = getMetadata;
    }

    public string StableId { get; }

    public string ContractVersion { get; }

    public IReadOnlyList<JsonContractMetadata> GetMetadata (
        JsonContractMetadataContext context)
    {
        return getMetadata(context);
    }
}
