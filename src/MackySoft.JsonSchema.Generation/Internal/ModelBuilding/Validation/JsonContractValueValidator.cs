using System.Text.Json;
using MackySoft.JsonSchema.Generation.ContractModel;
using MackySoft.JsonSchema.Generation.Internal.ModelBuilding.Shapes;

namespace MackySoft.JsonSchema.Generation.Internal.ModelBuilding.Validation;

/// <summary>
/// Normalizes finite JSON value declarations and checks them against a
/// serializer-derived model shape.
/// </summary>
internal sealed class JsonContractValueValidator
{
    private readonly List<PendingValidation> pendingValidations = new();

    internal void RegisterAgainstShape (
        string contractId,
        Type targetType,
        string? jsonPropertyName,
        JsonElement value,
        ContractNodeShape shape,
        JsonContractConstraints constraints,
        bool isNullable)
    {
        if (value.ValueKind == JsonValueKind.Undefined)
        {
            throw ContractMetadataFailure.Invalid(
                contractId,
                targetType,
                jsonPropertyName,
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
                "A non-nullable contract cannot declare the null value.");
        }

        pendingValidations.Add(
            new PendingValidation(
                contractId,
                targetType,
                jsonPropertyName,
                value,
                shape,
                constraints));
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
            JsonContractConstraints constraints)
        {
            ContractId = contractId;
            TargetType = targetType;
            JsonPropertyName = jsonPropertyName;
            Value = value.Clone();
            Shape = shape;
            Constraints = constraints;
        }

        internal string ContractId { get; }

        internal Type TargetType { get; }

        internal string? JsonPropertyName { get; }

        internal JsonElement Value { get; }

        internal ContractNodeShape Shape { get; }

        internal JsonContractConstraints Constraints { get; }

    }
}
