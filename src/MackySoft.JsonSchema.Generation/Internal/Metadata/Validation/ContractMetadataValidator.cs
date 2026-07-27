using System.Text.Json;
using MackySoft.JsonSchema.Generation.ContractModel;
using MackySoft.JsonSchema.Generation.Internal.Metadata.Contracts;
using MackySoft.JsonSchema.Generation.Internal.Metadata.Declarations;
using MackySoft.JsonSchema.Generation.Metadata;

namespace MackySoft.JsonSchema.Generation.Internal.Metadata.Validation;

internal static class ContractMetadataValidator
{
    internal static ResolvedContractMetadata Resolve (
        MetadataResolutionTarget target,
        MetadataDeclarationSet declarations)
    {
        ResolvedContractMetadata.MetadataProvenance[] metadataDeclarations =
            MetadataValueResolver.SortAndValidate(
                declarations.Metadata,
                target);
        ResolvedContractMetadata.OneOfBranchProvenance[] branchDeclarations =
            declarations.OneOfBranches.ToArray();
        ResolvedContractMetadata.DiscriminatorProvenance[]
            discriminatorDeclarations =
                declarations.Discriminators.ToArray();

        if (!target.IsMember
            && MetadataValueResolver.HasKind(
                metadataDeclarations,
                JsonContractMetadataKind.Required))
        {
            throw MetadataFailure.Invalid(
                target,
                JsonContractMetadataKind.Required,
                MetadataValueResolver.SourceIds(
                    metadataDeclarations,
                    JsonContractMetadataKind.Required),
                "Required metadata is valid only for a serialized member.");
        }

        // Single-value facts reject unequal sources, while examples and enum
        // values form canonical, order-independent sets.
        string? title = MetadataValueResolver.ResolveString(
            metadataDeclarations,
            JsonContractMetadataKind.Title,
            target);
        string? description = MetadataValueResolver.ResolveString(
            metadataDeclarations,
            JsonContractMetadataKind.Description,
            target);
        IReadOnlyList<JsonElement> examples =
            MetadataValueResolver.ResolveJsonSet(
                metadataDeclarations,
                JsonContractMetadataKind.Example);

        bool? isRequired = MetadataValueResolver.ResolveMarker(
            metadataDeclarations,
            JsonContractMetadataKind.Required);
        bool? allowsNull = MetadataValueResolver.ResolveMarker(
            metadataDeclarations,
            JsonContractMetadataKind.AllowNull);
        bool isArbitrary = MetadataValueResolver.ResolveMarker(
            metadataDeclarations,
            JsonContractMetadataKind.Arbitrary) == true;

        JsonElement? constant = MetadataValueResolver.ResolveJson(
            metadataDeclarations,
            JsonContractMetadataKind.Const,
            target);
        IReadOnlyList<JsonElement> allowedValues =
            MetadataValueResolver.ResolveJsonSet(
                metadataDeclarations,
                JsonContractMetadataKind.EnumValue);

        JsonElement? minimum = MetadataValueResolver.ResolveJson(
            metadataDeclarations,
            JsonContractMetadataKind.Minimum,
            target);
        JsonElement? exclusiveMinimum = MetadataValueResolver.ResolveJson(
            metadataDeclarations,
            JsonContractMetadataKind.ExclusiveMinimum,
            target);
        JsonElement? maximum = MetadataValueResolver.ResolveJson(
            metadataDeclarations,
            JsonContractMetadataKind.Maximum,
            target);
        JsonElement? exclusiveMaximum = MetadataValueResolver.ResolveJson(
            metadataDeclarations,
            JsonContractMetadataKind.ExclusiveMaximum,
            target);

        int? minimumLength = MetadataValueResolver.ResolveInteger(
            metadataDeclarations,
            JsonContractMetadataKind.MinimumLength,
            target);
        int? maximumLength = MetadataValueResolver.ResolveInteger(
            metadataDeclarations,
            JsonContractMetadataKind.MaximumLength,
            target);
        int? minimumItems = MetadataValueResolver.ResolveInteger(
            metadataDeclarations,
            JsonContractMetadataKind.MinimumItems,
            target);
        int? maximumItems = MetadataValueResolver.ResolveInteger(
            metadataDeclarations,
            JsonContractMetadataKind.MaximumItems,
            target);
        int? minimumProperties = MetadataValueResolver.ResolveInteger(
            metadataDeclarations,
            JsonContractMetadataKind.MinimumProperties,
            target);
        int? maximumProperties = MetadataValueResolver.ResolveInteger(
            metadataDeclarations,
            JsonContractMetadataKind.MaximumProperties,
            target);
        string? pattern = MetadataValueResolver.ResolveString(
            metadataDeclarations,
            JsonContractMetadataKind.Pattern,
            target);
        string? format = MetadataValueResolver.ResolveString(
            metadataDeclarations,
            JsonContractMetadataKind.Format,
            target);

        MetadataConstraintValidator.ValidateValueConstraints(
            target,
            metadataDeclarations,
            constant,
            allowedValues,
            minimum,
            exclusiveMinimum,
            maximum,
            exclusiveMaximum,
            minimumLength,
            maximumLength,
            minimumItems,
            maximumItems,
            minimumProperties,
            maximumProperties);

        IReadOnlyList<ResolvedOneOfBranch> resolvedBranches =
            OneOfMetadataResolver.ResolveBranches(
                branchDeclarations,
                target);
        string? discriminatorPropertyName =
            OneOfMetadataResolver.ResolveDiscriminator(
                discriminatorDeclarations,
                target);

        MetadataConstraintValidator.ValidateArbitraryContract(
            isArbitrary,
            metadataDeclarations,
            branchDeclarations,
            discriminatorDeclarations,
            target);

        return new ResolvedContractMetadata(
            new JsonContractAnnotations(title, description, examples),
            new JsonContractConstraints(
                minimum,
                exclusiveMinimum,
                maximum,
                exclusiveMaximum,
                minimumLength,
                maximumLength,
                minimumItems,
                maximumItems,
                minimumProperties,
                maximumProperties,
                pattern,
                format),
            isRequired,
            allowsNull,
            isArbitrary,
            constant,
            allowedValues,
            discriminatorPropertyName,
            resolvedBranches,
            metadataDeclarations,
            branchDeclarations,
            discriminatorDeclarations);
    }
}
