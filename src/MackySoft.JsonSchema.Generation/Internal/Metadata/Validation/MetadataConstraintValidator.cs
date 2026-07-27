using System.Text.Json;
using MackySoft.JsonSchema.Generation.ContractModel;
using MackySoft.JsonSchema.Generation.Internal.Determinism;
using MackySoft.JsonSchema.Generation.Internal.Metadata.Contracts;
using MackySoft.JsonSchema.Generation.Internal.Metadata.Declarations;

namespace MackySoft.JsonSchema.Generation.Internal.Metadata.Validation;

internal static class MetadataConstraintValidator
{
    internal static void Validate (
        MetadataResolutionTarget target,
        MetadataDeclarationSet declarations,
        JsonContractConstraints constraints)
    {
        ValidateOrderedPair(
            constraints.MinimumLength,
            constraints.MaximumLength,
            declarations.LengthBoundSourceIds,
            target,
            "String length bounds");
        ValidateOrderedPair(
            constraints.MinimumItems,
            constraints.MaximumItems,
            declarations.ItemCountBoundSourceIds,
            target,
            "Array item-count bounds");
        ValidateOrderedPair(
            constraints.MinimumProperties,
            constraints.MaximumProperties,
            declarations.PropertyCountBoundSourceIds,
            target,
            "Object property-count bounds");
        ValidateNumericBounds(
            constraints,
            declarations.NumericBoundSourceIds,
            target);
    }

    private static void ValidateOrderedPair (
        int? minimum,
        int? maximum,
        IEnumerable<string> sourceIds,
        MetadataResolutionTarget target,
        string pairName)
    {
        if (minimum.HasValue
            && maximum.HasValue
            && minimum.Value > maximum.Value)
        {
            throw MetadataFailure.Invalid(
                target,
                sourceIds,
                $"{pairName} must be ordered from minimum to maximum.");
        }
    }

    private static void ValidateNumericBounds (
        JsonContractConstraints constraints,
        IEnumerable<string> sourceIds,
        MetadataResolutionTarget target)
    {
        NumericBound? lower = StrongestLowerBound(
            constraints.Minimum,
            constraints.ExclusiveMinimum);
        NumericBound? upper = StrongestUpperBound(
            constraints.Maximum,
            constraints.ExclusiveMaximum);
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
                sourceIds,
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
