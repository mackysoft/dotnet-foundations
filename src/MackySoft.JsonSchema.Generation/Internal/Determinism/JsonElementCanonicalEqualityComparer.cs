using System.Text.Json;

namespace MackySoft.JsonSchema.Generation.Internal.Determinism;

internal sealed class JsonElementCanonicalEqualityComparer :
    IEqualityComparer<JsonElement>
{
    private JsonElementCanonicalEqualityComparer ()
    {
    }

    internal static JsonElementCanonicalEqualityComparer Instance { get; } =
        new();

    public bool Equals (JsonElement left, JsonElement right)
    {
        return JsonElementUtility.CompareCanonical(left, right) == 0;
    }

    public int GetHashCode (JsonElement value)
    {
        byte[] bytes =
            JsonSemanticValueCanonicalizer.GetCanonicalBytes(value);
        unchecked
        {
            int hash = 17;
            foreach (byte item in bytes)
            {
                hash = (hash * 31) + item;
            }

            return hash;
        }
    }
}
