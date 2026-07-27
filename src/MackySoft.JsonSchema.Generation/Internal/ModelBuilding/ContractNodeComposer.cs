using System.Text.Json;
using MackySoft.JsonSchema.Generation.ContractModel;
using MackySoft.JsonSchema.Generation.Internal.Metadata.Contracts;
using MackySoft.JsonSchema.Generation.Internal.ModelBuilding.Shapes;
using MackySoft.JsonSchema.Generation.Internal.ModelBuilding.Validation;

namespace MackySoft.JsonSchema.Generation.Internal.ModelBuilding;

/// <summary>
/// Validates declarative metadata against serializer-derived structure and creates
/// the immutable model node. It does not inspect serializer metadata or CLR shape.
/// </summary>
internal static class ContractNodeComposer
{
    internal static JsonContractNode Compose (
        string contractId,
        Type targetType,
        string? jsonPropertyName,
        ContractNodeShape shape,
        JsonContractValueValidator valueValidator,
        ResolvedContractMetadata? metadata,
        bool isNullable,
        JsonContractSource source)
    {
        if (shape is null)
        {
            throw new ArgumentNullException(nameof(shape));
        }

        if (valueValidator is null)
        {
            throw new ArgumentNullException(nameof(valueValidator));
        }

        JsonContractAnnotations annotations = ContractAnnotationComposer.Compose(
            contractId,
            targetType,
            jsonPropertyName,
            shape.Annotations,
            metadata);
        JsonContractConstraints constraints = ContractConstraintComposer.Compose(
            contractId,
            targetType,
            jsonPropertyName,
            shape,
            metadata);

        if (metadata?.Constant is JsonElement constant)
        {
            if (constant.ValueKind == JsonValueKind.Null && !isNullable)
            {
                throw ContractMetadataFailure.Invalid(
                    contractId,
                    targetType,
                    jsonPropertyName,
                    "A non-nullable contract cannot declare the null constant.");
            }

            valueValidator.RegisterAgainstShape(
                contractId,
                targetType,
                jsonPropertyName,
                constant,
                shape,
                constraints,
                isNullable);
            return CreateNode(
                JsonContractNodeKind.Const,
                isNullable || constant.ValueKind == JsonValueKind.Null,
                JsonContractValueValidator.GetScalarKind(constant),
                annotations,
                constraints,
                constant,
                Array.Empty<JsonElement>(),
                referenceId: null,
                items: null,
                additionalProperties: null,
                Array.Empty<JsonContractProperty>(),
                Array.Empty<JsonContractVariant>(),
                discriminator: null,
                source);
        }

        return CreateNode(
            shape.Kind,
            isNullable || shape.Kind == JsonContractNodeKind.Arbitrary,
            shape.ScalarKind,
            annotations,
            constraints,
            shape.Constant,
            shape.AllowedValues,
            shape.ReferenceId,
            shape.Items,
            shape.AdditionalProperties,
            shape.Properties,
            shape.Variants,
            shape.Discriminator,
            source);
    }

    private static JsonContractNode CreateNode (
        JsonContractNodeKind kind,
        bool isNullable,
        JsonContractScalarKind? scalarKind,
        JsonContractAnnotations annotations,
        JsonContractConstraints constraints,
        JsonElement? constant,
        IEnumerable<JsonElement> allowedValues,
        string? referenceId,
        JsonContractNode? items,
        JsonContractNode? additionalProperties,
        IEnumerable<JsonContractProperty> properties,
        IEnumerable<JsonContractVariant> variants,
        JsonContractDiscriminator? discriminator,
        JsonContractSource source)
    {
        return new JsonContractNode(
            kind,
            isNullable,
            scalarKind,
            annotations,
            constraints,
            constant,
            allowedValues,
            referenceId,
            items,
            additionalProperties,
            properties,
            variants,
            discriminator,
            source);
    }
}
