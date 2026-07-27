using System.Text.Json;
using MackySoft.Json.Canonicalization;
using MackySoft.JsonSchema.Generation.ContractModel;
using MackySoft.JsonSchema.Generation.Diagnostics;
using MackySoft.JsonSchema.Generation.Extensibility;
using MackySoft.JsonSchema.Generation.Internal.Determinism;

namespace MackySoft.JsonSchema.Generation.Internal.ModelBuilding.Contributions;

internal static class ModelContributionResolver
{
    private static readonly HashSet<string> ReservedSemanticNames =
        new(
            new[]
            {
                "additionalProperties",
                "allowedValues",
                "annotations",
                "constant",
                "constraints",
                "contractDigest",
                "contractId",
                "contributions",
                "definitions",
                "description",
                "discriminator",
                "discriminatorValue",
                "examples",
                "exclusiveMaximum",
                "exclusiveMinimum",
                "format",
                "id",
                "isNullable",
                "isRequired",
                "items",
                "kind",
                "maximum",
                "maximumItems",
                "maximumLength",
                "maximumProperties",
                "minimum",
                "minimumItems",
                "minimumLength",
                "minimumProperties",
                "name",
                "pattern",
                "properties",
                "propertyName",
                "referenceId",
                "requiredProperties",
                "root",
                "scalarKind",
                "sourceId",
                "targetPointer",
                "title",
                "value",
                "variants",
            },
            StringComparer.Ordinal);

    internal static IReadOnlyList<JsonContractModelContribution> Resolve (
        string contractId,
        JsonContractNode root,
        IReadOnlyList<JsonContractDefinition> definitions,
        IReadOnlyList<IJsonContractModelContributor> contributors)
    {
        if (contractId is null)
        {
            throw new ArgumentNullException(nameof(contractId));
        }

        if (root is null)
        {
            throw new ArgumentNullException(nameof(root));
        }

        if (definitions is null)
        {
            throw new ArgumentNullException(nameof(definitions));
        }

        if (contributors is null)
        {
            throw new ArgumentNullException(nameof(contributors));
        }

        // Contributors may target only the source-independent objects written by
        // the semantic model projection.
        JsonContractModelTargetIndex targetIndex =
            JsonContractModelTargetIndex.Create(root, definitions);
        IJsonContractModelContributor[] orderedContributors = contributors.ToArray();
        if (orderedContributors.Any(static contributor => contributor is null))
        {
            throw InvalidContribution(
                contractId,
                root.Source.TargetType,
                jsonPropertyName: null,
                sourceIds: Array.Empty<string>(),
                "The model contributor collection contains null.");
        }

        Array.Sort(
            orderedContributors,
            static (left, right) => UnicodeCodePointComparer.Instance.Compare(
                left.StableId,
                right.StableId));

        // Collect the complete finite declaration set before resolving duplicate
        // locations so conflict diagnostics include every contributing source.
        JsonContractModelContext context = targetIndex.CreateContext(
            contractId,
            root,
            definitions);
        var declarations = new List<ContributionDeclaration>();
        foreach (IJsonContractModelContributor contributor in orderedContributors)
        {
            CollectContributorSnapshot(
                context,
                contributor,
                targetIndex,
                declarations);
        }

        return NormalizeDeclarations(
            contractId,
            root.Source.TargetType,
            targetIndex,
            declarations);
    }

    private static void CollectContributorSnapshot (
        JsonContractModelContext context,
        IJsonContractModelContributor contributor,
        JsonContractModelTargetIndex targetIndex,
        ICollection<ContributionDeclaration> declarations)
    {
        string sourceId = contributor.StableId;
        JsonContractModelContribution[] snapshot;
        try
        {
            IReadOnlyList<JsonContractModelContribution>? contributionSnapshot =
                contributor.GetContributions(context);
            if (contributionSnapshot is null)
            {
                throw new InvalidOperationException(
                    "The contributor returned a null snapshot.");
            }

            snapshot = contributionSnapshot.ToArray();
        }
        catch (Exception exception)
        {
            throw InvalidContribution(
                context.ContractId,
                context.Root.Source.TargetType,
                jsonPropertyName: null,
                new[] { sourceId },
                $"Model contributor '{sourceId}' failed to produce a finite snapshot.",
                exception);
        }

        foreach (JsonContractModelContribution? contribution in snapshot)
        {
            ValidateAndCollect(
                context.ContractId,
                context.Root,
                sourceId,
                contribution,
                targetIndex,
                declarations);
        }
    }

    private static void ValidateAndCollect (
        string contractId,
        JsonContractNode root,
        string sourceId,
        JsonContractModelContribution? contribution,
        JsonContractModelTargetIndex targetIndex,
        ICollection<ContributionDeclaration> declarations)
    {
        if (contribution is null)
        {
            throw InvalidContribution(
                contractId,
                root.Source.TargetType,
                jsonPropertyName: null,
                new[] { sourceId },
                $"Model contributor '{sourceId}' returned a null declaration.");
        }

        if (!string.Equals(
                sourceId,
                contribution.SourceId,
                StringComparison.Ordinal))
        {
            throw InvalidContribution(
                contractId,
                root.Source.TargetType,
                jsonPropertyName: null,
                new[] { sourceId },
                $"Model contributor '{sourceId}' returned a declaration with source ID '{contribution.SourceId}'.");
        }

        ValidateLocationText(
            contractId,
            root.Source.TargetType,
            sourceId,
            contribution.TargetPointer,
            contribution.Name);

        if (!targetIndex.TryResolve(
                contribution.Target,
                out Type? targetType,
                out string? jsonPropertyName)
            || targetType is null)
        {
            throw InvalidContribution(
                contractId,
                root.Source.TargetType,
                jsonPropertyName: null,
                new[] { sourceId },
                $"Model contributor '{sourceId}' targeted invalid semantic JSON Pointer '{contribution.TargetPointer}'.");
        }

        if (ReservedSemanticNames.Contains(contribution.Name))
        {
            throw InvalidContribution(
                contractId,
                targetType,
                jsonPropertyName,
                new[] { sourceId },
                $"Model contribution name '{contribution.Name}' is reserved by the contract model.");
        }

        if (contribution.Value.ValueKind == JsonValueKind.Undefined)
        {
            throw InvalidContribution(
                contractId,
                targetType,
                jsonPropertyName,
                new[] { sourceId },
                $"Model contribution '{contribution.Name}' at '{contribution.TargetPointer}' has an undefined value.");
        }

        byte[] canonicalValue;
        try
        {
            canonicalValue = JsonElementUtility.GetCanonicalBytes(
                contribution.Value);
        }
        catch (Exception exception) when (
            exception is JsonException
            or JsonCanonicalizationException
            or ArgumentException
            or InvalidOperationException)
        {
            throw InvalidContribution(
                contractId,
                targetType,
                jsonPropertyName,
                new[] { sourceId },
                $"Model contribution '{contribution.Name}' at '{contribution.TargetPointer}' is not a canonicalizable JSON value.",
                exception);
        }

        declarations.Add(
            new ContributionDeclaration(
                contribution.Target,
                contribution.Name,
                canonicalValue,
                sourceId));
    }

    private static IReadOnlyList<JsonContractModelContribution> NormalizeDeclarations (
        string contractId,
        Type rootTargetType,
        JsonContractModelTargetIndex targetIndex,
        IReadOnlyList<ContributionDeclaration> declarations)
    {
        ContributionDeclaration[] ordered = declarations.ToArray();
        Array.Sort(ordered, CompareDeclarations);

        var result = new List<JsonContractModelContribution>(ordered.Length);
        for (int start = 0; start < ordered.Length;)
        {
            int end = start + 1;
            while (end < ordered.Length
                && SameLocation(ordered[start], ordered[end]))
            {
                end++;
            }

            ContributionDeclaration selected = ordered[start];
            bool hasConflict = false;
            for (int index = start + 1; index < end; index++)
            {
                if (!selected.CanonicalValue.AsSpan().SequenceEqual(
                        ordered[index].CanonicalValue))
                {
                    hasConflict = true;
                    break;
                }
            }

            if (hasConflict)
            {
                if (!targetIndex.TryResolve(
                        selected.Target,
                        out Type? targetType,
                        out string? jsonPropertyName)
                    || targetType is null)
                {
                    throw InvalidContribution(
                        contractId,
                        rootTargetType,
                        jsonPropertyName: null,
                        new[] { selected.SourceId },
                        "A normalized model contribution no longer belongs to its generation context.");
                }

                throw new JsonContractGenerationException(
                    JsonContractGenerationFailureKind.ModelContributionConflict,
                    $"Model contributors declared conflicting values for '{selected.Name}' at '{selected.TargetPointer}'.",
                    contractId,
                    targetType,
                    jsonPropertyName,
                    metadataKind: null,
                    SortSourceIds(
                        ordered
                            .Skip(start)
                            .Take(end - start)
                            .Select(static declaration => declaration.SourceId)));
            }

            result.Add(
                new JsonContractModelContribution(
                    selected.Target,
                    selected.Name,
                    ParseCanonicalValue(selected.CanonicalValue),
                    selected.SourceId));
            start = end;
        }

        return result.AsReadOnly();
    }

    private static void ValidateLocationText (
        string contractId,
        Type targetType,
        string sourceId,
        string targetPointer,
        string name)
    {
        try
        {
            _ = UnicodeCodePointComparer.Instance.Compare(
                targetPointer,
                targetPointer);
            _ = UnicodeCodePointComparer.Instance.Compare(name, name);
        }
        catch (ArgumentException exception)
        {
            throw InvalidContribution(
                contractId,
                targetType,
                jsonPropertyName: null,
                new[] { sourceId },
                $"Model contributor '{sourceId}' declared a location containing invalid Unicode.",
                exception);
        }
    }

    private static JsonElement ParseCanonicalValue (byte[] canonicalValue)
    {
        using JsonDocument document = JsonDocument.Parse(canonicalValue);
        return document.RootElement.Clone();
    }

    private static bool SameLocation (
        ContributionDeclaration left,
        ContributionDeclaration right)
    {
        return string.Equals(
                left.TargetPointer,
                right.TargetPointer,
                StringComparison.Ordinal)
            && string.Equals(left.Name, right.Name, StringComparison.Ordinal);
    }

    private static int CompareDeclarations (
        ContributionDeclaration left,
        ContributionDeclaration right)
    {
        int pointerComparison = UnicodeCodePointComparer.Instance.Compare(
            left.TargetPointer,
            right.TargetPointer);
        if (pointerComparison != 0)
        {
            return pointerComparison;
        }

        int nameComparison = UnicodeCodePointComparer.Instance.Compare(
            left.Name,
            right.Name);
        return nameComparison != 0
            ? nameComparison
            : UnicodeCodePointComparer.Instance.Compare(
                left.SourceId,
                right.SourceId);
    }

    private static IReadOnlyList<string> SortSourceIds (
        IEnumerable<string> sourceIds)
    {
        string[] ordered = sourceIds
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        Array.Sort(ordered, UnicodeCodePointComparer.Instance);
        return Array.AsReadOnly(ordered);
    }

    private static JsonContractGenerationException InvalidContribution (
        string contractId,
        Type targetType,
        string? jsonPropertyName,
        IEnumerable<string> sourceIds,
        string message,
        Exception? innerException = null)
    {
        return new JsonContractGenerationException(
            JsonContractGenerationFailureKind.InvalidModelContribution,
            message,
            contractId,
            targetType,
            jsonPropertyName,
            metadataKind: null,
            SortSourceIds(sourceIds),
            innerException);
    }

    private sealed class ContributionDeclaration
    {
        public ContributionDeclaration (
            JsonContractModelTarget target,
            string name,
            byte[] canonicalValue,
            string sourceId)
        {
            Target = target;
            Name = name;
            CanonicalValue = canonicalValue;
            SourceId = sourceId;
        }

        public JsonContractModelTarget Target { get; }

        public string TargetPointer => Target.Pointer;

        public string Name { get; }

        public byte[] CanonicalValue { get; }

        public string SourceId { get; }
    }
}
