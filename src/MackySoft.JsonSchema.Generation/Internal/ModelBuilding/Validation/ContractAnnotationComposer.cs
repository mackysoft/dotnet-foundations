using System.Text.Json;
using MackySoft.JsonSchema.Generation.ContractModel;
using MackySoft.JsonSchema.Generation.Internal.Determinism;
using MackySoft.JsonSchema.Generation.Internal.Metadata.Contracts;
using MackySoft.JsonSchema.Generation.Metadata;

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
            JsonContractMetadataKind.Title,
            surrogate?.Title,
            declared?.Title,
            metadata);
        string? description = ResolveText(
            contractId,
            targetType,
            jsonPropertyName,
            JsonContractMetadataKind.Description,
            surrogate?.Description,
            declared?.Description,
            metadata);

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
        JsonContractMetadataKind metadataKind,
        string? surrogateValue,
        string? declaredValue,
        ResolvedContractMetadata? metadata)
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

        IEnumerable<string> sourceIds = metadata?.MetadataDeclarations
            .Where(
                declaration =>
                    declaration.Metadata.Kind == metadataKind)
            .Select(static declaration => declaration.SourceId)
            ?? Array.Empty<string>();
        throw ContractMetadataFailure.Conflicting(
            contractId,
            targetType,
            jsonPropertyName,
            metadataKind,
            sourceIds,
            "A declaration on the mapped CLR source conflicts with the mapped surrogate annotation.");
    }
}
