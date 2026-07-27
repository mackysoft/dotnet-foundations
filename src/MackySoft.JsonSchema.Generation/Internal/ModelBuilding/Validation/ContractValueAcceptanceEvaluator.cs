using System.Text.Json;
using System.Text.RegularExpressions;
using MackySoft.JsonSchema.Generation.ContractModel;
using MackySoft.JsonSchema.Generation.Internal.Determinism;
using MackySoft.JsonSchema.Generation.Internal.ModelBuilding.Shapes;

namespace MackySoft.JsonSchema.Generation.Internal.ModelBuilding.Validation;

/// <summary>
/// Evaluates whether a JSON value is accepted by a contract shape or completed
/// contract node without retaining validation registrations.
/// </summary>
internal static class ContractValueAcceptanceEvaluator
{
    internal static bool Accepts (
        JsonContractNode node,
        JsonElement value)
    {
        return AcceptsNode(
            node,
            value,
            referenceResolver: null,
            new List<ReferenceEvaluation>());
    }

    internal static bool Accepts (
        ContractNodeShape shape,
        JsonContractConstraints constraints,
        JsonElement value,
        Func<string, JsonContractNode> referenceResolver)
    {
        return AcceptsShape(
            shape,
            constraints,
            value,
            referenceResolver,
            new List<ReferenceEvaluation>());
    }

    internal static JsonContractScalarKind? GetScalarKind (JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.Null => JsonContractScalarKind.Null,
            JsonValueKind.True or JsonValueKind.False =>
                JsonContractScalarKind.Boolean,
            JsonValueKind.Number =>
                IsIntegerJsonNumber(value.GetRawText())
                    ? JsonContractScalarKind.Integer
                    : JsonContractScalarKind.Number,
            JsonValueKind.String => JsonContractScalarKind.String,
            _ => null,
        };
    }

    private static bool IsIntegerJsonNumber (string value)
    {
        return value.IndexOf('.') < 0
            && value.IndexOf('e') < 0
            && value.IndexOf('E') < 0;
    }

    private static bool AcceptsNode (
        JsonContractNode node,
        JsonElement value,
        Func<string, JsonContractNode>? referenceResolver,
        List<ReferenceEvaluation> activeReferences)
    {
        if (value.ValueKind == JsonValueKind.Null)
        {
            return node.IsNullable;
        }

        bool matches = node.Kind switch
        {
            JsonContractNodeKind.Arbitrary => true,
            JsonContractNodeKind.Scalar =>
                MatchesScalarValue(value, node.ScalarKind),
            JsonContractNodeKind.Enum => node.AllowedValues.Any(
                allowed =>
                    JsonElementUtility.CompareCanonical(allowed, value) == 0),
            JsonContractNodeKind.Const => node.Constant.HasValue
                && JsonElementUtility.CompareCanonical(
                    node.Constant.Value,
                    value) == 0,
            JsonContractNodeKind.Array =>
                AcceptsArray(
                    value,
                    node.Items,
                    node.Constraints,
                    referenceResolver,
                    activeReferences),
            JsonContractNodeKind.Object =>
                AcceptsObject(
                    value,
                    node.Properties,
                    node.AdditionalProperties,
                    node.Constraints,
                    referenceResolver,
                    activeReferences),
            JsonContractNodeKind.Dictionary =>
                AcceptsDictionary(
                    value,
                    node.AdditionalProperties,
                    node.Constraints,
                    referenceResolver,
                    activeReferences),
            JsonContractNodeKind.OneOf =>
                AcceptsOneOf(
                    value,
                    node.Variants,
                    referenceResolver,
                    activeReferences),
            JsonContractNodeKind.Reference =>
                AcceptsReference(
                    node.ReferenceId,
                    value,
                    referenceResolver,
                    activeReferences),
            _ => false,
        };
        return matches && AcceptsConstraints(value, node.Constraints);
    }

    private static bool AcceptsShape (
        ContractNodeShape shape,
        JsonContractConstraints constraints,
        JsonElement value,
        Func<string, JsonContractNode> referenceResolver,
        List<ReferenceEvaluation> activeReferences)
    {
        bool matches = shape.Kind switch
        {
            JsonContractNodeKind.Arbitrary => true,
            JsonContractNodeKind.Scalar =>
                MatchesScalarValue(value, shape.ScalarKind),
            JsonContractNodeKind.Enum => shape.AllowedValues.Any(
                allowed =>
                    JsonElementUtility.CompareCanonical(allowed, value) == 0),
            JsonContractNodeKind.Const => shape.Constant.HasValue
                && JsonElementUtility.CompareCanonical(
                    shape.Constant.Value,
                    value) == 0,
            JsonContractNodeKind.Array =>
                AcceptsArray(
                    value,
                    shape.Items,
                    constraints,
                    referenceResolver,
                    activeReferences),
            JsonContractNodeKind.Object =>
                AcceptsObject(
                    value,
                    shape.Properties,
                    shape.AdditionalProperties,
                    constraints,
                    referenceResolver,
                    activeReferences),
            JsonContractNodeKind.Dictionary =>
                AcceptsDictionary(
                    value,
                    shape.AdditionalProperties,
                    constraints,
                    referenceResolver,
                    activeReferences),
            JsonContractNodeKind.OneOf =>
                AcceptsOneOf(
                    value,
                    shape.Variants,
                    referenceResolver,
                    activeReferences),
            JsonContractNodeKind.Reference =>
                AcceptsReference(
                    shape.ReferenceId,
                    value,
                    referenceResolver,
                    activeReferences),
            _ => false,
        };
        return matches && AcceptsConstraints(value, constraints);
    }

    private static bool MatchesScalarValue (
        JsonElement value,
        JsonContractScalarKind? scalarKind)
    {
        if (!MatchesScalar(value.ValueKind, scalarKind))
        {
            return false;
        }

        return scalarKind != JsonContractScalarKind.Integer
            || IsIntegerJsonNumber(value.GetRawText());
    }

    private static bool AcceptsArray (
        JsonElement value,
        JsonContractNode? items,
        JsonContractConstraints constraints,
        Func<string, JsonContractNode>? referenceResolver,
        List<ReferenceEvaluation> activeReferences)
    {
        if (value.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        int count = value.GetArrayLength();
        if ((constraints.MinimumItems.HasValue
                && count < constraints.MinimumItems.Value)
            || (constraints.MaximumItems.HasValue
                && count > constraints.MaximumItems.Value))
        {
            return false;
        }

        return items is null
            || value.EnumerateArray().All(
                item => AcceptsNode(
                    items,
                    item,
                    referenceResolver,
                    activeReferences));
    }

    private static bool AcceptsObject (
        JsonElement value,
        IReadOnlyList<JsonContractProperty> properties,
        JsonContractNode? additionalProperties,
        JsonContractConstraints constraints,
        Func<string, JsonContractNode>? referenceResolver,
        List<ReferenceEvaluation> activeReferences)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        Dictionary<string, JsonElement> supplied = value
            .EnumerateObject()
            .ToDictionary(
                static property => property.Name,
                static property => property.Value,
                StringComparer.Ordinal);
        if (!AcceptsPropertyCount(supplied.Count, constraints)
            || properties.Any(
                property =>
                    property.IsRequired
                    && !supplied.ContainsKey(property.Name)))
        {
            return false;
        }

        Dictionary<string, JsonContractProperty> declared = properties
            .ToDictionary(
                static property => property.Name,
                StringComparer.Ordinal);
        foreach ((string name, JsonElement propertyValue) in supplied)
        {
            if (declared.TryGetValue(
                    name,
                    out JsonContractProperty? property))
            {
                if (!AcceptsNode(
                        property.Value,
                        propertyValue,
                        referenceResolver,
                        activeReferences))
                {
                    return false;
                }
            }
            else if (additionalProperties is null
                || !AcceptsNode(
                    additionalProperties,
                    propertyValue,
                    referenceResolver,
                    activeReferences))
            {
                return false;
            }
        }

        return true;
    }

    private static bool AcceptsDictionary (
        JsonElement value,
        JsonContractNode? additionalProperties,
        JsonContractConstraints constraints,
        Func<string, JsonContractNode>? referenceResolver,
        List<ReferenceEvaluation> activeReferences)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        JsonProperty[] properties = value.EnumerateObject().ToArray();
        return AcceptsPropertyCount(properties.Length, constraints)
            && additionalProperties is not null
            && properties.All(
                property => AcceptsNode(
                    additionalProperties,
                    property.Value,
                    referenceResolver,
                    activeReferences));
    }

    private static bool AcceptsOneOf (
        JsonElement value,
        IReadOnlyList<JsonContractVariant> variants,
        Func<string, JsonContractNode>? referenceResolver,
        List<ReferenceEvaluation> activeReferences)
    {
        int matches = 0;
        foreach (JsonContractVariant variant in variants)
        {
            bool accepts = variant.Value is not null
                ? AcceptsNode(
                    variant.Value,
                    value,
                    referenceResolver,
                    activeReferences)
                : value.ValueKind == JsonValueKind.Object
                    && variant.RequiredProperties.All(
                        required => value.TryGetProperty(required, out _));
            if (accepts)
            {
                matches++;
            }
        }

        return matches == 1;
    }

    private static bool AcceptsReference (
        string? referenceId,
        JsonElement value,
        Func<string, JsonContractNode>? referenceResolver,
        List<ReferenceEvaluation> activeReferences)
    {
        if (referenceId is null || referenceResolver is null)
        {
            return false;
        }

        if (activeReferences.Any(
            active =>
                string.Equals(
                    active.ReferenceId,
                    referenceId,
                    StringComparison.Ordinal)
                && JsonElementUtility.CompareCanonical(
                    active.Value,
                    value) == 0))
        {
            return false;
        }

        activeReferences.Add(new ReferenceEvaluation(referenceId, value));
        try
        {
            return AcceptsNode(
                referenceResolver(referenceId),
                value,
                referenceResolver,
                activeReferences);
        }
        finally
        {
            activeReferences.RemoveAt(activeReferences.Count - 1);
        }
    }

    private static bool AcceptsConstraints (
        JsonElement value,
        JsonContractConstraints constraints)
    {
        if (value.ValueKind == JsonValueKind.Number)
        {
            if ((constraints.Minimum.HasValue
                    && JsonSemanticValueCanonicalizer.CompareNumbers(
                        value,
                        constraints.Minimum.Value) < 0)
                || (constraints.ExclusiveMinimum.HasValue
                    && JsonSemanticValueCanonicalizer.CompareNumbers(
                        value,
                        constraints.ExclusiveMinimum.Value) <= 0)
                || (constraints.Maximum.HasValue
                    && JsonSemanticValueCanonicalizer.CompareNumbers(
                        value,
                        constraints.Maximum.Value) > 0)
                || (constraints.ExclusiveMaximum.HasValue
                    && JsonSemanticValueCanonicalizer.CompareNumbers(
                        value,
                        constraints.ExclusiveMaximum.Value) >= 0))
            {
                return false;
            }
        }

        if (value.ValueKind == JsonValueKind.String)
        {
            string text = value.GetString()!;
            int length = GetUnicodeCodePointCount(text);
            if ((constraints.MinimumLength.HasValue
                    && length < constraints.MinimumLength.Value)
                || (constraints.MaximumLength.HasValue
                    && length > constraints.MaximumLength.Value)
                || (constraints.Pattern is not null
                    && !MatchesPattern(text, constraints.Pattern)))
            {
                return false;
            }
        }

        return true;
    }

    private static bool AcceptsPropertyCount (
        int count,
        JsonContractConstraints constraints)
    {
        return (!constraints.MinimumProperties.HasValue
                || count >= constraints.MinimumProperties.Value)
            && (!constraints.MaximumProperties.HasValue
                || count <= constraints.MaximumProperties.Value);
    }

    private static int GetUnicodeCodePointCount (string value)
    {
        int count = 0;
        for (int index = 0; index < value.Length; index++)
        {
            if (char.IsHighSurrogate(value[index])
                && index + 1 < value.Length
                && char.IsLowSurrogate(value[index + 1]))
            {
                index++;
            }

            count++;
        }

        return count;
    }

    private static bool MatchesPattern (
        string value,
        string pattern)
    {
        try
        {
            return new Regex(
                    pattern,
                    RegexOptions.ECMAScript,
                    TimeSpan.FromSeconds(1))
                .IsMatch(value);
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }

    private static bool MatchesScalar (
        JsonValueKind valueKind,
        JsonContractScalarKind? scalarKind)
    {
        return scalarKind switch
        {
            JsonContractScalarKind.Null => valueKind == JsonValueKind.Null,
            JsonContractScalarKind.Boolean =>
                valueKind is JsonValueKind.True or JsonValueKind.False,
            JsonContractScalarKind.Integer or JsonContractScalarKind.Number =>
                valueKind == JsonValueKind.Number,
            JsonContractScalarKind.String => valueKind == JsonValueKind.String,
            _ => false,
        };
    }

    private sealed class ReferenceEvaluation
    {
        internal ReferenceEvaluation (
            string referenceId,
            JsonElement value)
        {
            ReferenceId = referenceId;
            Value = value;
        }

        internal string ReferenceId { get; }

        internal JsonElement Value { get; }
    }
}
