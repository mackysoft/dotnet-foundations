using MackySoft.JsonSchema.Generation.ContractModel;
using MackySoft.JsonSchema.Generation.Internal.Metadata.Contracts;
using MackySoft.JsonSchema.Generation.Metadata;

namespace MackySoft.JsonSchema.Generation.Internal.ModelBuilding.Validation;

/// <summary> Enforces that an arbitrary JSON declaration remains structurally unconstrained. </summary>
internal static class ArbitraryContractValidator
{
    public static void Validate (
        string contractId,
        Type targetType,
        string? jsonPropertyName,
        ResolvedContractMetadata metadata)
    {
        JsonContractConstraints constraints = metadata.Constraints;
        bool hasConstraints = constraints.Minimum.HasValue
            || constraints.ExclusiveMinimum.HasValue
            || constraints.Maximum.HasValue
            || constraints.ExclusiveMaximum.HasValue
            || constraints.MinimumLength.HasValue
            || constraints.MaximumLength.HasValue
            || constraints.MinimumItems.HasValue
            || constraints.MaximumItems.HasValue
            || constraints.MinimumProperties.HasValue
            || constraints.MaximumProperties.HasValue
            || constraints.Pattern is not null
            || constraints.Format is not null;
        if (metadata.Constant.HasValue
            || metadata.AllowedValues.Count != 0
            || metadata.OneOfBranches.Count != 0
            || metadata.DiscriminatorPropertyName is not null
            || hasConstraints)
        {
            throw ContractMetadataFailure.Invalid(
                contractId,
                targetType,
                jsonPropertyName,
                JsonContractMetadataKind.Arbitrary,
                "An arbitrary JSON declaration cannot also impose structural metadata.");
        }
    }
}
