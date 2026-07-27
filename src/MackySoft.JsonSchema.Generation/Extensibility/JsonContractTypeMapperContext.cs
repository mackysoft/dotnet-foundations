using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace MackySoft.JsonSchema.Generation.Extensibility;

/// <summary>
/// Provides the configured, read-only serializer contract inspected by a
/// registered type mapper.
/// </summary>
public sealed class JsonContractTypeMapperContext
{
    internal JsonContractTypeMapperContext (
        Type targetType,
        JsonTypeInfo typeInfo,
        JsonSerializerOptions serializerOptions,
        MemberInfo? member,
        string? jsonPropertyName,
        JsonConverter? propertyConverter)
    {
        TargetType = targetType ?? throw new ArgumentNullException(nameof(targetType));
        TypeInfo = typeInfo ?? throw new ArgumentNullException(nameof(typeInfo));
        SerializerOptions = serializerOptions ?? throw new ArgumentNullException(nameof(serializerOptions));
        Member = member;
        JsonPropertyName = jsonPropertyName;
        PropertyConverter = propertyConverter;
    }

    /// <summary> Gets the CLR type being mapped. </summary>
    public Type TargetType { get; }

    /// <summary>
    /// Gets the configured, read-only serializer type information used by
    /// runtime JSON serialization.
    /// </summary>
    public JsonTypeInfo TypeInfo { get; }

    /// <summary>
    /// Gets the private, read-only serializer options snapshot that produced
    /// <see cref="TypeInfo" />.
    /// </summary>
    public JsonSerializerOptions SerializerOptions { get; }

    /// <summary> Gets the property or field whose value is being mapped, or <see langword="null" /> for a type contract. </summary>
    public MemberInfo? Member { get; }

    /// <summary> Gets the serialized property name when mapping a member value. </summary>
    public string? JsonPropertyName { get; }

    /// <summary> Gets a converter configured specifically for the member, when present. </summary>
    public JsonConverter? PropertyConverter { get; }

    /// <summary>
    /// Gets the converter that takes precedence for the mapped type or member.
    /// </summary>
    /// <remarks>
    /// A member converter takes precedence over the converter exposed by
    /// <see cref="TypeInfo" />.
    /// </remarks>
    public JsonConverter EffectiveConverter =>
        PropertyConverter ?? TypeInfo.Converter;
}
