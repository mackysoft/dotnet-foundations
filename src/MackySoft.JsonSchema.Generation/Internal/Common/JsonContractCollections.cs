using System.Text.Json;
using MackySoft.JsonSchema.Generation.Internal.Determinism;

namespace MackySoft.JsonSchema.Generation.Internal.Common;

internal static class JsonContractCollections
{
    internal static IReadOnlyList<T> Copy<T> (
        IEnumerable<T> values,
        string parameterName)
    {
        if (values is null)
        {
            throw new ArgumentNullException(parameterName);
        }

        var copy = values.ToArray();
        for (var index = 0; index < copy.Length; index++)
        {
            if (copy[index] is null)
            {
                throw new ArgumentException(
                    "The collection must not contain null values.",
                    parameterName);
            }
        }

        return Array.AsReadOnly(copy);
    }

    internal static IReadOnlyList<JsonElement> CloneJsonElements (
        IEnumerable<JsonElement> values,
        string parameterName)
    {
        if (values is null)
        {
            throw new ArgumentNullException(parameterName);
        }

        return Array.AsReadOnly(
            values.Select(JsonElementUtility.Clone).ToArray());
    }

    internal static JsonElement? CloneNullableJsonElement (JsonElement? value)
    {
        return value.HasValue
            ? JsonElementUtility.Clone(value.Value)
            : null;
    }
}
