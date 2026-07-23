using System.Globalization;
using System.Text;
using System.Text.Json;
using Org.Webpki.Es6NumberSerialization;

namespace MackySoft.Json.Canonicalization;

/// <summary>
/// Produces the UTF-8 representation of a JSON value prescribed by RFC 8785.
/// </summary>
public static class Rfc8785JsonCanonicalizer
{
    private const int MaximumDepth = 64;

    private static readonly UTF8Encoding StrictUtf8 = new(
        encoderShouldEmitUTF8Identifier: false,
        throwOnInvalidBytes: true);

    /// <summary>
    /// Canonicalizes one strictly encoded JSON value.
    /// </summary>
    /// <param name="utf8Json">
    /// UTF-8 JSON without a byte-order mark, comments, trailing commas, or additional
    /// top-level values.
    /// </param>
    /// <returns>
    /// A newly allocated byte array containing the RFC 8785 canonical JSON. The caller
    /// owns the returned array.
    /// </returns>
    /// <exception cref="JsonCanonicalizationException">
    /// The input violates the accepted JSON or RFC 8785 input contract.
    /// </exception>
    public static byte[] Canonicalize (ReadOnlySpan<byte> utf8Json)
    {
        ValidateUtf8(utf8Json);

        if (HasUtf8ByteOrderMark(utf8Json))
        {
            throw Failure(
                JsonCanonicalizationFailureKind.InvalidJson,
                "A UTF-8 byte-order mark is not permitted.");
        }

        ValidateJsonSyntaxAndDepth(utf8Json);

        try
        {
            using JsonDocument document = JsonDocument.Parse(
                utf8Json.ToArray(),
                new JsonDocumentOptions
                {
                    AllowTrailingCommas = false,
                    CommentHandling = JsonCommentHandling.Disallow,
                    MaxDepth = MaximumDepth,
                });

            return CanonicalizeValidatedElement(document.RootElement);
        }
        catch (JsonCanonicalizationException)
        {
            throw;
        }
        catch (JsonException exception)
        {
            throw Failure(
                JsonCanonicalizationFailureKind.InvalidJson,
                "The input is not valid JSON.",
                exception);
        }
    }

    /// <summary>
    /// Canonicalizes a parsed JSON value.
    /// </summary>
    /// <param name="value">The parsed JSON value to canonicalize.</param>
    /// <returns>
    /// A newly allocated byte array containing the RFC 8785 canonical JSON. The caller
    /// owns the returned array and it does not depend on the source document lifetime.
    /// </returns>
    /// <exception cref="JsonCanonicalizationException">
    /// The value violates the RFC 8785 input contract or is undefined.
    /// </exception>
    /// <remarks>
    /// Source syntax and parser depth are owned by the parser that created <paramref name="value" />
    /// and are not re-evaluated by this entry point.
    /// </remarks>
    public static byte[] Canonicalize (JsonElement value)
    {
        try
        {
            if (value.ValueKind == JsonValueKind.Undefined)
            {
                throw Failure(
                    JsonCanonicalizationFailureKind.InvalidJson,
                    "An undefined JsonElement is not a JSON value.");
            }

            return CanonicalizeValidatedElement(value);
        }
        catch (JsonCanonicalizationException)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is InvalidOperationException
            or ObjectDisposedException)
        {
            throw Failure(
                JsonCanonicalizationFailureKind.InvalidJson,
                "The JsonElement is not available for canonicalization.",
                exception);
        }
    }

    private static byte[] CanonicalizeValidatedElement (JsonElement value)
    {
        StringBuilder builder = new();
        AppendValue(value, builder);

        try
        {
            return StrictUtf8.GetBytes(builder.ToString());
        }
        catch (EncoderFallbackException exception)
        {
            throw Failure(
                JsonCanonicalizationFailureKind.InvalidUnicode,
                "The JSON value contains invalid Unicode.",
                exception);
        }
    }

    private static void AppendValue (JsonElement value, StringBuilder builder)
    {
        Stack<TraversalFrame> frames = new();
        frames.Push(TraversalFrame.ForValue(value));

        while (frames.Count > 0)
        {
            TraversalFrame frame = frames.Pop();
            switch (frame.Kind)
            {
                case TraversalFrameKind.Value:
                    AppendValueFrame(frame.Value, builder, frames);
                    break;

                case TraversalFrameKind.Array:
                    AppendArrayFrame(frame, builder, frames);
                    break;

                case TraversalFrameKind.Object:
                    AppendObjectFrame(frame, builder, frames);
                    break;

                default:
                    throw new InvalidOperationException("Unknown canonicalization traversal frame.");
            }
        }
    }

    private static void AppendValueFrame (
        JsonElement value,
        StringBuilder builder,
        Stack<TraversalFrame> frames)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Object:
                builder.Append('{');
                frames.Push(TraversalFrame.ForObject(GetCanonicalProperties(value), index: 0));
                break;

            case JsonValueKind.Array:
                builder.Append('[');
                frames.Push(TraversalFrame.ForArray(value.EnumerateArray(), isFirst: true));
                break;

            case JsonValueKind.String:
                AppendString(GetValidatedString(value), builder);
                break;

            case JsonValueKind.Number:
                builder.Append(SerializeNumber(value));
                break;

            case JsonValueKind.True:
                builder.Append("true");
                break;

            case JsonValueKind.False:
                builder.Append("false");
                break;

            case JsonValueKind.Null:
                builder.Append("null");
                break;

            default:
                throw Failure(
                    JsonCanonicalizationFailureKind.InvalidJson,
                    "The value is not a defined JSON value.");
        }
    }

    private static List<CanonicalProperty> GetCanonicalProperties (JsonElement value)
    {
        List<CanonicalProperty> properties = new();
        HashSet<string> propertyNames = new(StringComparer.Ordinal);

        foreach (JsonProperty property in value.EnumerateObject())
        {
            string name = GetValidatedString(property);
            if (!propertyNames.Add(name))
            {
                throw Failure(
                    JsonCanonicalizationFailureKind.DuplicateProperty,
                    "The JSON object contains a duplicate property name.");
            }

            properties.Add(new CanonicalProperty(name, property.Value));
        }

        properties.Sort(static (left, right) =>
            StringComparer.Ordinal.Compare(left.Name, right.Name));

        return properties;
    }

    private static void AppendObjectFrame (
        TraversalFrame frame,
        StringBuilder builder,
        Stack<TraversalFrame> frames)
    {
        List<CanonicalProperty> properties = frame.Properties!;
        if (frame.Index >= properties.Count)
        {
            builder.Append('}');
            return;
        }

        if (frame.Index > 0)
        {
            builder.Append(',');
        }

        CanonicalProperty property = properties[frame.Index];
        AppendString(property.Name, builder);
        builder.Append(':');
        frames.Push(TraversalFrame.ForObject(properties, frame.Index + 1));
        frames.Push(TraversalFrame.ForValue(property.Value));
    }

    private static void AppendArrayFrame (
        TraversalFrame frame,
        StringBuilder builder,
        Stack<TraversalFrame> frames)
    {
        JsonElement.ArrayEnumerator enumerator = frame.ArrayEnumerator;
        if (!enumerator.MoveNext())
        {
            builder.Append(']');
            return;
        }

        if (!frame.IsFirst)
        {
            builder.Append(',');
        }

        JsonElement item = enumerator.Current;
        frames.Push(TraversalFrame.ForArray(enumerator, isFirst: false));
        frames.Push(TraversalFrame.ForValue(item));
    }

    private static string SerializeNumber (JsonElement value)
    {
        string rawNumber = value.GetRawText();
        if (!double.TryParse(
                rawNumber,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double number)
            || double.IsNaN(number)
            || double.IsInfinity(number))
        {
            throw Failure(
                JsonCanonicalizationFailureKind.NumberNotRepresentable,
                $"The JSON number {FormatForMessage(rawNumber)} is not representable as a finite IEEE 754 binary64 value.");
        }

        // Some netstandard runtimes discard the sign bit while parsing negative zero,
        // so the token sign is part of the cross-runtime check.
        if (number == 0d
            && (rawNumber[0] == '-'
                || BitConverter.DoubleToInt64Bits(number) == long.MinValue))
        {
            throw Failure(
                JsonCanonicalizationFailureKind.NegativeZero,
                "Negative zero is not permitted by the RFC 8785 input contract.");
        }

        return NumberToJson.SerializeNumber(number);
    }

    private static string GetValidatedString (JsonElement value)
    {
        try
        {
            return ValidateUtf16(value.GetString()!);
        }
        catch (ObjectDisposedException)
        {
            throw;
        }
        catch (InvalidOperationException exception)
        {
            throw Failure(
                JsonCanonicalizationFailureKind.InvalidUnicode,
                "A JSON string contains invalid Unicode.",
                exception);
        }
    }

    private static string GetValidatedString (JsonProperty property)
    {
        try
        {
            return ValidateUtf16(property.Name);
        }
        catch (ObjectDisposedException)
        {
            throw;
        }
        catch (InvalidOperationException exception)
        {
            throw Failure(
                JsonCanonicalizationFailureKind.InvalidUnicode,
                "A JSON property name contains invalid Unicode.",
                exception);
        }
    }

    private static string ValidateUtf16 (string value)
    {
        for (int index = 0; index < value.Length; index++)
        {
            char character = value[index];
            if (char.IsHighSurrogate(character))
            {
                if (index + 1 >= value.Length || !char.IsLowSurrogate(value[index + 1]))
                {
                    throw Failure(
                        JsonCanonicalizationFailureKind.InvalidUnicode,
                        "A JSON string contains an unpaired UTF-16 surrogate.");
                }

                index++;
            }
            else if (char.IsLowSurrogate(character))
            {
                throw Failure(
                    JsonCanonicalizationFailureKind.InvalidUnicode,
                    "A JSON string contains an unpaired UTF-16 surrogate.");
            }
        }

        return value;
    }

    private static void AppendString (string value, StringBuilder builder)
    {
        builder.Append('"');
        foreach (char character in value)
        {
            switch (character)
            {
                case '"':
                    builder.Append("\\\"");
                    break;

                case '\\':
                    builder.Append("\\\\");
                    break;

                case '\b':
                    builder.Append("\\b");
                    break;

                case '\t':
                    builder.Append("\\t");
                    break;

                case '\n':
                    builder.Append("\\n");
                    break;

                case '\f':
                    builder.Append("\\f");
                    break;

                case '\r':
                    builder.Append("\\r");
                    break;

                default:
                    if (character <= '\u001F')
                    {
                        builder.Append("\\u");
                        builder.Append(((int)character).ToString("x4", CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        builder.Append(character);
                    }

                    break;
            }
        }

        builder.Append('"');
    }

    private static void ValidateUtf8 (ReadOnlySpan<byte> utf8Json)
    {
        try
        {
            _ = StrictUtf8.GetCharCount(utf8Json);
        }
        catch (DecoderFallbackException exception)
        {
            throw Failure(
                JsonCanonicalizationFailureKind.InvalidUnicode,
                "The input is not valid UTF-8.",
                exception);
        }
    }

    private static void ValidateJsonSyntaxAndDepth (ReadOnlySpan<byte> utf8Json)
    {
        try
        {
            ReadAllTokens(utf8Json, MaximumDepth);
        }
        catch (JsonException exception)
        {
            if (IsValidJsonBeyondMaximumDepth(utf8Json))
            {
                throw Failure(
                    JsonCanonicalizationFailureKind.MaximumDepthExceeded,
                    $"The JSON value exceeds the maximum nesting depth of {MaximumDepth}.",
                    exception);
            }

            throw Failure(
                JsonCanonicalizationFailureKind.InvalidJson,
                "The input is not valid JSON.",
                exception);
        }
    }

    private static bool IsValidJsonBeyondMaximumDepth (ReadOnlySpan<byte> utf8Json)
    {
        try
        {
            ReadAllTokens(utf8Json, int.MaxValue);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static void ReadAllTokens (ReadOnlySpan<byte> utf8Json, int maximumDepth)
    {
        Utf8JsonReader reader = new(
            utf8Json,
            new JsonReaderOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow,
                MaxDepth = maximumDepth,
            });

        while (reader.Read())
        {
        }
    }

    private static bool HasUtf8ByteOrderMark (ReadOnlySpan<byte> value)
    {
        return value.Length >= 3
            && value[0] == 0xEF
            && value[1] == 0xBB
            && value[2] == 0xBF;
    }

    private static string FormatForMessage (string value)
    {
        const int maximumLength = 80;
        string displayValue = value.Length <= maximumLength
            ? value
            : value.Substring(0, maximumLength) + "…";

        return $"'{displayValue}'";
    }

    private static JsonCanonicalizationException Failure (
        JsonCanonicalizationFailureKind failureKind,
        string message,
        Exception? innerException = null)
    {
        return new JsonCanonicalizationException(failureKind, message, innerException);
    }

    private readonly struct CanonicalProperty
    {
        public CanonicalProperty (string name, JsonElement value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; }

        public JsonElement Value { get; }
    }

    private enum TraversalFrameKind
    {
        Value,
        Array,
        Object,
    }

    private readonly struct TraversalFrame
    {
        private TraversalFrame (
            TraversalFrameKind kind,
            JsonElement value,
            JsonElement.ArrayEnumerator arrayEnumerator,
            List<CanonicalProperty>? properties,
            int index,
            bool isFirst)
        {
            Kind = kind;
            Value = value;
            ArrayEnumerator = arrayEnumerator;
            Properties = properties;
            Index = index;
            IsFirst = isFirst;
        }

        public TraversalFrameKind Kind { get; }

        public JsonElement Value { get; }

        public JsonElement.ArrayEnumerator ArrayEnumerator { get; }

        public List<CanonicalProperty>? Properties { get; }

        public int Index { get; }

        public bool IsFirst { get; }

        public static TraversalFrame ForValue (JsonElement value)
        {
            return new TraversalFrame(
                TraversalFrameKind.Value,
                value,
                default,
                properties: null,
                index: 0,
                isFirst: false);
        }

        public static TraversalFrame ForArray (
            JsonElement.ArrayEnumerator arrayEnumerator,
            bool isFirst)
        {
            return new TraversalFrame(
                TraversalFrameKind.Array,
                default,
                arrayEnumerator,
                properties: null,
                index: 0,
                isFirst);
        }

        public static TraversalFrame ForObject (
            List<CanonicalProperty> properties,
            int index)
        {
            return new TraversalFrame(
                TraversalFrameKind.Object,
                default,
                default,
                properties,
                index,
                isFirst: false);
        }
    }
}
