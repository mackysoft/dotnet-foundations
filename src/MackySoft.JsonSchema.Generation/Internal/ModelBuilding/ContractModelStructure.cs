using MackySoft.JsonSchema.Generation.ContractModel;
namespace MackySoft.JsonSchema.Generation.Internal.ModelBuilding;

internal sealed class ContractModelStructure
{
    public ContractModelStructure (
        JsonContractNode root,
        IReadOnlyList<JsonContractDefinition> definitions)
    {
        Root = root;
        Definitions = definitions;
    }

    public JsonContractNode Root { get; }

    public IReadOnlyList<JsonContractDefinition> Definitions { get; }
}
