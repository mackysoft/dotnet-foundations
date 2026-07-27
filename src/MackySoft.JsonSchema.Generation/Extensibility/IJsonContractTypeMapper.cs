namespace MackySoft.JsonSchema.Generation.Extensibility;

/// <summary>
/// Maps an explicitly recognized converter or value-object representation to
/// a supported JSON contract shape.
/// </summary>
public interface IJsonContractTypeMapper : IJsonContractExtension
{
    /// <summary>
    /// Determines whether this mapper recognizes the requested serializer
    /// contract and value representation.
    /// </summary>
    /// <param name="context">
    /// The exact read-only STJ type contract and, for a property value, its
    /// declaring type and property contracts.
    /// </param>
    bool CanMap (JsonContractTypeMapperContext context);

    /// <summary>
    /// Maps a serializer contract and value representation previously
    /// recognized by <see cref="CanMap" />.
    /// </summary>
    /// <param name="context">
    /// The same exact STJ contracts supplied to the successful
    /// <see cref="CanMap" /> invocation.
    /// </param>
    /// <returns> The complete declared representation of the recognized type. </returns>
    JsonContractTypeMapping Map (JsonContractTypeMapperContext context);
}
