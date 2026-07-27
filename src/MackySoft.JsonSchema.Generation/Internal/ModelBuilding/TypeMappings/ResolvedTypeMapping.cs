using MackySoft.JsonSchema.Generation.Extensibility;

namespace MackySoft.JsonSchema.Generation.Internal.ModelBuilding.TypeMappings;

/// <summary>
/// Couples one mapper with the mapping it authoritatively returned.
/// </summary>
internal sealed class ResolvedTypeMapping
{
    internal ResolvedTypeMapping (
        IJsonContractTypeMapper mapper,
        JsonContractTypeMapping mapping)
    {
        Mapper = mapper
            ?? throw new ArgumentNullException(nameof(mapper));
        Mapping = mapping
            ?? throw new ArgumentNullException(nameof(mapping));
    }

    internal IJsonContractTypeMapper Mapper { get; }

    internal JsonContractTypeMapping Mapping { get; }
}
