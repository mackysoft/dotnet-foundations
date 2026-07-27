using System.Text.Json;
using MackySoft.JsonSchema.Generation.ContractModel;
using MackySoft.JsonSchema.Generation.Internal.Determinism;
using MackySoft.JsonSchema.Generation.Internal.Metadata.Contracts;

namespace MackySoft.JsonSchema.Generation.Internal.ModelBuilding.Validation;

/// <summary>
/// Preserves annotations carried by a mapped surrogate while validating
/// declarations attached to the mapped CLR source.
/// </summary>
internal static class ContractAnnotationComposer
{
    internal static JsonContractAnnotations Compose (
        string contractId,
        Type targetType,
        string? jsonPropertyName,
        JsonContractAnnotations? surrogate,
        ResolvedContractMetadata? metadata)
    {
        JsonContractAnnotations? declared = metadata?.Annotations;
        string? title = ResolveText(
            contractId,
            targetType,
            jsonPropertyName,
            surrogate?.Title,
            declared?.Title,
            metadata?.TitleSourceIds ?? Array.Empty<string>());
        string? description = ResolveText(
            contractId,
            targetType,
            jsonPropertyName,
            surrogate?.Description,
            declared?.Description,
            metadata?.DescriptionSourceIds ?? Array.Empty<string>());

        JsonElement[] examples = (surrogate?.Examples
                ?? Array.Empty<JsonElement>())
            .Concat(declared?.Examples ?? Array.Empty<JsonElement>())
            .ToArray();
        Array.Sort(examples, JsonElementUtility.CompareCanonical);
        JsonElement[] uniqueExamples = examples
            .Distinct(JsonElementCanonicalEqualityComparer.Instance)
            .ToArray();

        return new JsonContractAnnotations(
            title,
            description,
            uniqueExamples);
    }

    private static string? ResolveText (
        string contractId,
        Type targetType,
        string? jsonPropertyName,
        string? surrogateValue,
        string? declaredValue,
        IEnumerable<string> sourceIds)
    {
        if (surrogateValue is null)
        {
            return declaredValue;
        }

        if (declaredValue is null
            || string.Equals(
                surrogateValue,
                declaredValue,
                StringComparison.Ordinal))
        {
            return surrogateValue;
        }

        throw ContractMetadataFailure.Conflicting(
            contractId,
            targetType,
            jsonPropertyName,
            sourceIds,
            "A declaration on the mapped CLR source conflicts with the mapped surrogate annotation.");
    }
}
