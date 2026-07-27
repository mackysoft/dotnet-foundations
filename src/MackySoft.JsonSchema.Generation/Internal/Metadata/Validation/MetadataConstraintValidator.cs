using System.Text.Json;
using MackySoft.JsonSchema.Generation.Internal.Determinism;
using MackySoft.JsonSchema.Generation.Internal.Metadata.Contracts;
using MackySoft.JsonSchema.Generation.Metadata;

namespace MackySoft.JsonSchema.Generation.Internal.Metadata.Validation;

internal static class MetadataConstraintValidator
{
    private static readonly JsonContractMetadataKind[] StructuralMetadataKinds =
    {
        JsonContractMetadataKind.Const,
        JsonContractMetadataKind.EnumValue,
        JsonContractMetadataKind.Minimum,
        JsonContractMetadataKind.ExclusiveMinimum,
        JsonContractMetadataKind.Maximum,
        JsonContractMetadataKind.ExclusiveMaximum,
        JsonContractMetadataKind.MinimumLength,
        JsonContractMetadataKind.MaximumLength,
        JsonContractMetadataKind.MinimumItems,
        JsonContractMetadataKind.MaximumItems,
        JsonContractMetadataKind.MinimumProperties,
        JsonContractMetadataKind.MaximumProperties,
        JsonContractMetadataKind.Pattern,
        JsonContractMetadataKind.Format,
    };

    internal static void ValidateValueConstraints (
        MetadataResolutionTarget target,
        IReadOnlyList<ResolvedContractMetadata.MetadataProvenance> metadata,
        JsonElement? constant,
        IReadOnlyList<JsonElement> allowedValues,
        JsonElement? minimum,
        JsonElement? exclusiveMinimum,
        JsonElement? maximum,
        JsonElement? exclusiveMaximum,
        int? minimumLength,
        int? maximumLength,
        int? minimumItems,
        int? maximumItems,
        int? minimumProperties,
        int? maximumProperties)
    {
        ValidateOrderedPair(
            minimumLength,
            maximumLength,
            JsonContractMetadataKind.MinimumLength,
            JsonContractMetadataKind.MaximumLength,
            metadata,
            target,
            "String length bounds");
        ValidateOrderedPair(
            minimumItems,
            maximumItems,
            JsonContractMetadataKind.MinimumItems,
            JsonContractMetadataKind.MaximumItems,
            metadata,
            target,
            "Array item-count bounds");
        ValidateOrderedPair(
            minimumProperties,
            maximumProperties,
            JsonContractMetadataKind.MinimumProperties,
            JsonContractMetadataKind.MaximumProperties,
            metadata,
            target,
            "Object property-count bounds");
        ValidateNumericBounds(
            minimum,
            exclusiveMinimum,
            maximum,
            exclusiveMaximum,
            metadata,
            target);
        ValidateConstantAndAllowedValues(
            constant,
            allowedValues,
            metadata,
            target);
    }

    internal static void ValidateArbitraryContract (
        bool isArbitrary,
        IReadOnlyList<ResolvedContractMetadata.MetadataProvenance> metadata,
        IReadOnlyList<ResolvedContractMetadata.OneOfBranchProvenance> oneOfBranches,
        IReadOnlyList<ResolvedContractMetadata.DiscriminatorProvenance> discriminators,
        MetadataResolutionTarget target)
    {
        if (!isArbitrary)
        {
            return;
        }

        ResolvedContractMetadata.MetadataProvenance[] structuralMetadata =
            metadata
                .Where(
                    declaration => StructuralMetadataKinds.Contains(
                        declaration.Metadata.Kind))
                .ToArray();
        if (structuralMetadata.Length == 0
            && oneOfBranches.Count == 0
            && discriminators.Count == 0)
        {
            return;
        }

        IEnumerable<string> sources = MetadataValueResolver.SourceIds(
                metadata,
                JsonContractMetadataKind.Arbitrary)
            .Concat(
                structuralMetadata.Select(
                    static declaration => declaration.SourceId))
            .Concat(
                oneOfBranches.Select(
                    static declaration => declaration.SourceId))
            .Concat(
                discriminators.Select(
                    static declaration => declaration.SourceId));
        throw MetadataFailure.Invalid(
            target,
            JsonContractMetadataKind.Arbitrary,
            sources,
            "Arbitrary JSON metadata cannot coexist with structural value metadata.");
    }

    private static void ValidateOrderedPair (
        int? minimum,
        int? maximum,
        JsonContractMetadataKind minimumKind,
        JsonContractMetadataKind maximumKind,
        IReadOnlyList<ResolvedContractMetadata.MetadataProvenance> declarations,
        MetadataResolutionTarget target,
        string pairName)
    {
        if (minimum.HasValue
            && maximum.HasValue
            && minimum.Value > maximum.Value)
        {
            throw MetadataFailure.Invalid(
                target,
                metadataKind: null,
                MetadataValueResolver.SourceIds(
                    declarations,
                    minimumKind,
                    maximumKind),
                $"{pairName} must be ordered from minimum to maximum.");
        }
    }

    private static void ValidateNumericBounds (
        JsonElement? minimum,
        JsonElement? exclusiveMinimum,
        JsonElement? maximum,
        JsonElement? exclusiveMaximum,
        IReadOnlyList<ResolvedContractMetadata.MetadataProvenance> declarations,
        MetadataResolutionTarget target)
    {
        NumericBound? lower = StrongestLowerBound(minimum, exclusiveMinimum);
        NumericBound? upper = StrongestUpperBound(maximum, exclusiveMaximum);
        if (!lower.HasValue || !upper.HasValue)
        {
            return;
        }

        int comparison = JsonSemanticValueCanonicalizer.CompareNumbers(
            lower.Value.Value,
            upper.Value.Value);
        if (comparison > 0
            || (comparison == 0
                && (lower.Value.IsExclusive || upper.Value.IsExclusive)))
        {
            throw MetadataFailure.Invalid(
                target,
                metadataKind: null,
                MetadataValueResolver.SourceIds(
                    declarations,
                    JsonContractMetadataKind.Minimum,
                    JsonContractMetadataKind.ExclusiveMinimum,
                    JsonContractMetadataKind.Maximum,
                    JsonContractMetadataKind.ExclusiveMaximum),
                "Numeric bounds do not describe an ordered, non-empty interval.");
        }
    }

    private static NumericBound? StrongestLowerBound (
        JsonElement? inclusive,
        JsonElement? exclusive)
    {
        if (!inclusive.HasValue)
        {
            return exclusive.HasValue
                ? new NumericBound(exclusive.Value, isExclusive: true)
                : null;
        }

        if (!exclusive.HasValue)
        {
            return new NumericBound(inclusive.Value, isExclusive: false);
        }

        return JsonSemanticValueCanonicalizer.CompareNumbers(
                inclusive.Value,
                exclusive.Value) > 0
            ? new NumericBound(inclusive.Value, isExclusive: false)
            : new NumericBound(exclusive.Value, isExclusive: true);
    }

    private static NumericBound? StrongestUpperBound (
        JsonElement? inclusive,
        JsonElement? exclusive)
    {
        if (!inclusive.HasValue)
        {
            return exclusive.HasValue
                ? new NumericBound(exclusive.Value, isExclusive: true)
                : null;
        }

        if (!exclusive.HasValue)
        {
            return new NumericBound(inclusive.Value, isExclusive: false);
        }

        return JsonSemanticValueCanonicalizer.CompareNumbers(
                inclusive.Value,
                exclusive.Value) < 0
            ? new NumericBound(inclusive.Value, isExclusive: false)
            : new NumericBound(exclusive.Value, isExclusive: true);
    }

    private static void ValidateConstantAndAllowedValues (
        JsonElement? constant,
        IReadOnlyList<JsonElement> allowedValues,
        IReadOnlyList<ResolvedContractMetadata.MetadataProvenance> declarations,
        MetadataResolutionTarget target)
    {
        if (constant.HasValue
            && allowedValues.Count != 0
            && !allowedValues.Any(
                value => JsonElementUtility.CompareCanonical(
                    constant.Value,
                    value) == 0))
        {
            throw MetadataFailure.Invalid(
                target,
                JsonContractMetadataKind.Const,
                MetadataValueResolver.SourceIds(
                    declarations,
                    JsonContractMetadataKind.Const,
                    JsonContractMetadataKind.EnumValue),
                "The declared constant is not contained in the finite allowed-value set.");
        }

    }

    private readonly struct NumericBound
    {
        internal NumericBound (
            JsonElement value,
            bool isExclusive)
        {
            Value = value;
            IsExclusive = isExclusive;
        }

        internal JsonElement Value { get; }

        internal bool IsExclusive { get; }
    }
}
