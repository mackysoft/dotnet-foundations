using MackySoft.JsonSchema.Generation.Internal.Common;

namespace MackySoft.JsonSchema.Generation.ContractModel;

/// <summary>
/// Represents one normalized JSON contract from which schema and descriptive metadata are projected.
/// </summary>
public sealed class JsonContractModel
{
    internal JsonContractModel (
        string contractId,
        string contractDigest,
        JsonContractNode root,
        IEnumerable<JsonContractDefinition> definitions,
        IEnumerable<JsonContractModelContribution> contributions)
    {
        ContractId = contractId ?? throw new ArgumentNullException(nameof(contractId));
        ContractDigest = contractDigest ?? throw new ArgumentNullException(nameof(contractDigest));
        Root = root ?? throw new ArgumentNullException(nameof(root));
        Definitions = JsonContractCollections.Copy(definitions, nameof(definitions));
        Contributions = JsonContractCollections.Copy(contributions, nameof(contributions));
    }

    /// <summary> Gets the caller-supplied stable identifier of the public JSON contract. </summary>
    public string ContractId { get; }

    /// <summary> Gets the lowercase SHA-256 digest of the model's canonical semantic projection and settings. </summary>
    public string ContractDigest { get; }

    /// <summary> Gets the root JSON value contract. </summary>
    public JsonContractNode Root { get; }

    /// <summary> Gets reusable definitions in deterministic identifier order. </summary>
    public IReadOnlyList<JsonContractDefinition> Definitions { get; }

    /// <summary> Gets product metadata declarations in deterministic target and name order. </summary>
    public IReadOnlyList<JsonContractModelContribution> Contributions { get; }
}
