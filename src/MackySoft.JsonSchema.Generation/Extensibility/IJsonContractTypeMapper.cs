namespace MackySoft.JsonSchema.Generation.Extensibility;

/// <summary>
/// Maps an explicitly recognized custom converter contract to a supported JSON
/// contract shape.
/// </summary>
public interface IJsonContractTypeMapper : IJsonContractExtension
{
    /// <summary>
    /// Determines whether this mapper recognizes the requested custom converter
    /// contract.
    /// </summary>
    bool CanMap (JsonContractTypeMapperContext context);

    /// <summary>
    /// Maps a custom converter contract previously recognized by
    /// <see cref="CanMap" />.
    /// </summary>
    /// <returns> The complete declared representation of the recognized type. </returns>
    JsonContractTypeMapping Map (JsonContractTypeMapperContext context);
}
