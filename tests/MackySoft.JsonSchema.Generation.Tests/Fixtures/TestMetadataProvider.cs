using MackySoft.JsonSchema.Generation.Extensibility;
using MackySoft.JsonSchema.Generation.Metadata;

namespace MackySoft.JsonSchema.Generation.Tests.Fixtures;

internal sealed class TestMetadataProvider<TValue>
    : IJsonContractMetadataProvider<TValue>
{
    private readonly Action<
        JsonContractMetadataContext<TValue>,
        JsonContractMetadataBuilder<TValue>> provideMetadata;

    internal TestMetadataProvider (
        string stableId,
        Action<
            JsonContractMetadataContext<TValue>,
            JsonContractMetadataBuilder<TValue>> provideMetadata,
        string contractVersion = "1")
    {
        StableId = stableId;
        ContractVersion = contractVersion;
        this.provideMetadata = provideMetadata
            ?? throw new ArgumentNullException(nameof(provideMetadata));
    }

    public string StableId { get; }

    public string ContractVersion { get; }

    public void ProvideMetadata (
        JsonContractMetadataContext<TValue> context,
        JsonContractMetadataBuilder<TValue> builder)
    {
        provideMetadata(context, builder);
    }
}
