using MackySoft.JsonSchema.Generation.Extensibility;
namespace MackySoft.JsonSchema.Generation.Tests.Fixtures;

internal sealed class TestTypeMapper : IJsonContractTypeMapper
{
    private readonly Func<JsonContractTypeMapperContext, bool> canMap;

    private readonly Func<JsonContractTypeMapperContext, JsonContractTypeMapping> map;

    internal TestTypeMapper (
        string stableId,
        Func<JsonContractTypeMapperContext, bool> canMap,
        Func<JsonContractTypeMapperContext, JsonContractTypeMapping> map,
        string contractVersion = "1")
    {
        StableId = stableId;
        ContractVersion = contractVersion;
        this.canMap = canMap;
        this.map = map;
    }

    public string StableId { get; }

    public string ContractVersion { get; }

    public bool CanMap (JsonContractTypeMapperContext context)
    {
        return canMap(context);
    }

    public JsonContractTypeMapping Map (JsonContractTypeMapperContext context)
    {
        return map(context);
    }
}
