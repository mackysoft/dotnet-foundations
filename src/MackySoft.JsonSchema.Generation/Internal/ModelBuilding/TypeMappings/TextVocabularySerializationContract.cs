using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using MackySoft.JsonSchema.Generation.Extensibility;
using MackySoft.JsonSchema.Generation.Internal.ModelBuilding.TypeSystem;

namespace MackySoft.JsonSchema.Generation.Internal.ModelBuilding.TypeMappings;

/// <summary>
/// Validates vocabulary values against the effective serializer contract used
/// by their mapped type or property.
/// </summary>
internal sealed class TextVocabularySerializationContract
{
    private readonly Type targetType;
    private readonly JsonTypeInfo typeInfo;
    private readonly JsonSerializerOptions? propertyOptions;

    internal TextVocabularySerializationContract (
        JsonContractTypeMapperContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        targetType = context.TypeInfo.Type;
        typeInfo = context.TypeInfo;
        var propertyConverter =
            context.PropertyInfo?.CustomConverter;
        propertyOptions = propertyConverter is null
            ? null
            : VocabularyContractReader.CreateEffectiveOptions(
                context.DeclaringTypeInfo.Options,
                propertyConverter);
    }

    internal void EnsureCanonicalRoundTrip (
        object value,
        string canonicalText)
    {
        EnsureCanonicalSerialization(value, canonicalText);
        EnsureCanonicalDeserialization(value, canonicalText);
    }

    private void EnsureCanonicalSerialization (
        object value,
        string canonicalText)
    {
        JsonElement serialized = Serialize(value);
        if (serialized.ValueKind == JsonValueKind.String
            && string.Equals(
                serialized.GetString(),
                canonicalText,
                StringComparison.Ordinal))
        {
            return;
        }

        throw new InvalidOperationException(
            $"The effective converter does not write canonical text '{canonicalText}' for vocabulary type '{targetType.FullName}'.");
    }

    private void EnsureCanonicalDeserialization (
        object value,
        string canonicalText)
    {
        object? deserialized = Deserialize(canonicalText);
        if (object.Equals(deserialized, value))
        {
            return;
        }

        throw new InvalidOperationException(
            $"The effective converter does not resolve canonical text '{canonicalText}' to its declared value for vocabulary type '{targetType.FullName}'.");
    }

    private JsonElement Serialize (object value)
    {
        return propertyOptions is null
            ? JsonSerializer.SerializeToElement(value, typeInfo)
            : JsonSerializer.SerializeToElement(
                value,
                targetType,
                propertyOptions);
    }

    private object? Deserialize (string canonicalText)
    {
        JsonElement jsonText =
            JsonSerializer.SerializeToElement(canonicalText);
        return propertyOptions is null
            ? JsonSerializer.Deserialize(jsonText, typeInfo)
            : JsonSerializer.Deserialize(
                jsonText,
                targetType,
                propertyOptions);
    }
}
