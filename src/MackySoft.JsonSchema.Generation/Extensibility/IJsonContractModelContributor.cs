using MackySoft.JsonSchema.Generation.ContractModel;
namespace MackySoft.JsonSchema.Generation.Extensibility;

/// <summary> Inspects a completed contract model and declares product metadata without changing its structure. </summary>
public interface IJsonContractModelContributor : IJsonContractExtension
{
    /// <summary> Gets the complete finite contribution snapshot for a completed model. </summary>
    /// <param name="context"> The completed structure and its context-scoped model targets. </param>
    /// <returns>
    /// Product metadata declarations only. Every contribution's
    /// <see cref="JsonContractModelContribution.SourceId" /> must equal this
    /// contributor's <see cref="IJsonContractExtension.StableId" />. An empty
    /// list makes no change.
    /// </returns>
    IReadOnlyList<JsonContractModelContribution> GetContributions (JsonContractModelContext context);
}
