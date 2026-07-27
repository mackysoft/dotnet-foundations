using System.Globalization;
using System.Text.Json;
using MackySoft.Json.Canonicalization;
using MackySoft.JsonSchema.Generation.Internal.Determinism;
using MackySoft.JsonSchema.Generation.Internal.Metadata.Contracts;
using MackySoft.JsonSchema.Generation.Internal.Metadata.Normalization;
using MackySoft.JsonSchema.Generation.Metadata;
using MackySoft.Text.Vocabularies;

namespace MackySoft.JsonSchema.Generation.Internal.Metadata.Validation;

internal static class MetadataValueResolver
{
    internal static ResolvedContractMetadata.MetadataProvenance[] SortAndValidate (
        IEnumerable<ResolvedContractMetadata.MetadataProvenance> declarations,
        MetadataResolutionTarget target)
    {
        ResolvedContractMetadata.MetadataProvenance[] result =
            declarations.ToArray();
        Array.Sort(result, CompareDeclarations);
        foreach (ResolvedContractMetadata.MetadataProvenance declaration in result)
        {
            ValidateDeclaration(declaration, target);
        }

        return result;
    }

    internal static string? ResolveString (
        IReadOnlyList<ResolvedContractMetadata.MetadataProvenance> declarations,
        JsonContractMetadataKind kind,
        MetadataResolutionTarget target)
    {
        ResolvedContractMetadata.MetadataProvenance[] matches =
            ForKind(declarations, kind);
        if (matches.Length == 0)
        {
            return null;
        }

        string value = matches[0].Metadata.StringValue!;
        if (matches.Any(
                declaration => !string.Equals(
                    value,
                    declaration.Metadata.StringValue,
                    StringComparison.Ordinal)))
        {
            throw MetadataFailure.Conflicting(
                target,
                kind,
                matches.Select(static declaration => declaration.SourceId));
        }

        return value;
    }

    internal static JsonElement? ResolveJson (
        IReadOnlyList<ResolvedContractMetadata.MetadataProvenance> declarations,
        JsonContractMetadataKind kind,
        MetadataResolutionTarget target)
    {
        ResolvedContractMetadata.MetadataProvenance[] matches =
            ForKind(declarations, kind);
        if (matches.Length == 0)
        {
            return null;
        }

        JsonElement value = matches[0].Metadata.JsonValue!.Value;
        if (matches.Any(
                declaration => JsonElementUtility.CompareCanonical(
                    value,
                    declaration.Metadata.JsonValue!.Value) != 0))
        {
            throw MetadataFailure.Conflicting(
                target,
                kind,
                matches.Select(static declaration => declaration.SourceId));
        }

        return MetadataJsonNormalizer.Normalize(value);
    }

    internal static int? ResolveInteger (
        IReadOnlyList<ResolvedContractMetadata.MetadataProvenance> declarations,
        JsonContractMetadataKind kind,
        MetadataResolutionTarget target)
    {
        ResolvedContractMetadata.MetadataProvenance[] matches =
            ForKind(declarations, kind);
        if (matches.Length == 0)
        {
            return null;
        }

        int value = matches[0].Metadata.IntegerValue!.Value;
        if (matches.Any(
                declaration => declaration.Metadata.IntegerValue!.Value != value))
        {
            throw MetadataFailure.Conflicting(
                target,
                kind,
                matches.Select(static declaration => declaration.SourceId));
        }

        return value;
    }

    internal static bool? ResolveMarker (
        IReadOnlyList<ResolvedContractMetadata.MetadataProvenance> declarations,
        JsonContractMetadataKind kind)
    {
        return HasKind(declarations, kind) ? true : null;
    }

    internal static IReadOnlyList<JsonElement> ResolveJsonSet (
        IReadOnlyList<ResolvedContractMetadata.MetadataProvenance> declarations,
        JsonContractMetadataKind kind)
    {
        JsonElement[] values = ForKind(declarations, kind)
            .Select(
                static declaration => MetadataJsonNormalizer.Normalize(
                    declaration.Metadata.JsonValue!.Value))
            .ToArray();
        Array.Sort(values, JsonElementUtility.CompareCanonical);

        if (values.Length < 2)
        {
            return Array.AsReadOnly(values);
        }

        var uniqueValues = new List<JsonElement>(values.Length)
        {
            values[0],
        };
        for (int index = 1; index < values.Length; index++)
        {
            if (JsonElementUtility.CompareCanonical(
                    uniqueValues[uniqueValues.Count - 1],
                    values[index]) != 0)
            {
                uniqueValues.Add(values[index]);
            }
        }

        return uniqueValues.AsReadOnly();
    }

    internal static bool HasKind (
        IReadOnlyList<ResolvedContractMetadata.MetadataProvenance> declarations,
        JsonContractMetadataKind kind)
    {
        return declarations.Any(
            declaration => declaration.Metadata.Kind == kind);
    }

    internal static IReadOnlyList<string> SourceIds (
        IReadOnlyList<ResolvedContractMetadata.MetadataProvenance> declarations,
        params JsonContractMetadataKind[] kinds)
    {
        return MetadataFailure.SortSourceIds(
            declarations
                .Where(declaration => kinds.Contains(declaration.Metadata.Kind))
                .Select(static declaration => declaration.SourceId));
    }

    private static void ValidateDeclaration (
        ResolvedContractMetadata.MetadataProvenance declaration,
        MetadataResolutionTarget target)
    {
        JsonContractMetadata metadata = declaration.Metadata;
        try
        {
            switch (metadata.Kind)
            {
                case JsonContractMetadataKind.Title:
                case JsonContractMetadataKind.Description:
                case JsonContractMetadataKind.Format:
                    RequirePayload(
                        metadata,
                        requiresString: true,
                        requiresJson: false,
                        requiresInteger: false);
                    MetadataTextContract.Validate(
                        metadata.StringValue!,
                        target,
                        metadata.Kind,
                        declaration.SourceId,
                        $"The {Vocabulary.GetText(metadata.Kind)} metadata value",
                        rejectWhitespaceOnly:
                            true);
                    break;

                case JsonContractMetadataKind.Pattern:
                    RequirePayload(
                        metadata,
                        requiresString: true,
                        requiresJson: false,
                        requiresInteger: false);
                    MetadataTextContract.Validate(
                        metadata.StringValue!,
                        target,
                        metadata.Kind,
                        declaration.SourceId,
                        "The pattern metadata value",
                        rejectWhitespaceOnly: false);
                    JsonSchemaPatternContract.Validate(metadata.StringValue!);
                    break;

                case JsonContractMetadataKind.Example:
                case JsonContractMetadataKind.Const:
                case JsonContractMetadataKind.EnumValue:
                    RequirePayload(
                        metadata,
                        requiresString: false,
                        requiresJson: true,
                        requiresInteger: false);
                    MetadataJsonNormalizer.Validate(metadata.JsonValue!.Value);
                    break;

                case JsonContractMetadataKind.Minimum:
                case JsonContractMetadataKind.ExclusiveMinimum:
                case JsonContractMetadataKind.Maximum:
                case JsonContractMetadataKind.ExclusiveMaximum:
                    RequirePayload(
                        metadata,
                        requiresString: false,
                        requiresJson: true,
                        requiresInteger: false);
                    MetadataJsonNormalizer.Validate(metadata.JsonValue!.Value);
                    if (metadata.JsonValue.Value.ValueKind != JsonValueKind.Number)
                    {
                        throw new InvalidOperationException(
                            "A numeric bound must contain a JSON number.");
                    }
                    break;

                case JsonContractMetadataKind.MinimumLength:
                case JsonContractMetadataKind.MaximumLength:
                case JsonContractMetadataKind.MinimumItems:
                case JsonContractMetadataKind.MaximumItems:
                case JsonContractMetadataKind.MinimumProperties:
                case JsonContractMetadataKind.MaximumProperties:
                    RequirePayload(
                        metadata,
                        requiresString: false,
                        requiresJson: false,
                        requiresInteger: true);
                    if (metadata.IntegerValue!.Value < 0)
                    {
                        throw new InvalidOperationException(
                            "Length and count metadata must be non-negative.");
                    }
                    break;

                case JsonContractMetadataKind.Required:
                case JsonContractMetadataKind.AllowNull:
                case JsonContractMetadataKind.Arbitrary:
                    RequirePayload(
                        metadata,
                        requiresString: false,
                        requiresJson: false,
                        requiresInteger: false);
                    break;

                default:
                    throw new InvalidOperationException(
                        "The metadata kind is not declared by the metadata vocabulary.");
            }
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
                metadata.Kind,
                new[] { declaration.SourceId },
                $"The {Vocabulary.GetText(metadata.Kind)} metadata value is malformed.",
                exception);
        }
    }

    private static void RequirePayload (
        JsonContractMetadata metadata,
        bool requiresString,
        bool requiresJson,
        bool requiresInteger)
    {
        if ((metadata.StringValue is not null) != requiresString
            || metadata.JsonValue.HasValue != requiresJson
            || metadata.IntegerValue.HasValue != requiresInteger)
        {
            throw new InvalidOperationException(
                "The metadata payload does not match its declared kind.");
        }
    }

    private static ResolvedContractMetadata.MetadataProvenance[] ForKind (
        IReadOnlyList<ResolvedContractMetadata.MetadataProvenance> declarations,
        JsonContractMetadataKind kind)
    {
        return declarations
            .Where(declaration => declaration.Metadata.Kind == kind)
            .ToArray();
    }

    private static int CompareDeclarations (
        ResolvedContractMetadata.MetadataProvenance left,
        ResolvedContractMetadata.MetadataProvenance right)
    {
        int kindComparison = UnicodeCodePointComparer.Instance.Compare(
            Vocabulary.GetText(left.Metadata.Kind),
            Vocabulary.GetText(right.Metadata.Kind));
        if (kindComparison != 0)
        {
            return kindComparison;
        }

        int sourceComparison = UnicodeCodePointComparer.Instance.Compare(
            left.SourceId,
            right.SourceId);
        return sourceComparison != 0
            ? sourceComparison
            : string.CompareOrdinal(
                GetPayloadSortKey(left.Metadata),
                GetPayloadSortKey(right.Metadata));
    }

    private static string GetPayloadSortKey (JsonContractMetadata metadata)
    {
        if (metadata.StringValue is not null)
        {
            return $"s:{metadata.StringValue.Length}:{metadata.StringValue}";
        }

        if (metadata.JsonValue.HasValue)
        {
            JsonElement value = metadata.JsonValue.Value;
            return value.ValueKind == JsonValueKind.Undefined
                ? "j:undefined"
                : $"j:{(int)value.ValueKind}:{value.GetRawText()}";
        }

        return metadata.IntegerValue.HasValue
            ? $"i:{metadata.IntegerValue.Value.ToString(CultureInfo.InvariantCulture)}"
            : "m:";
    }
}
