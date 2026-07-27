using System.Text.Json;
using MackySoft.Json.Canonicalization;

namespace MackySoft.JsonSchema.Generation.Internal.Determinism;

internal static class JsonElementUtility
{
    public static JsonElement Clone (JsonElement value)
    {
        return value.ValueKind == JsonValueKind.Undefined
            ? value
            : value.Clone();
    }

    public static JsonElement ParseStrict (string json)
    {
        if (json == null)
        {
            throw new ArgumentNullException(nameof(json));
        }

        using JsonDocument document = JsonDocument.Parse(
            json,
            new JsonDocumentOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow,
            });

        return document.RootElement.Clone();
    }

    public static int CompareCanonical (JsonElement left, JsonElement right)
    {
        byte[] leftBytes =
            JsonSemanticValueCanonicalizer.GetCanonicalBytes(left);
        byte[] rightBytes =
            JsonSemanticValueCanonicalizer.GetCanonicalBytes(right);
        int commonLength = Math.Min(leftBytes.Length, rightBytes.Length);

        for (int index = 0; index < commonLength; index++)
        {
            int comparison = leftBytes[index].CompareTo(rightBytes[index]);
            if (comparison != 0)
            {
                return comparison;
            }
        }

        return leftBytes.Length.CompareTo(rightBytes.Length);
    }

    public static byte[] GetCanonicalBytes (JsonElement value)
    {
        byte[] canonical = Rfc8785JsonCanonicalizer.Canonicalize(value);
        JsonSemanticValueCanonicalizer.EnsureRfc8785PreservesNumbers(
            value,
            canonical);
        return canonical;
    }
}
