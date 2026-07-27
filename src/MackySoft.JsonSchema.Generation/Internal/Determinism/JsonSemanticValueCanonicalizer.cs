using System.Buffers;
using System.Text.Json;
using MackySoft.Json.Canonicalization;

namespace MackySoft.JsonSchema.Generation.Internal.Determinism;

/// <summary>
/// Produces a collision-free canonical representation of JSON value semantics.
/// JSON numbers are represented by an exact decimal coefficient and exponent so
/// RFC 8785 binary64 serialization cannot merge distinct contract values.
/// </summary>
internal static class JsonSemanticValueCanonicalizer
{
    internal static byte[] GetCanonicalBytes (JsonElement value)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            WriteValue(writer, value);
            writer.Flush();
        }

        return Rfc8785JsonCanonicalizer.Canonicalize(buffer.WrittenSpan);
    }

    internal static void WriteValue (
        Utf8JsonWriter writer,
        JsonElement value)
    {
        if (writer is null)
        {
            throw new ArgumentNullException(nameof(writer));
        }

        writer.WriteStartArray();
        switch (value.ValueKind)
        {
            case JsonValueKind.Null:
                writer.WriteNumberValue(0);
                break;

            case JsonValueKind.False:
                writer.WriteNumberValue(1);
                break;

            case JsonValueKind.True:
                writer.WriteNumberValue(2);
                break;

            case JsonValueKind.Number:
                writer.WriteNumberValue(3);
                writer.WriteStringValue(
                    ExactJsonNumber.Parse(value.GetRawText())
                        .ToCanonicalText());
                break;

            case JsonValueKind.String:
                writer.WriteNumberValue(4);
                writer.WriteStringValue(value.GetString());
                break;

            case JsonValueKind.Array:
                writer.WriteNumberValue(5);
                writer.WriteStartArray();
                foreach (JsonElement item in value.EnumerateArray())
                {
                    WriteValue(writer, item);
                }

                writer.WriteEndArray();
                break;

            case JsonValueKind.Object:
                writer.WriteNumberValue(6);
                writer.WriteStartObject();
                foreach (JsonProperty property in value.EnumerateObject())
                {
                    writer.WritePropertyName(property.Name);
                    WriteValue(writer, property.Value);
                }

                writer.WriteEndObject();
                break;

            default:
                throw new InvalidOperationException(
                    "An undefined JsonElement has no contract semantics.");
        }

        writer.WriteEndArray();
    }

    internal static void EnsureRfc8785PreservesNumbers (
        JsonElement original,
        ReadOnlySpan<byte> canonicalUtf8)
    {
        using JsonDocument canonical = JsonDocument.Parse(canonicalUtf8.ToArray());
        if (!HasEqualNumericSemantics(original, canonical.RootElement))
        {
            throw new InvalidOperationException(
                "RFC 8785 binary64 serialization would change a JSON number's exact contract value.");
        }
    }

    internal static int CompareNumbers (
        JsonElement left,
        JsonElement right)
    {
        if (left.ValueKind != JsonValueKind.Number)
        {
            throw new ArgumentException(
                "The value must be a JSON number.",
                nameof(left));
        }

        if (right.ValueKind != JsonValueKind.Number)
        {
            throw new ArgumentException(
                "The value must be a JSON number.",
                nameof(right));
        }

        ExactJsonNumber leftNumber =
            ExactJsonNumber.Parse(left.GetRawText());
        ExactJsonNumber rightNumber =
            ExactJsonNumber.Parse(right.GetRawText());
        return leftNumber.CompareTo(rightNumber);
    }

    private static bool HasEqualNumericSemantics (
        JsonElement left,
        JsonElement right)
    {
        if (left.ValueKind != right.ValueKind)
        {
            return false;
        }

        switch (left.ValueKind)
        {
            case JsonValueKind.Number:
                return ExactJsonNumber.Parse(left.GetRawText())
                    .CompareTo(
                        ExactJsonNumber.Parse(right.GetRawText()))
                    == 0;

            case JsonValueKind.Array:
            {
                JsonElement.ArrayEnumerator leftItems = left.EnumerateArray();
                JsonElement.ArrayEnumerator rightItems = right.EnumerateArray();
                while (leftItems.MoveNext())
                {
                    if (!rightItems.MoveNext()
                        || !HasEqualNumericSemantics(
                            leftItems.Current,
                            rightItems.Current))
                    {
                        return false;
                    }
                }

                return !rightItems.MoveNext();
            }

            case JsonValueKind.Object:
            {
                Dictionary<string, JsonElement> rightProperties =
                    right.EnumerateObject().ToDictionary(
                        static property => property.Name,
                        static property => property.Value,
                        StringComparer.Ordinal);
                int count = 0;
                foreach (JsonProperty property in left.EnumerateObject())
                {
                    count++;
                    if (!rightProperties.TryGetValue(
                            property.Name,
                            out JsonElement rightValue)
                        || !HasEqualNumericSemantics(
                            property.Value,
                            rightValue))
                    {
                        return false;
                    }
                }

                return count == rightProperties.Count;
            }

            default:
                return true;
        }
    }
}
