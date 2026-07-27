using System.Globalization;
using System.Numerics;
using System.Text.Json;

namespace MackySoft.JsonSchema.Generation.Metadata;

/// <summary>
/// Represents one exact JSON number token without binary floating-point
/// conversion.
/// </summary>
public sealed class JsonContractNumber
{
    private JsonContractNumber (string token)
    {
        Token = token;
    }

    /// <summary> Gets the validated JSON number token. </summary>
    public string Token { get; }

    /// <summary> Creates an exact number from a signed 64-bit integer. </summary>
    /// <param name="value"> The integer value. </param>
    /// <returns> An exact JSON number with the same mathematical value. </returns>
    public static JsonContractNumber FromInt64 (long value)
    {
        return new JsonContractNumber(
            value.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary> Creates an exact number from an unsigned 64-bit integer. </summary>
    /// <param name="value"> The integer value. </param>
    /// <returns> An exact JSON number with the same mathematical value. </returns>
    public static JsonContractNumber FromUInt64 (ulong value)
    {
        return new JsonContractNumber(
            value.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary> Creates an exact number from a decimal value. </summary>
    /// <param name="value"> The decimal value. </param>
    /// <returns> An exact JSON number with the same mathematical value. </returns>
    public static JsonContractNumber FromDecimal (decimal value)
    {
        return new JsonContractNumber(
            value.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary> Creates an exact number from an arbitrary-precision integer. </summary>
    /// <param name="value"> The integer value. </param>
    /// <returns> An exact JSON number with the same mathematical value. </returns>
    public static JsonContractNumber FromBigInteger (BigInteger value)
    {
        return new JsonContractNumber(
            value.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Parses one complete JSON number token without converting it to
    /// <see cref="double" /> or <see cref="float" />.
    /// </summary>
    /// <param name="token">
    /// A JSON number token without leading or trailing whitespace.
    /// </param>
    /// <returns> The validated exact number. </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="token" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="FormatException">
    /// <paramref name="token" /> is not exactly one JSON number token.
    /// </exception>
    public static JsonContractNumber Parse (string token)
    {
        if (token is null)
        {
            throw new ArgumentNullException(nameof(token));
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(
                token,
                new JsonDocumentOptions
                {
                    AllowTrailingCommas = false,
                    CommentHandling = JsonCommentHandling.Disallow,
                });
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Number
                || !string.Equals(
                    token,
                    root.GetRawText(),
                    StringComparison.Ordinal))
            {
                throw new FormatException(
                    "The value must be exactly one JSON number token without surrounding whitespace.");
            }

            return new JsonContractNumber(token);
        }
        catch (JsonException exception)
        {
            throw new FormatException(
                "The value is not a valid JSON number token.",
                exception);
        }
    }

    internal JsonElement ToJsonElement ()
    {
        using JsonDocument document = JsonDocument.Parse(Token);
        return document.RootElement.Clone();
    }
}
