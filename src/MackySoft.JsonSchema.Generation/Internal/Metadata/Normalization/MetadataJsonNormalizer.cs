using System.Text.Json;
using MackySoft.JsonSchema.Generation.Internal.Determinism;

namespace MackySoft.JsonSchema.Generation.Internal.Metadata.Normalization;

internal static class MetadataJsonNormalizer
{
    internal static JsonElement ParseStrict (string json)
    {
        return JsonElementUtility.ParseStrict(json);
    }

    internal static JsonElement Normalize (JsonElement value)
    {
        byte[] canonicalBytes = JsonElementUtility.GetCanonicalBytes(value);
        using JsonDocument document = JsonDocument.Parse(canonicalBytes);
        return document.RootElement.Clone();
    }

    internal static void Validate (JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Undefined)
        {
            throw new InvalidOperationException(
                "Undefined is not a JSON contract value.");
        }

        _ = JsonElementUtility.GetCanonicalBytes(value);
    }
}
