using System.Text.Json;
using System.Text.Json.Serialization;

namespace MackySoft.Text.Vocabularies.Json;

/// <summary> Creates strict JSON converters for declared text vocabulary types. </summary>
/// <remarks>
/// Vocabulary discovery and validation are delegated to <see cref="Vocabulary" />. JSON values and property names
/// are resolved as canonical strings without input normalization.
/// </remarks>
public sealed class VocabularyJsonConverterFactory : JsonConverterFactory
{
    /// <inheritdoc />
    /// <exception cref="InvalidOperationException"> Thrown when a declared vocabulary definition is invalid. </exception>
    public override bool CanConvert (Type typeToConvert)
    {
        return Vocabulary.IsVocabulary(typeToConvert);
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException"> Thrown when <paramref name="typeToConvert" /> is not a vocabulary type. </exception>
    /// <exception cref="InvalidOperationException"> Thrown when a declared vocabulary definition is invalid. </exception>
    public override JsonConverter CreateConverter (
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (!CanConvert(typeToConvert))
        {
            throw new ArgumentException(
                $"Type '{typeToConvert.FullName}' does not declare a text vocabulary.",
                nameof(typeToConvert));
        }

        return (JsonConverter)Activator.CreateInstance(
            typeof(VocabularyJsonConverter<>).MakeGenericType(typeToConvert))!;
    }

    private sealed class VocabularyJsonConverter<TValue> : JsonConverter<TValue>
        where TValue : struct, Enum
    {
        public override TValue Read (
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException(
                    $"Vocabulary value '{typeof(TValue).FullName}' must be encoded as a JSON string.");
            }

            var text = reader.GetString();
            if (!Vocabulary.TryGetValue(text, out TValue value))
            {
                throw new JsonException(
                    $"JSON string '{text}' is not declared by vocabulary '{typeof(TValue).FullName}'.");
            }

            return value;
        }

        public override TValue ReadAsPropertyName (
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException(
                    $"Vocabulary key '{typeof(TValue).FullName}' must be encoded as a JSON property name.");
            }

            var text = reader.GetString();
            if (!Vocabulary.TryGetValue(text, out TValue value))
            {
                throw new JsonException(
                    $"JSON property name '{text}' is not declared by vocabulary '{typeof(TValue).FullName}'.");
            }

            return value;
        }

        public override void Write (
            Utf8JsonWriter writer,
            TValue value,
            JsonSerializerOptions options)
        {
            if (!Vocabulary.TryGetText(value, out var text))
            {
                throw new JsonException(
                    $"Value '{value}' is not declared by vocabulary '{typeof(TValue).FullName}'.");
            }

            writer.WriteStringValue(text);
        }

        public override void WriteAsPropertyName (
            Utf8JsonWriter writer,
            TValue value,
            JsonSerializerOptions options)
        {
            if (!Vocabulary.TryGetText(value, out var text))
            {
                throw new JsonException(
                    $"Value '{value}' is not declared by vocabulary '{typeof(TValue).FullName}'.");
            }

            writer.WritePropertyName(text);
        }
    }
}
