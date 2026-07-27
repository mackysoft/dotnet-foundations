using System.Text.Json;
using MackySoft.JsonSchema.Generation.ContractModel;
using MackySoft.JsonSchema.Generation.Internal.Metadata.Contracts;
using MackySoft.JsonSchema.Generation.Internal.Metadata.Declarations;

namespace MackySoft.JsonSchema.Generation.Internal.Metadata.Validation;

internal static class ContractMetadataValidator
{
    internal static ResolvedContractMetadata Resolve (
        MetadataResolutionTarget target,
        MetadataDeclarationSet declarations)
    {
        // Single-value facts reject unequal sources, while examples form a
        // canonical, order-independent set.
        string? title = MetadataDeclarationResolver.ResolveText(
            declarations.Titles,
            target,
            "title",
            "The title metadata value");
        string? description = MetadataDeclarationResolver.ResolveText(
            declarations.Descriptions,
            target,
            "description",
            "The description metadata value");
        IReadOnlyList<JsonElement> examples =
            MetadataDeclarationResolver.ResolveExamples(
                declarations.Examples,
                target);

        JsonElement? constant = MetadataDeclarationResolver.ResolveJson(
            declarations.Constants,
            target,
            "const");
        JsonElement? minimum = MetadataDeclarationResolver.ResolveJson(
            declarations.Minimums,
            target,
            "minimum",
            requireNumber: true);
        JsonElement? exclusiveMinimum =
            MetadataDeclarationResolver.ResolveJson(
                declarations.ExclusiveMinimums,
                target,
                "exclusiveMinimum",
                requireNumber: true);
        JsonElement? maximum = MetadataDeclarationResolver.ResolveJson(
            declarations.Maximums,
            target,
            "maximum",
            requireNumber: true);
        JsonElement? exclusiveMaximum =
            MetadataDeclarationResolver.ResolveJson(
                declarations.ExclusiveMaximums,
                target,
                "exclusiveMaximum",
                requireNumber: true);

        int? minimumLength =
            MetadataDeclarationResolver.ResolveNonNegativeInteger(
                declarations.MinimumLengths,
                target,
                "minimumLength");
        int? maximumLength =
            MetadataDeclarationResolver.ResolveNonNegativeInteger(
                declarations.MaximumLengths,
                target,
                "maximumLength");
        int? minimumItems =
            MetadataDeclarationResolver.ResolveNonNegativeInteger(
                declarations.MinimumItemCounts,
                target,
                "minimumItems");
        int? maximumItems =
            MetadataDeclarationResolver.ResolveNonNegativeInteger(
                declarations.MaximumItemCounts,
                target,
                "maximumItems");
        int? minimumProperties =
            MetadataDeclarationResolver.ResolveNonNegativeInteger(
                declarations.MinimumPropertyCounts,
                target,
                "minimumProperties");
        int? maximumProperties =
            MetadataDeclarationResolver.ResolveNonNegativeInteger(
                declarations.MaximumPropertyCounts,
                target,
                "maximumProperties");
        string? pattern = MetadataDeclarationResolver.ResolvePattern(
            declarations.Patterns,
            target);

        var constraints = new JsonContractConstraints(
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
            format: null);
        MetadataConstraintValidator.Validate(
            target,
            declarations,
            constraints);

        return new ResolvedContractMetadata(
            new JsonContractAnnotations(title, description, examples),
            constraints,
            constant,
            declarations.CreateSnapshot());
    }
}
