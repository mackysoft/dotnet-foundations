using System.Text.Json.Serialization.Metadata;

namespace MackySoft.JsonSchema.Generation.Metadata;

/// <summary>
/// Exposes the effective serializer contract for one typed metadata target.
/// </summary>
/// <typeparam name="TValue"> The CLR value type described by the target contract. </typeparam>
public sealed class JsonContractMetadataContext<TValue>
{
    internal JsonContractMetadataContext (
        JsonTypeInfo<TValue> typeInfo,
        JsonTypeInfo declaringTypeInfo,
        JsonPropertyInfo? propertyInfo)
    {
        TypeInfo = typeInfo
            ?? throw new ArgumentNullException(nameof(typeInfo));
        DeclaringTypeInfo = declaringTypeInfo
            ?? throw new ArgumentNullException(nameof(declaringTypeInfo));
        PropertyInfo = propertyInfo;
    }

    /// <summary>
    /// Gets the serializer contract for <typeparamref name="TValue" /> used to
    /// build the target contract node and serialize typed metadata values.
    /// </summary>
    /// <remarks>
    /// A property-only converter or number-handling override is exposed by
    /// <see cref="PropertyInfo" /> rather than this type contract. Typed
    /// examples and constants are rejected when such an override cannot be
    /// reproduced by this <see cref="JsonTypeInfo{T}" />.
    /// </remarks>
    public JsonTypeInfo<TValue> TypeInfo { get; }

    /// <summary>
    /// Gets the serializer contract that declares <see cref="PropertyInfo" />,
    /// or <see cref="TypeInfo" /> when the target is a type contract.
    /// </summary>
    public JsonTypeInfo DeclaringTypeInfo { get; }

    /// <summary>
    /// Gets the effective serialized property, or <see langword="null" /> when
    /// the target is a type contract.
    /// </summary>
    public JsonPropertyInfo? PropertyInfo { get; }
}
