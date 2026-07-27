using System.Text.Json;
using MackySoft.JsonSchema.Generation.ContractModel;
using MackySoft.JsonSchema.Generation.Internal.Determinism;
using MackySoft.JsonSchema.Generation.Internal.Metadata.Contracts;
using MackySoft.JsonSchema.Generation.Internal.ModelBuilding.Shapes;
using MackySoft.JsonSchema.Generation.Internal.ModelBuilding.Validation;
using MackySoft.JsonSchema.Generation.Internal.ModelBuilding.Variants;
using MackySoft.JsonSchema.Generation.Metadata;

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

        ValidateMetadataCompatibility(
            contractId,
            targetType,
            jsonPropertyName,
            metadata);

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

        if (metadata?.IsArbitrary == true)
        {
            ArbitraryContractValidator.Validate(
                contractId,
                targetType,
                jsonPropertyName,
                metadata);
            return CreateNode(
                JsonContractNodeKind.Arbitrary,
                isNullable: true,
                scalarKind: null,
                annotations,
                constraints,
                constant: null,
                Array.Empty<JsonElement>(),
                referenceId: null,
                items: null,
                additionalProperties: null,
                Array.Empty<JsonContractProperty>(),
                Array.Empty<JsonContractVariant>(),
                discriminator: null,
                source);
        }

        if (metadata?.Constant is JsonElement constant)
        {
            if (constant.ValueKind == JsonValueKind.Null && !isNullable)
            {
                throw ContractMetadataFailure.Invalid(
                    contractId,
                    targetType,
                    jsonPropertyName,
                    JsonContractMetadataKind.Const,
                    "A non-nullable contract cannot declare the null constant.");
            }

            if (metadata.AllowedValues.Count != 0
                && !metadata.AllowedValues.Any(
                    value => JsonElementUtility.CompareCanonical(value, constant) == 0))
            {
                throw ContractMetadataFailure.Invalid(
                    contractId,
                    targetType,
                    jsonPropertyName,
                    JsonContractMetadataKind.Const,
                    "The declared constant is not contained in the declared enum values.");
            }

            valueValidator.RegisterAgainstShape(
                contractId,
                targetType,
                jsonPropertyName,
                constant,
                shape,
                constraints,
                isNullable,
                JsonContractMetadataKind.Const);
            return CreateNode(
                JsonContractNodeKind.Const,
                isNullable || constant.ValueKind == JsonValueKind.Null,
                JsonContractValueValidator.GetScalarKind(constant),
                annotations,
                constraints,
                constant,
                metadata.AllowedValues,
                referenceId: null,
                items: null,
                additionalProperties: null,
                Array.Empty<JsonContractProperty>(),
                Array.Empty<JsonContractVariant>(),
                discriminator: null,
                source);
        }

        if (metadata is not null && metadata.AllowedValues.Count != 0)
        {
            JsonElement[] values = valueValidator.NormalizeAllowedValues(
                contractId,
                targetType,
                jsonPropertyName,
                metadata.AllowedValues,
                shape,
                constraints,
                isNullable);
            JsonContractScalarKind? scalarKind = JsonContractValueValidator.GetCommonScalarKind(values);
            if (!isNullable
                && values.Any(
                    static value => value.ValueKind == JsonValueKind.Null))
            {
                throw ContractMetadataFailure.Invalid(
                    contractId,
                    targetType,
                    jsonPropertyName,
                    JsonContractMetadataKind.EnumValue,
                    "A non-nullable contract cannot include null in its enum values.");
            }

            return CreateNode(
                JsonContractNodeKind.Enum,
                isNullable || values.Any(
                    static value => value.ValueKind == JsonValueKind.Null),
                scalarKind,
                annotations,
                constraints,
                constant: null,
                values,
                referenceId: null,
                items: null,
                additionalProperties: null,
                Array.Empty<JsonContractProperty>(),
                Array.Empty<JsonContractVariant>(),
                discriminator: null,
                source);
        }

        IReadOnlyList<JsonContractVariant> variants = shape.Variants;
        JsonContractDiscriminator? discriminator = shape.Discriminator;
        if (metadata is not null && metadata.OneOfBranches.Count != 0)
        {
            if (shape.Kind != JsonContractNodeKind.Object
                || shape.Variants.Count != 0)
            {
                throw ContractMetadataFailure.Invalid(
                    contractId,
                    targetType,
                    jsonPropertyName,
                    JsonContractMetadataKind.OneOfBranch,
                    "Property-set oneOf metadata can only decorate a non-polymorphic object contract.");
            }

            variants = PropertySetVariantComposer.Compose(
                contractId,
                targetType,
                jsonPropertyName,
                shape,
                metadata);
            discriminator = metadata.DiscriminatorPropertyName is null
                ? null
                : new JsonContractDiscriminator(metadata.DiscriminatorPropertyName);
        }
        else if (metadata?.DiscriminatorPropertyName is not null)
        {
            throw ContractMetadataFailure.Invalid(
                contractId,
                targetType,
                jsonPropertyName,
                JsonContractMetadataKind.Discriminator,
                "A discriminator requires at least one oneOf branch.");
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
            variants,
            discriminator,
            source);
    }

    internal static void ValidateMetadataCompatibility (
        string contractId,
        Type targetType,
        string? jsonPropertyName,
        ResolvedContractMetadata? metadata)
    {
        if (metadata is null
            || (!metadata.Constant.HasValue
                && metadata.AllowedValues.Count == 0)
            || (metadata.OneOfBranches.Count == 0
                && metadata.DiscriminatorPropertyName is null))
        {
            return;
        }

        IEnumerable<string> finiteValueSources = metadata
            .MetadataDeclarations
            .Where(
                declaration =>
                    declaration.Metadata.Kind
                        is JsonContractMetadataKind.Const
                        or JsonContractMetadataKind.EnumValue)
            .Select(static declaration => declaration.SourceId);
        IEnumerable<string> branchSources = metadata
            .OneOfBranchDeclarations
            .Select(static declaration => declaration.SourceId)
            .Concat(
                metadata.DiscriminatorDeclarations.Select(
                    static declaration => declaration.SourceId));
        throw ContractMetadataFailure.Conflicting(
            contractId,
            targetType,
            jsonPropertyName,
            metadataKind: null,
            finiteValueSources.Concat(branchSources),
            "Finite JSON value metadata cannot be combined with oneOf or discriminator metadata.");
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
