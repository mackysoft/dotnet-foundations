using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;

namespace MackySoft.JsonSchema.Generation.Internal.Projection.JsonSchema.VendorExtensions;

internal static class JsonPointerResolver
{
    public static JsonObject ResolveSchemaObject (JsonNode root, string pointer)
    {
        if (root == null)
        {
            throw new ArgumentNullException(nameof(root));
        }

        if (pointer == null)
        {
            throw new ArgumentNullException(nameof(pointer));
        }

        JsonNode? current = root;
        string[] segments = DecodeSegments(pointer);
        int segmentIndex = 0;
        while (segmentIndex < segments.Length)
        {
            if (current is not JsonObject schema)
            {
                throw NonSchemaTarget(pointer);
            }

            string keyword = segments[segmentIndex++];
            switch (keyword)
            {
                case "properties":
                case "$defs":
                    current = ResolveSchemaMapValue(
                        schema,
                        keyword,
                        segments,
                        ref segmentIndex,
                        pointer);
                    break;

                case "items":
                case "additionalProperties":
                    current = ResolveSegment(schema, keyword, pointer);
                    break;

                case "oneOf":
                case "anyOf":
                case "allOf":
                    current = ResolveSchemaArrayValue(
                        schema,
                        keyword,
                        segments,
                        ref segmentIndex,
                        pointer);
                    break;

                default:
                    throw NonSchemaTarget(pointer);
            }
        }

        return current as JsonObject
            ?? throw NonSchemaTarget(pointer);
    }

    private static JsonNode? ResolveSchemaMapValue (
        JsonObject schema,
        string keyword,
        IReadOnlyList<string> segments,
        ref int segmentIndex,
        string pointer)
    {
        if (segmentIndex >= segments.Count)
        {
            throw NonSchemaTarget(pointer);
        }

        JsonNode? mapNode = ResolveSegment(schema, keyword, pointer);
        if (mapNode is not JsonObject map)
        {
            throw NonSchemaTarget(pointer);
        }

        return ResolveSegment(
            map,
            segments[segmentIndex++],
            pointer);
    }

    private static JsonNode? ResolveSchemaArrayValue (
        JsonObject schema,
        string keyword,
        IReadOnlyList<string> segments,
        ref int segmentIndex,
        string pointer)
    {
        if (segmentIndex >= segments.Count)
        {
            throw NonSchemaTarget(pointer);
        }

        JsonNode? arrayNode = ResolveSegment(schema, keyword, pointer);
        if (arrayNode is not JsonArray array)
        {
            throw NonSchemaTarget(pointer);
        }

        return ResolveSegment(
            array,
            segments[segmentIndex++],
            pointer);
    }

    private static string[] DecodeSegments (string pointer)
    {
        if (pointer.Length == 0)
        {
            return Array.Empty<string>();
        }

        if (pointer[0] != '/')
        {
            throw new ArgumentException(
                "A JSON Pointer must be empty or begin with '/'.",
                nameof(pointer));
        }

        var segments = new List<string>();
        int segmentStart = 1;
        while (segmentStart <= pointer.Length)
        {
            int separatorIndex = pointer.IndexOf('/', segmentStart);
            int segmentEnd = separatorIndex < 0
                ? pointer.Length
                : separatorIndex;
            segments.Add(
                DecodeSegment(
                    pointer,
                    segmentStart,
                    segmentEnd - segmentStart));

            if (separatorIndex < 0)
            {
                break;
            }

            segmentStart = separatorIndex + 1;
        }

        return segments.ToArray();
    }

    private static JsonNode? ResolveSegment (
        JsonNode? current,
        string segment,
        string pointer)
    {
        if (current is JsonObject jsonObject)
        {
            if (!jsonObject.TryGetPropertyValue(segment, out JsonNode? value))
            {
                throw new KeyNotFoundException(
                    $"JSON Pointer '{pointer}' identifies a property that does not exist.");
            }

            return value;
        }

        if (current is JsonArray jsonArray)
        {
            int index = ParseArrayIndex(segment, pointer);
            if (index >= jsonArray.Count)
            {
                throw new IndexOutOfRangeException(
                    $"JSON Pointer '{pointer}' identifies an array element that does not exist.");
            }

            return jsonArray[index];
        }

        throw new InvalidOperationException(
            $"JSON Pointer '{pointer}' traverses through a value that is not a container.");
    }

    private static int ParseArrayIndex (string segment, string pointer)
    {
        if (segment.Length == 0
            || (segment.Length > 1 && segment[0] == '0')
            || (segment[0] < '0' || segment[0] > '9'))
        {
            throw InvalidArrayIndex(segment, pointer);
        }

        if (!int.TryParse(
                segment,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out int index))
        {
            throw InvalidArrayIndex(segment, pointer);
        }

        return index;
    }

    private static string DecodeSegment (
        string pointer,
        int start,
        int length)
    {
        int end = start + length;
        int firstEscape = pointer.IndexOf('~', start, length);
        if (firstEscape < 0)
        {
            return pointer.Substring(start, length);
        }

        StringBuilder decoded = new(length);
        decoded.Append(pointer, start, firstEscape - start);

        for (int index = firstEscape; index < end; index++)
        {
            char character = pointer[index];
            if (character != '~')
            {
                decoded.Append(character);
                continue;
            }

            if (++index >= end)
            {
                throw InvalidEscape(pointer);
            }

            decoded.Append(pointer[index] switch
            {
                '0' => '~',
                '1' => '/',
                _ => throw InvalidEscape(pointer),
            });
        }

        return decoded.ToString();
    }

    private static ArgumentException InvalidArrayIndex (
        string segment,
        string pointer)
    {
        return new ArgumentException(
            $"Array token '{segment}' in JSON Pointer '{pointer}' is not a valid existing array index.",
            nameof(pointer));
    }

    private static ArgumentException InvalidEscape (string pointer)
    {
        return new ArgumentException(
            $"JSON Pointer '{pointer}' contains an invalid '~' escape.",
            nameof(pointer));
    }

    private static InvalidOperationException NonSchemaTarget (string pointer)
    {
        return new InvalidOperationException(
            $"JSON Pointer '{pointer}' does not identify a JSON Schema subschema object.");
    }
}
