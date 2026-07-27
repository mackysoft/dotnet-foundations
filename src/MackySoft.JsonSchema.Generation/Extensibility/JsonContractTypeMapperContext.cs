using System.Text.Json.Serialization.Metadata;

namespace MackySoft.JsonSchema.Generation.Extensibility;

/// <summary>
/// Provides the configured, read-only serializer contract inspected by a
/// registered type mapper.
/// </summary>
public sealed class JsonContractTypeMapperContext
{
    internal JsonContractTypeMapperContext (
        JsonTypeInfo typeInfo,
        JsonTypeInfo declaringTypeInfo,
        JsonPropertyInfo? propertyInfo)
    {
        TypeInfo = typeInfo ?? throw new ArgumentNullException(nameof(typeInfo));
        DeclaringTypeInfo = declaringTypeInfo
            ?? throw new ArgumentNullException(nameof(declaringTypeInfo));
        PropertyInfo = propertyInfo;
    }

    /// <summary>
    /// Gets the exact configured, read-only serializer contract for the value
    /// being mapped.
    /// </summary>
    /// <remarks>
    /// The mapped CLR type and owning serializer options are available from
    /// <see cref="JsonTypeInfo.Type" /> and
    /// <see cref="JsonTypeInfo.Options" />, respectively.
    /// </remarks>
    public JsonTypeInfo TypeInfo { get; }

    /// <summary>
    /// Gets the exact configured, read-only serializer contract that declares
    /// <see cref="PropertyInfo" />.
    /// </summary>
    /// <remarks>
    /// This is the same instance as <see cref="TypeInfo" /> when the mapping
    /// does not describe a property value.
    /// </remarks>
    public JsonTypeInfo DeclaringTypeInfo { get; }

    /// <summary>
    /// Gets the exact effective serializer property contract whose value is
    /// being mapped, or <see langword="null" /> for a type contract.
    /// </summary>
    /// <remarks>
    /// The serialized name comes from <see cref="JsonPropertyInfo.Name" />.
    /// <see cref="JsonPropertyInfo.CustomConverter" />, when present, takes
    /// precedence over <see cref="JsonTypeInfo.Converter" />.
    /// </remarks>
    public JsonPropertyInfo? PropertyInfo { get; }
}
