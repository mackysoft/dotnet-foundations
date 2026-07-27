using System.Text.Json;
using MackySoft.JsonSchema.Generation.Diagnostics;
using MackySoft.JsonSchema.Generation.Internal.Determinism;
using MackySoft.JsonSchema.Generation.Internal.Metadata.Contracts;
using MackySoft.JsonSchema.Generation.Internal.Metadata.Declarations;
using MackySoft.JsonSchema.Generation.Internal.Metadata.Normalization;

namespace MackySoft.JsonSchema.Generation.Internal.Metadata.Validation;

internal static class MetadataDeclarationResolver
{
    internal static string? ResolveText (
        IReadOnlyList<MetadataDeclaration<string>> declarations,
        MetadataResolutionTarget target,
        string declarationName,
        string valueName,
        bool rejectWhitespaceOnly = true)
    {
        if (declarations.Count == 0)
        {
            return null;
        }

        foreach (MetadataDeclaration<string> declaration in declarations)
        {
            MetadataTextContract.Validate(
                declaration.Value,
                target,
                declaration.SourceId,
                valueName,
                rejectWhitespaceOnly);
        }

        string value = declarations[0].Value;
        if (declarations.Any(
                declaration => !string.Equals(
                    value,
                    declaration.Value,
                    StringComparison.Ordinal)))
        {
            throw MetadataFailure.Conflicting(
                target,
                declarationName,
                SourceIds(declarations));
        }

        return value;
    }

    internal static string? ResolvePattern (
        IReadOnlyList<MetadataDeclaration<string>> declarations,
        MetadataResolutionTarget target)
    {
        string? pattern = ResolveText(
            declarations,
            target,
            "pattern",
            "The pattern metadata value",
            rejectWhitespaceOnly: false);
        if (pattern is null)
        {
            return null;
        }

        try
        {
            JsonSchemaPatternContract.Validate(pattern);
        }
        catch (ArgumentException exception)
        {
            throw MetadataFailure.Invalid(
                target,
                SourceIds(declarations),
                "The pattern metadata value is malformed.",
                exception);
        }

        return pattern;
    }

    internal static int? ResolveNonNegativeInteger (
        IReadOnlyList<MetadataDeclaration<int>> declarations,
        MetadataResolutionTarget target,
        string declarationName)
    {
        if (declarations.Count == 0)
        {
            return null;
        }

        foreach (MetadataDeclaration<int> declaration in declarations)
        {
            if (declaration.Value < 0)
            {
                throw MetadataFailure.Invalid(
                    target,
                    new[] { declaration.SourceId },
                    $"{declarationName} metadata must be non-negative.");
            }
        }

        int value = declarations[0].Value;
        if (declarations.Any(
                declaration => declaration.Value != value))
        {
            throw MetadataFailure.Conflicting(
                target,
                declarationName,
                SourceIds(declarations));
        }

        return value;
    }

    internal static JsonElement? ResolveJson (
        IReadOnlyList<MetadataDeclaration<JsonElement>> declarations,
        MetadataResolutionTarget target,
        string declarationName,
        bool requireNumber = false)
    {
        if (declarations.Count == 0)
        {
            return null;
        }

        JsonElement[] values = declarations
            .Select(
                declaration => NormalizeJson(
                    declaration,
                    target,
                    declarationName,
                    requireNumber))
            .ToArray();
        JsonElement value = values[0];
        if (values.Any(
                candidate => JsonElementUtility.CompareCanonical(
                    value,
                    candidate) != 0))
        {
            throw MetadataFailure.Conflicting(
                target,
                declarationName,
                SourceIds(declarations));
        }

        return value;
    }

    internal static IReadOnlyList<JsonElement> ResolveExamples (
        IReadOnlyList<MetadataDeclaration<JsonElement>> declarations,
        MetadataResolutionTarget target)
    {
        JsonElement[] values = declarations
            .Select(
                declaration => NormalizeJson(
                    declaration,
                    target,
                    "example",
                    requireNumber: false))
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

    private static JsonElement NormalizeJson (
        MetadataDeclaration<JsonElement> declaration,
        MetadataResolutionTarget target,
        string declarationName,
        bool requireNumber)
    {
        try
        {
            if (requireNumber
                && declaration.Value.ValueKind != JsonValueKind.Number)
            {
                throw new InvalidOperationException(
                    "A numeric bound must contain a JSON number.");
            }

            return MetadataJsonValueNormalizer.Normalize(declaration.Value);
        }
        catch (JsonContractGenerationException)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is JsonException
                or InvalidOperationException
                or ArgumentException
                or FormatException
                or ObjectDisposedException)
        {
            throw MetadataFailure.Invalid(
                target,
                new[] { declaration.SourceId },
                $"The {declarationName} metadata value is malformed.",
                exception);
        }
    }

    private static IEnumerable<string> SourceIds<TValue> (
        IEnumerable<MetadataDeclaration<TValue>> declarations)
    {
        return declarations.Select(
            static declaration => declaration.SourceId);
    }
}
