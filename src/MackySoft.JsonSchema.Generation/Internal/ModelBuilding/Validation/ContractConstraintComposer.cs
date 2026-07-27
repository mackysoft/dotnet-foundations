using System.Text.Json;
using MackySoft.JsonSchema.Generation.ContractModel;
using MackySoft.JsonSchema.Generation.Internal.Determinism;
using MackySoft.JsonSchema.Generation.Internal.Metadata.Contracts;
using MackySoft.JsonSchema.Generation.Internal.ModelBuilding.Shapes;

namespace MackySoft.JsonSchema.Generation.Internal.ModelBuilding.Validation;

/// <summary>
/// Merges declared constraints with serializer-derived facts and validates that
/// every constraint applies to the completed JSON value shape.
/// </summary>
internal static class ContractConstraintComposer
{
    public static JsonContractConstraints Compose (
        string contractId,
        Type targetType,
        string? jsonPropertyName,
        ContractNodeShape shape,
        ResolvedContractMetadata? metadata)
    {
        JsonContractConstraints? declared = metadata?.Constraints;
        string? format = declared?.Format ?? shape.Format;
        if (shape.Format is not null
            && declared?.Format is not null
            && !string.Equals(shape.Format, declared.Format, StringComparison.Ordinal))
        {
            throw ContractMetadataFailure.Invalid(
                contractId,
                targetType,
                jsonPropertyName,
                $"Declared format '{declared.Format}' conflicts with serializer-derived format '{shape.Format}'.");
        }

        NumericBounds lowerBounds = ResolveLowerBounds(
            contractId,
            targetType,
            jsonPropertyName,
            shape.Minimum,
            shape.ExclusiveMinimum,
            declared?.Minimum,
            declared?.ExclusiveMinimum);
        NumericBounds upperBounds = ResolveUpperBounds(
            contractId,
            targetType,
            jsonPropertyName,
            shape.Maximum,
            shape.ExclusiveMaximum,
            declared?.Maximum,
            declared?.ExclusiveMaximum);
        int? minimumLength = ResolveMinimumCount(
            contractId,
            targetType,
            jsonPropertyName,
            shape.MinimumLength,
            declared?.MinimumLength,
            "length");
        int? maximumLength = ResolveMaximumCount(
            contractId,
            targetType,
            jsonPropertyName,
            shape.MaximumLength,
            declared?.MaximumLength,
            "length");
        int? minimumItems = ResolveMinimumCount(
            contractId,
            targetType,
            jsonPropertyName,
            shape.MinimumItems,
            declared?.MinimumItems,
            "item count");
        int? maximumItems = ResolveMaximumCount(
            contractId,
            targetType,
            jsonPropertyName,
            shape.MaximumItems,
            declared?.MaximumItems,
            "item count");
        int? minimumProperties = ResolveMinimumCount(
            contractId,
            targetType,
            jsonPropertyName,
            shape.MinimumProperties,
            declared?.MinimumProperties,
            "property count");
        int? maximumProperties = ResolveMaximumCount(
            contractId,
            targetType,
            jsonPropertyName,
            shape.MaximumProperties,
            declared?.MaximumProperties,
            "property count");
        string? pattern = ResolvePattern(
            contractId,
            targetType,
            jsonPropertyName,
            shape.Pattern,
            declared?.Pattern);
        var result = new JsonContractConstraints(
            lowerBounds.Inclusive,
            lowerBounds.Exclusive,
            upperBounds.Inclusive,
            upperBounds.Exclusive,
            minimumLength,
            maximumLength,
            minimumItems,
            maximumItems,
            minimumProperties,
            maximumProperties,
            pattern,
            format);

        ValidateKinds(
            contractId,
            targetType,
            jsonPropertyName,
            shape.Kind,
            shape.ScalarKind,
            result);
        ValidateRanges(
            contractId,
            targetType,
            jsonPropertyName,
            result);
        return result;
    }

    private static int? ResolveMinimumCount (
        string contractId,
        Type targetType,
        string? jsonPropertyName,
        int? structuralMinimum,
        int? declaredMinimum,
        string valueKind)
    {
        if (structuralMinimum.HasValue
            && declaredMinimum.HasValue
            && declaredMinimum.Value < structuralMinimum.Value)
        {
            throw ContractMetadataFailure.Invalid(
                contractId,
                targetType,
                jsonPropertyName,
                $"The declared minimum {valueKind} extends below the structural contract.");
        }

        return declaredMinimum ?? structuralMinimum;
    }

    private static int? ResolveMaximumCount (
        string contractId,
        Type targetType,
        string? jsonPropertyName,
        int? structuralMaximum,
        int? declaredMaximum,
        string valueKind)
    {
        if (structuralMaximum.HasValue
            && declaredMaximum.HasValue
            && declaredMaximum.Value > structuralMaximum.Value)
        {
            throw ContractMetadataFailure.Invalid(
                contractId,
                targetType,
                jsonPropertyName,
                $"The declared maximum {valueKind} extends above the structural contract.");
        }

        return declaredMaximum ?? structuralMaximum;
    }

    private static string? ResolvePattern (
        string contractId,
        Type targetType,
        string? jsonPropertyName,
        string? serializerPattern,
        string? declaredPattern)
    {
        if (serializerPattern is not null
            && declaredPattern is not null
            && !string.Equals(
                serializerPattern,
                declaredPattern,
                StringComparison.Ordinal))
        {
            throw ContractMetadataFailure.Invalid(
                contractId,
                targetType,
                jsonPropertyName,
                "The declared pattern cannot be combined with the serializer-derived pattern.");
        }

        return declaredPattern ?? serializerPattern;
    }

    private static NumericBounds ResolveLowerBounds (
        string contractId,
        Type targetType,
        string? jsonPropertyName,
        JsonElement? structuralMinimum,
        JsonElement? structuralExclusiveMinimum,
        JsonElement? declaredMinimum,
        JsonElement? declaredExclusiveMinimum)
    {
        NumericBound? structural = StrongestLowerBound(
            structuralMinimum,
            structuralExclusiveMinimum);
        ValidateLowerBound(
            contractId,
            targetType,
            jsonPropertyName,
            structural,
            declaredMinimum,
            isExclusive: false);
        ValidateLowerBound(
            contractId,
            targetType,
            jsonPropertyName,
            structural,
            declaredExclusiveMinimum,
            isExclusive: true);
        return new NumericBounds(
            declaredMinimum ?? structuralMinimum,
            declaredExclusiveMinimum ?? structuralExclusiveMinimum);
    }

    private static NumericBounds ResolveUpperBounds (
        string contractId,
        Type targetType,
        string? jsonPropertyName,
        JsonElement? structuralMaximum,
        JsonElement? structuralExclusiveMaximum,
        JsonElement? declaredMaximum,
        JsonElement? declaredExclusiveMaximum)
    {
        NumericBound? structural = StrongestUpperBound(
            structuralMaximum,
            structuralExclusiveMaximum);
        ValidateUpperBound(
            contractId,
            targetType,
            jsonPropertyName,
            structural,
            declaredMaximum,
            isExclusive: false);
        ValidateUpperBound(
            contractId,
            targetType,
            jsonPropertyName,
            structural,
            declaredExclusiveMaximum,
            isExclusive: true);
        return new NumericBounds(
            declaredMaximum ?? structuralMaximum,
            declaredExclusiveMaximum ?? structuralExclusiveMaximum);
    }

    private static void ValidateLowerBound (
        string contractId,
        Type targetType,
        string? jsonPropertyName,
        NumericBound? structural,
        JsonElement? declared,
        bool isExclusive)
    {
        if (!structural.HasValue || !declared.HasValue)
        {
            return;
        }

        int comparison = CompareNumbers(
            declared.Value,
            structural.Value.Value);
        if (comparison < 0
            || (comparison == 0
                && structural.Value.IsExclusive
                && !isExclusive))
        {
            throw ContractMetadataFailure.Invalid(
                contractId,
                targetType,
                jsonPropertyName,
                "The declared lower bound extends below the structural contract.");
        }
    }

    private static void ValidateUpperBound (
        string contractId,
        Type targetType,
        string? jsonPropertyName,
        NumericBound? structural,
        JsonElement? declared,
        bool isExclusive)
    {
        if (!structural.HasValue || !declared.HasValue)
        {
            return;
        }

        int comparison = CompareNumbers(
            declared.Value,
            structural.Value.Value);
        if (comparison > 0
            || (comparison == 0
                && structural.Value.IsExclusive
                && !isExclusive))
        {
            throw ContractMetadataFailure.Invalid(
                contractId,
                targetType,
                jsonPropertyName,
                "The declared upper bound extends above the structural contract.");
        }
    }

    private static void ValidateKinds (
        string contractId,
        Type targetType,
        string? jsonPropertyName,
        JsonContractNodeKind kind,
        JsonContractScalarKind? scalarKind,
        JsonContractConstraints constraints)
    {
        bool hasNumeric = constraints.Minimum.HasValue
            || constraints.ExclusiveMinimum.HasValue
            || constraints.Maximum.HasValue
            || constraints.ExclusiveMaximum.HasValue;
        bool isNumeric = (kind is JsonContractNodeKind.Scalar
                or JsonContractNodeKind.Enum)
            && (scalarKind is JsonContractScalarKind.Integer
                or JsonContractScalarKind.Number);
        if (hasNumeric && !isNumeric)
        {
            throw ContractMetadataFailure.Invalid(
                contractId,
                targetType,
                jsonPropertyName,
                "Numeric bounds can only decorate an integer or number contract.");
        }

        bool hasString = constraints.MinimumLength.HasValue
            || constraints.MaximumLength.HasValue
            || constraints.Pattern is not null
            || constraints.Format is not null;
        bool isString = (kind is JsonContractNodeKind.Scalar
                or JsonContractNodeKind.Enum)
            && scalarKind == JsonContractScalarKind.String;
        if (hasString && !isString)
        {
            throw ContractMetadataFailure.Invalid(
                contractId,
                targetType,
                jsonPropertyName,
                "String constraints can only decorate a string contract.");
        }

        if ((constraints.MinimumItems.HasValue || constraints.MaximumItems.HasValue)
            && kind != JsonContractNodeKind.Array)
        {
            throw ContractMetadataFailure.Invalid(
                contractId,
                targetType,
                jsonPropertyName,
                "Item-count constraints can only decorate an array contract.");
        }

        if ((constraints.MinimumProperties.HasValue
                || constraints.MaximumProperties.HasValue)
            && kind is not JsonContractNodeKind.Object
                and not JsonContractNodeKind.Dictionary)
        {
            throw ContractMetadataFailure.Invalid(
                contractId,
                targetType,
                jsonPropertyName,
                "Property-count constraints can only decorate an object contract.");
        }
    }

    private static void ValidateRanges (
        string contractId,
        Type targetType,
        string? jsonPropertyName,
        JsonContractConstraints constraints)
    {
        ValidateIntegerRange(
            contractId,
            targetType,
            jsonPropertyName,
            constraints.MinimumLength,
            constraints.MaximumLength);
        ValidateIntegerRange(
            contractId,
            targetType,
            jsonPropertyName,
            constraints.MinimumItems,
            constraints.MaximumItems);
        ValidateIntegerRange(
            contractId,
            targetType,
            jsonPropertyName,
            constraints.MinimumProperties,
            constraints.MaximumProperties);

        NumericBound? lower = StrongestLowerBound(
            constraints.Minimum,
            constraints.ExclusiveMinimum);
        NumericBound? upper = StrongestUpperBound(
            constraints.Maximum,
            constraints.ExclusiveMaximum);
        if (lower.HasValue && upper.HasValue)
        {
            int comparison = CompareNumbers(
                lower.Value.Value,
                upper.Value.Value);
            if (comparison > 0
                || (comparison == 0
                    && (lower.Value.IsExclusive
                        || upper.Value.IsExclusive)))
            {
                throw ContractMetadataFailure.Invalid(
                    contractId,
                    targetType,
                    jsonPropertyName,
                    "The declared numeric lower bound exceeds the upper bound.");
            }
        }
    }

    private static void ValidateIntegerRange (
        string contractId,
        Type targetType,
        string? jsonPropertyName,
        int? minimum,
        int? maximum)
    {
        if (minimum < 0 || maximum < 0 || minimum > maximum)
        {
            throw ContractMetadataFailure.Invalid(
                contractId,
                targetType,
                jsonPropertyName,
                "A declared count range is negative or reversed.");
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

        int comparison = CompareNumbers(
            inclusive.Value,
            exclusive.Value);
        return comparison > 0
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

        int comparison = CompareNumbers(
            inclusive.Value,
            exclusive.Value);
        return comparison < 0
            ? new NumericBound(inclusive.Value, isExclusive: false)
            : new NumericBound(exclusive.Value, isExclusive: true);
    }

    private static int CompareNumbers (
        JsonElement left,
        JsonElement right)
    {
        return JsonSemanticValueCanonicalizer.CompareNumbers(left, right);
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

    private readonly struct NumericBounds
    {
        internal NumericBounds (
            JsonElement? inclusive,
            JsonElement? exclusive)
        {
            Inclusive = inclusive;
            Exclusive = exclusive;
        }

        internal JsonElement? Inclusive { get; }

        internal JsonElement? Exclusive { get; }
    }
}
