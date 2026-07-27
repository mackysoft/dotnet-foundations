using System.Text.Json;
using MackySoft.Json.Canonicalization;
using MackySoft.JsonSchema.Generation.Annotations;
using MackySoft.JsonSchema.Generation.ContractModel;
using MackySoft.JsonSchema.Generation.Internal.Determinism;
using MackySoft.JsonSchema.Generation.Internal.Metadata.Contracts;
using MackySoft.JsonSchema.Generation.Internal.Metadata.Normalization;
using MackySoft.JsonSchema.Generation.Metadata;
using MackySoft.Text.Vocabularies;

namespace MackySoft.JsonSchema.Generation.Internal.Metadata.Declarations;

internal static class OneOfBranchDeclarationNormalizer
{
    internal static ResolvedOneOfBranch Normalize (
        MetadataResolutionTarget target,
        OneOfBranchAttribute attribute,
        string sourceId)
    {
        MetadataResolutionTarget typeTarget = ForTypeMetadata(target);
        ValidateName(typeTarget, attribute.Name, sourceId);
        string[] requiredProperties = NormalizeRequiredProperties(
            typeTarget,
            attribute.RequiredPropertyNames,
            sourceId);
        ValidateDescription(typeTarget, attribute.Description, sourceId);

        JsonElement? discriminatorValue = attribute.DiscriminatorValueJson is null
            ? null
            : ParseAndNormalizeAttributeJson(
                attribute.DiscriminatorValueJson,
                typeTarget,
                sourceId);
        JsonElement[] examples = attribute.ExampleJson is null
            ? Array.Empty<JsonElement>()
            : new[]
            {
                ParseAndNormalizeAttributeJson(
                    attribute.ExampleJson,
                    typeTarget,
                    sourceId),
            };

        return new ResolvedOneOfBranch(
            attribute.Name,
            requiredProperties,
            discriminatorValue,
            new JsonContractAnnotations(
                title: null,
                attribute.Description,
                examples));
    }

    internal static ResolvedOneOfBranch Normalize (
        MetadataResolutionTarget target,
        JsonContractBranchMetadata branch,
        string sourceId)
    {
        ValidateName(target, branch.Name, sourceId);
        string[] requiredProperties = NormalizeRequiredProperties(
            target,
            branch.RequiredPropertyNames,
            sourceId);
        ValidateDescription(target, branch.Description, sourceId);

        JsonElement? discriminatorValue = branch.DiscriminatorValue.HasValue
            ? NormalizeProviderJson(
                branch.DiscriminatorValue.Value,
                target,
                sourceId)
            : null;
        JsonElement[] examples = branch.Examples
            .Select(
                example => NormalizeProviderJson(
                    example,
                    target,
                    sourceId))
            .ToArray();
        Array.Sort(examples, JsonElementUtility.CompareCanonical);
        examples = examples
            .Distinct(JsonElementCanonicalEqualityComparer.Instance)
            .ToArray();

        return new ResolvedOneOfBranch(
            branch.Name,
            requiredProperties,
            discriminatorValue,
            new JsonContractAnnotations(
                title: null,
                branch.Description,
                examples));
    }

    private static string[] NormalizeRequiredProperties (
        MetadataResolutionTarget target,
        IEnumerable<string> propertyNames,
        string sourceId)
    {
        string[] requiredProperties = propertyNames.ToArray();
        foreach (string propertyName in requiredProperties)
        {
            MetadataTextContract.Validate(
                propertyName,
                target,
                JsonContractMetadataKind.OneOfBranch,
                sourceId,
                "A oneOf required property name");
        }

        Array.Sort(requiredProperties, UnicodeCodePointComparer.Instance);
        return requiredProperties
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static void ValidateName (
        MetadataResolutionTarget target,
        string name,
        string sourceId)
    {
        MetadataTextContract.Validate(
            name,
            target,
            JsonContractMetadataKind.OneOfBranch,
            sourceId,
            "The oneOf branch name");
    }

    private static void ValidateDescription (
        MetadataResolutionTarget target,
        string? description,
        string sourceId)
    {
        if (description is null)
        {
            return;
        }

        MetadataTextContract.Validate(
            description,
            target,
            JsonContractMetadataKind.OneOfBranch,
            sourceId,
            "The oneOf branch description");
    }

    private static JsonElement ParseAndNormalizeAttributeJson (
        string json,
        MetadataResolutionTarget target,
        string sourceId)
    {
        JsonElement value;
        try
        {
            value = MetadataJsonNormalizer.ParseStrict(json);
        }
        catch (Exception exception) when (
            exception is JsonException or ArgumentException)
        {
            throw MetadataFailure.Invalid(
                target,
                JsonContractMetadataKind.OneOfBranch,
                new[] { sourceId },
                $"The {Vocabulary.GetText(JsonContractMetadataKind.OneOfBranch)} attribute value is not strict JSON.",
                exception);
        }

        try
        {
            return MetadataJsonNormalizer.Normalize(value);
        }
        catch (Exception exception) when (
            exception is JsonCanonicalizationException
            or JsonException
            or InvalidOperationException
            or ArgumentException)
        {
            throw MetadataFailure.Invalid(
                target,
                JsonContractMetadataKind.OneOfBranch,
                new[] { sourceId },
                "The oneOfBranch attribute value cannot be canonicalized.",
                exception);
        }
    }

    private static JsonElement NormalizeProviderJson (
        JsonElement value,
        MetadataResolutionTarget target,
        string sourceId)
    {
        try
        {
            MetadataJsonNormalizer.Validate(value);
            return MetadataJsonNormalizer.Normalize(value);
        }
        catch (Exception exception) when (
            exception is JsonCanonicalizationException
            or JsonException
            or InvalidOperationException
            or ArgumentException
            or ObjectDisposedException)
        {
            throw MetadataFailure.Invalid(
                target,
                JsonContractMetadataKind.OneOfBranch,
                new[] { sourceId },
                $"Provider '{sourceId}' returned malformed "
                + $"{Vocabulary.GetText(JsonContractMetadataKind.OneOfBranch)} JSON.",
                exception);
        }
    }

    private static MetadataResolutionTarget ForTypeMetadata (
        MetadataResolutionTarget target)
    {
        return new MetadataResolutionTarget(
            target.ContractId,
            target.TargetType,
            jsonPropertyName: null,
            isMember: false);
    }
}
