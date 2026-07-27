using System.Buffers;
using System.Text.Json;
using MackySoft.JsonSchema.Generation.Internal.Determinism;

namespace MackySoft.JsonSchema.Generation.Internal.Metadata.Normalization;

internal static class MetadataJsonValueNormalizer
{
    internal static JsonElement Normalize (JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Undefined)
        {
            throw new InvalidOperationException(
                "Undefined is not a JSON contract value.");
        }

        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            WriteValue(writer, value);
            writer.Flush();
        }

        using JsonDocument document = JsonDocument.Parse(buffer.WrittenMemory);
        return document.RootElement.Clone();
    }

    private static void WriteValue (
        Utf8JsonWriter writer,
        JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Null:
                writer.WriteNullValue();
                break;

            case JsonValueKind.False:
                writer.WriteBooleanValue(false);
                break;

            case JsonValueKind.True:
                writer.WriteBooleanValue(true);
                break;

            case JsonValueKind.Number:
                writer.WriteRawValue(
                    ExactJsonNumber.Parse(value.GetRawText())
                        .ToJsonText(),
                    skipInputValidation: true);
                break;

            case JsonValueKind.String:
                writer.WriteStringValue(value.GetString());
                break;

            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (JsonElement item in value.EnumerateArray())
                {
                    WriteValue(writer, item);
                }

                writer.WriteEndArray();
                break;

            case JsonValueKind.Object:
                WriteObject(writer, value);
                break;

            default:
                throw new InvalidOperationException(
                    "Undefined is not a JSON contract value.");
        }
    }

    private static void WriteObject (
        Utf8JsonWriter writer,
        JsonElement value)
    {
        JsonProperty[] properties = value.EnumerateObject().ToArray();
        Array.Sort(
            properties,
            static (left, right) =>
                UnicodeCodePointComparer.Instance.Compare(
                    left.Name,
                    right.Name));

        writer.WriteStartObject();
        string? previousName = null;
        foreach (JsonProperty property in properties)
        {
            if (previousName is not null
                && string.Equals(
                    previousName,
                    property.Name,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"JSON object property '{property.Name}' is duplicated.");
            }

            writer.WritePropertyName(property.Name);
            WriteValue(writer, property.Value);
            previousName = property.Name;
        }

        writer.WriteEndObject();
    }
}
