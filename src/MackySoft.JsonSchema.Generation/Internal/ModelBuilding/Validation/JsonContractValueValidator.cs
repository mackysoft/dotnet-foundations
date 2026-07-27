using System.Text.Json;
using MackySoft.JsonSchema.Generation.ContractModel;
using MackySoft.JsonSchema.Generation.Internal.Determinism;
using MackySoft.JsonSchema.Generation.Internal.ModelBuilding.Shapes;
using MackySoft.JsonSchema.Generation.Metadata;

namespace MackySoft.JsonSchema.Generation.Internal.ModelBuilding.Validation;

/// <summary>
/// Normalizes finite JSON value declarations and checks them against a
/// serializer-derived model shape.
/// </summary>
internal sealed class JsonContractValueValidator
{
    private readonly List<PendingValidation> pendingValidations = new();

    internal JsonElement[] NormalizeAllowedValues (
        string contractId,
        Type targetType,
        string? jsonPropertyName,
        IReadOnlyList<JsonElement> allowedValues,
        ContractNodeShape shape,
        JsonContractConstraints constraints,
        bool isNullable)
    {
        JsonElement[] values = allowedValues.ToArray();
        foreach (JsonElement value in values)
        {
            RegisterAgainstShape(
                contractId,
                targetType,
                jsonPropertyName,
                value,
                shape,
                constraints,
                isNullable,
                JsonContractMetadataKind.EnumValue);
        }

        Array.Sort(values, JsonElementUtility.CompareCanonical);
        var unique = new List<JsonElement>(values.Length);
        foreach (JsonElement value in values)
        {
            if (unique.Count == 0
                || JsonElementUtility.CompareCanonical(
                    unique[unique.Count - 1],
                    value) != 0)
            {
                unique.Add(value);
            }
        }

        return unique.ToArray();
    }

    internal void RegisterAgainstShape (
        string contractId,
        Type targetType,
        string? jsonPropertyName,
        JsonElement value,
        ContractNodeShape shape,
        JsonContractConstraints constraints,
        bool isNullable,
        JsonContractMetadataKind metadataKind)
    {
        if (value.ValueKind == JsonValueKind.Undefined)
        {
            throw ContractMetadataFailure.Invalid(
                contractId,
                targetType,
                jsonPropertyName,
                metadataKind,
                "A contract value cannot be undefined.");
        }

        if (value.ValueKind == JsonValueKind.Null)
        {
            if (isNullable)
            {
                return;
            }

            throw ContractMetadataFailure.Invalid(
                contractId,
                targetType,
                jsonPropertyName,
                metadataKind,
                "A non-nullable contract cannot declare the null value.");
        }

        pendingValidations.Add(
            new PendingValidation(
                contractId,
                targetType,
                jsonPropertyName,
                value,
                shape,
                constraints,
                metadataKind));
    }

    internal void ValidateAll (
        Func<string, JsonContractNode> referenceResolver)
    {
        if (referenceResolver is null)
        {
            throw new ArgumentNullException(nameof(referenceResolver));
        }

        foreach (PendingValidation pending in pendingValidations)
        {
            if (!ContractValueAcceptanceEvaluator.Accepts(
                    pending.Shape,
                    pending.Constraints,
                    pending.Value,
                    referenceResolver))
            {
                throw ContractMetadataFailure.Invalid(
                    pending.ContractId,
                    pending.TargetType,
                    pending.JsonPropertyName,
                    pending.MetadataKind,
                    "A declared JSON value is incompatible with the serializer-derived contract shape.");
            }
        }
    }

    internal static bool Accepts (
        JsonContractNode node,
        JsonElement value)
    {
        return ContractValueAcceptanceEvaluator.Accepts(node, value);
    }

    internal static JsonContractScalarKind? GetCommonScalarKind (
        IReadOnlyList<JsonElement> values)
    {
        JsonContractScalarKind? result = null;
        bool hasNonNullValue = false;
        foreach (JsonElement value in values)
        {
            if (value.ValueKind == JsonValueKind.Null)
            {
                continue;
            }

            hasNonNullValue = true;
            JsonContractScalarKind? current = GetScalarKind(value);
            if (!current.HasValue)
            {
                return null;
            }

            if (result.HasValue && result.Value != current.Value)
            {
                if ((result.Value is JsonContractScalarKind.Integer
                        or JsonContractScalarKind.Number)
                    && (current.Value is JsonContractScalarKind.Integer
                        or JsonContractScalarKind.Number))
                {
                    result = JsonContractScalarKind.Number;
                    continue;
                }

                return null;
            }

            result = current;
        }

        return hasNonNullValue
            ? result
            : JsonContractScalarKind.Null;
    }

    internal static JsonContractScalarKind? GetScalarKind (JsonElement value)
    {
        return ContractValueAcceptanceEvaluator.GetScalarKind(value);
    }

    private sealed class PendingValidation
    {
        internal PendingValidation (
            string contractId,
            Type targetType,
            string? jsonPropertyName,
            JsonElement value,
            ContractNodeShape shape,
            JsonContractConstraints constraints,
            JsonContractMetadataKind metadataKind)
        {
            ContractId = contractId;
            TargetType = targetType;
            JsonPropertyName = jsonPropertyName;
            Value = value.Clone();
            Shape = shape;
            Constraints = constraints;
            MetadataKind = metadataKind;
        }

        internal string ContractId { get; }

        internal Type TargetType { get; }

        internal string? JsonPropertyName { get; }

        internal JsonElement Value { get; }

        internal ContractNodeShape Shape { get; }

        internal JsonContractConstraints Constraints { get; }

        internal JsonContractMetadataKind MetadataKind { get; }
    }
}
