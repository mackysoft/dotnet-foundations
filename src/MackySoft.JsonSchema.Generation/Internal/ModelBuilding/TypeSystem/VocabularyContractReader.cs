using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using MackySoft.JsonSchema.Generation.Diagnostics;
using MackySoft.Text.Vocabularies;

namespace MackySoft.JsonSchema.Generation.Internal.ModelBuilding.TypeSystem;

/// <summary>
/// Reads finite text contracts through <see cref="Vocabulary"/> and verifies that
/// the authoritative serializer writes the same canonical texts.
/// </summary>
internal static class VocabularyContractReader
{
    internal static bool IsVocabulary (Type type)
    {
        try
        {
            return Vocabulary.IsVocabulary(type);
        }
        catch (Exception exception)
        {
            throw new JsonContractGenerationException(
                JsonContractGenerationFailureKind.UnsupportedTypeInfo,
                $"Vocabulary type '{type.FullName}' is invalid.",
                targetType: type,
                innerException: exception);
        }
    }

    internal static void EnsureNumericRepresentation (
        string contractId,
        Type enumType,
        JsonSerializerOptions serializerOptions,
        JsonConverter? propertyConverter,
        string? jsonPropertyName)
    {
        try
        {
            foreach (object value in GetRepresentativeValues(enumType))
            {
                JsonElement serialized = SerializeValue(
                    value,
                    enumType,
                    serializerOptions,
                    propertyConverter);
                if (serialized.ValueKind == JsonValueKind.String)
                {
                    throw new JsonContractGenerationException(
                        JsonContractGenerationFailureKind.UnsupportedConverter,
                        "A closed enum-to-string contract must be declared through MackySoft.Text.Vocabularies.",
                        contractId,
                        enumType,
                        jsonPropertyName);
                }

                if (serialized.ValueKind != JsonValueKind.Number
                    || serialized.GetRawText().IndexOf('.') >= 0
                    || serialized.GetRawText().IndexOf('e') >= 0
                    || serialized.GetRawText().IndexOf('E') >= 0)
                {
                    throw new JsonContractGenerationException(
                        JsonContractGenerationFailureKind.UnsupportedConverter,
                        $"Enum converter for '{enumType.FullName}' does not write an integer JSON value.",
                        contractId,
                        enumType,
                        jsonPropertyName);
                }
            }
        }
        catch (JsonContractGenerationException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new JsonContractGenerationException(
                JsonContractGenerationFailureKind.UnsupportedConverter,
                $"Enum converter for '{enumType.FullName}' could not be interpreted deterministically.",
                contractId,
                enumType,
                jsonPropertyName,
                innerException: exception);
        }
    }

    private static JsonElement SerializeValue (
        object value,
        Type valueType,
        JsonSerializerOptions serializerOptions,
        JsonConverter? propertyConverter)
    {
        JsonSerializerOptions effectiveOptions =
            CreateEffectiveOptions(
                serializerOptions,
                propertyConverter);

        string json = JsonSerializer.Serialize(
            value,
            valueType,
            effectiveOptions);
        using JsonDocument document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    internal static JsonSerializerOptions CreateEffectiveOptions (
        JsonSerializerOptions serializerOptions,
        JsonConverter? propertyConverter)
    {
        if (propertyConverter is null)
        {
            return serializerOptions;
        }

        var effectiveOptions = new JsonSerializerOptions(serializerOptions);
        effectiveOptions.Converters.Insert(0, propertyConverter);
        return effectiveOptions;
    }

    private static IEnumerable<object> GetRepresentativeValues (Type enumType)
    {
        FieldInfo[] fields = enumType.GetFields(
            BindingFlags.Public | BindingFlags.Static);
        Array.Sort(
            fields,
            static (left, right) =>
                left.MetadataToken.CompareTo(right.MetadataToken));
        foreach (FieldInfo field in fields)
        {
            yield return field.GetValue(null)
                ?? throw new InvalidOperationException(
                    $"Enum member '{enumType.FullName}.{field.Name}' has no value.");
        }

        Type underlyingType = Enum.GetUnderlyingType(enumType);
        yield return Enum.ToObject(
            enumType,
            Convert.ChangeType(
                0,
                underlyingType,
                System.Globalization.CultureInfo.InvariantCulture));
    }

}
