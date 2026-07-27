using System.Text.Json;
using MackySoft.JsonSchema.Generation.ContractModel;
using MackySoft.JsonSchema.Generation.Internal.Determinism;
using MackySoft.JsonSchema.Generation.Internal.Metadata.Contracts;
using MackySoft.JsonSchema.Generation.Internal.ModelBuilding.Shapes;
using MackySoft.JsonSchema.Generation.Internal.ModelBuilding.Validation;
using MackySoft.JsonSchema.Generation.Metadata;

namespace MackySoft.JsonSchema.Generation.Internal.ModelBuilding.Variants;

/// <summary>
/// Validates property-set oneOf declarations and creates deterministic model
/// variants from an already completed object shape.
/// </summary>
internal static class PropertySetVariantComposer
{
    public static IReadOnlyList<JsonContractVariant> Compose (
        string contractId,
        Type targetType,
        string? jsonPropertyName,
        ContractNodeShape shape,
        ResolvedContractMetadata metadata)
    {
        var propertyNames = new HashSet<string>(
            shape.Properties.Select(static property => property.Name),
            StringComparer.Ordinal);
        if (metadata.DiscriminatorPropertyName is not null
            && !propertyNames.Contains(metadata.DiscriminatorPropertyName))
        {
            throw ContractMetadataFailure.Invalid(
                contractId,
                targetType,
                jsonPropertyName,
                JsonContractMetadataKind.Discriminator,
                $"Discriminator property '{metadata.DiscriminatorPropertyName}' is not present in the object contract.");
        }

        JsonContractProperty? discriminatorProperty =
            metadata.DiscriminatorPropertyName is null
                ? null
                : shape.Properties.Single(
                    property => string.Equals(
                        property.Name,
                        metadata.DiscriminatorPropertyName,
                        StringComparison.Ordinal));
        if (discriminatorProperty is not null
            && !discriminatorProperty.IsRequired)
        {
            throw ContractMetadataFailure.Invalid(
                contractId,
                targetType,
                jsonPropertyName,
                JsonContractMetadataKind.Discriminator,
                $"Discriminator property '{discriminatorProperty.Name}' must be required by the object contract.");
        }

        var names = new HashSet<string>(StringComparer.Ordinal);
        var discriminatorValues = new List<JsonElement>();
        var variants = new List<JsonContractVariant>(metadata.OneOfBranches.Count);
        foreach (ResolvedOneOfBranch branch in metadata.OneOfBranches)
        {
            if (!names.Add(branch.Name))
            {
                throw ContractMetadataFailure.Invalid(
                    contractId,
                    targetType,
                    jsonPropertyName,
                    JsonContractMetadataKind.OneOfBranch,
                    $"oneOf branch name '{branch.Name}' is duplicated.");
            }

            if (branch.RequiredProperties.Count == 0
                && branch.DiscriminatorValue is null)
            {
                throw ContractMetadataFailure.Invalid(
                    contractId,
                    targetType,
                    jsonPropertyName,
                    JsonContractMetadataKind.OneOfBranch,
                    $"oneOf branch '{branch.Name}' does not declare a selection condition.");
            }

            string[] requiredProperties = branch.RequiredProperties
                .Distinct(StringComparer.Ordinal)
                .OrderBy(
                    static value => value,
                    UnicodeCodePointComparer.Instance)
                .ToArray();
            string? missingProperty = requiredProperties.FirstOrDefault(
                property => !propertyNames.Contains(property));
            if (missingProperty is not null)
            {
                throw ContractMetadataFailure.Invalid(
                    contractId,
                    targetType,
                    jsonPropertyName,
                    JsonContractMetadataKind.OneOfBranch,
                    $"oneOf branch '{branch.Name}' requires unknown JSON property '{missingProperty}'.");
            }

            ValidateDiscriminator(
                contractId,
                targetType,
                jsonPropertyName,
                metadata,
                discriminatorProperty,
                branch,
                discriminatorValues);

            variants.Add(
                new JsonContractVariant(
                    branch.Name,
                    value: null,
                    requiredProperties,
                    branch.DiscriminatorValue,
                    branch.Annotations));
        }

        variants.Sort(
            static (left, right) =>
                UnicodeCodePointComparer.Instance.Compare(left.Name, right.Name));
        return variants.AsReadOnly();
    }

    private static void ValidateDiscriminator (
        string contractId,
        Type targetType,
        string? jsonPropertyName,
        ResolvedContractMetadata metadata,
        JsonContractProperty? discriminatorProperty,
        ResolvedOneOfBranch branch,
        ICollection<JsonElement> discriminatorValues)
    {
        if (metadata.DiscriminatorPropertyName is null)
        {
            if (branch.DiscriminatorValue.HasValue)
            {
                throw ContractMetadataFailure.Invalid(
                    contractId,
                    targetType,
                    jsonPropertyName,
                    JsonContractMetadataKind.Discriminator,
                    $"oneOf branch '{branch.Name}' declares a value without a discriminator.");
            }

            return;
        }

        if (!branch.DiscriminatorValue.HasValue)
        {
            throw ContractMetadataFailure.Invalid(
                contractId,
                targetType,
                jsonPropertyName,
                JsonContractMetadataKind.Discriminator,
                $"oneOf branch '{branch.Name}' does not declare a discriminator value.");
        }

        if (branch.DiscriminatorValue.Value.ValueKind == JsonValueKind.Null
            || discriminatorProperty is null
            || !JsonContractValueValidator.Accepts(
                discriminatorProperty.Value,
                branch.DiscriminatorValue.Value))
        {
            throw ContractMetadataFailure.Invalid(
                contractId,
                targetType,
                jsonPropertyName,
                JsonContractMetadataKind.Discriminator,
                $"oneOf branch '{branch.Name}' declares a discriminator value incompatible with property '{metadata.DiscriminatorPropertyName}'.");
        }

        if (discriminatorValues.Any(
            value => JsonElementUtility.CompareCanonical(
                value,
                branch.DiscriminatorValue.Value) == 0))
        {
            throw ContractMetadataFailure.Invalid(
                contractId,
                targetType,
                jsonPropertyName,
                JsonContractMetadataKind.Discriminator,
                $"oneOf branch '{branch.Name}' duplicates a discriminator value.");
        }

        discriminatorValues.Add(branch.DiscriminatorValue.Value);
    }
}
