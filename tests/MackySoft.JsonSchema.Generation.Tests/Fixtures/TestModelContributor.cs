using MackySoft.JsonSchema.Generation.ContractModel;
using MackySoft.JsonSchema.Generation.Extensibility;

namespace MackySoft.JsonSchema.Generation.Tests.Fixtures;

internal sealed class TestModelContributor : IJsonContractModelContributor
{
    private readonly Func<
        JsonContractModelContext,
        IReadOnlyList<JsonContractModelContribution>> getContributions;

    internal TestModelContributor (
        string stableId,
        Func<
            JsonContractModelContext,
            IReadOnlyList<JsonContractModelContribution>> getContributions,
        string contractVersion = "1")
    {
        StableId = stableId;
        ContractVersion = contractVersion;
        this.getContributions = getContributions;
    }

    public string StableId { get; }

    public string ContractVersion { get; }

    public IReadOnlyList<JsonContractModelContribution> GetContributions (
        JsonContractModelContext context)
    {
        return getContributions(context);
    }
}
