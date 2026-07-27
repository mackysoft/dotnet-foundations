using MackySoft.JsonSchema.Generation.Extensibility;

namespace MackySoft.JsonSchema.Generation.Internal.ModelBuilding.TypeMappings;

/// <summary>
/// Couples one mapper with the mapping it authoritatively returned.
/// </summary>
internal sealed class ResolvedTypeMapping
{
    internal ResolvedTypeMapping (
        IJsonContractTypeMapper mapper,
        JsonContractTypeMapping mapping,
        JsonContractTypeMapperContext context)
    {
        Mapper = mapper
            ?? throw new ArgumentNullException(nameof(mapper));
        Mapping = mapping
            ?? throw new ArgumentNullException(nameof(mapping));
        Context = context
            ?? throw new ArgumentNullException(nameof(context));
    }

    internal IJsonContractTypeMapper Mapper { get; }

    internal JsonContractTypeMapping Mapping { get; }

    internal JsonContractTypeMapperContext Context { get; }
}
