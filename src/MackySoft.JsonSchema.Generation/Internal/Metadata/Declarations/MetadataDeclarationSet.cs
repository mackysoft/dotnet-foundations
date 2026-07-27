using System.Text.Json;
using MackySoft.JsonSchema.Generation.Internal.Determinism;
using MackySoft.JsonSchema.Generation.Internal.Metadata.Contracts;

namespace MackySoft.JsonSchema.Generation.Internal.Metadata.Declarations;

internal sealed class MetadataDeclarationSet
{
    private readonly List<MetadataDeclaration<string>> titles = new();
    private readonly List<MetadataDeclaration<string>> descriptions = new();
    private readonly List<MetadataDeclaration<JsonElement>> examples = new();
    private readonly List<MetadataDeclaration<JsonElement>> constants = new();
    private readonly List<MetadataDeclaration<JsonElement>> minimums = new();
    private readonly List<MetadataDeclaration<JsonElement>>
        exclusiveMinimums =
            new();
    private readonly List<MetadataDeclaration<JsonElement>> maximums = new();
    private readonly List<MetadataDeclaration<JsonElement>>
        exclusiveMaximums =
            new();
    private readonly List<MetadataDeclaration<int>> minimumLengths = new();
    private readonly List<MetadataDeclaration<int>> maximumLengths = new();
    private readonly List<MetadataDeclaration<int>> minimumItemCounts = new();
    private readonly List<MetadataDeclaration<int>> maximumItemCounts = new();
    private readonly List<MetadataDeclaration<int>>
        minimumPropertyCounts =
            new();
    private readonly List<MetadataDeclaration<int>>
        maximumPropertyCounts =
            new();
    private readonly List<MetadataDeclaration<string>> patterns = new();

    internal IReadOnlyList<MetadataDeclaration<string>> Titles => titles;

    internal IReadOnlyList<MetadataDeclaration<string>> Descriptions =>
        descriptions;

    internal IReadOnlyList<MetadataDeclaration<JsonElement>> Examples =>
        examples;

    internal IReadOnlyList<MetadataDeclaration<JsonElement>> Constants =>
        constants;

    internal IReadOnlyList<MetadataDeclaration<JsonElement>> Minimums =>
        minimums;

    internal IReadOnlyList<MetadataDeclaration<JsonElement>>
        ExclusiveMinimums =>
            exclusiveMinimums;

    internal IReadOnlyList<MetadataDeclaration<JsonElement>> Maximums =>
        maximums;

    internal IReadOnlyList<MetadataDeclaration<JsonElement>>
        ExclusiveMaximums =>
            exclusiveMaximums;

    internal IReadOnlyList<MetadataDeclaration<int>> MinimumLengths =>
        minimumLengths;

    internal IReadOnlyList<MetadataDeclaration<int>> MaximumLengths =>
        maximumLengths;

    internal IReadOnlyList<MetadataDeclaration<int>> MinimumItemCounts =>
        minimumItemCounts;

    internal IReadOnlyList<MetadataDeclaration<int>> MaximumItemCounts =>
        maximumItemCounts;

    internal IReadOnlyList<MetadataDeclaration<int>> MinimumPropertyCounts =>
        minimumPropertyCounts;

    internal IReadOnlyList<MetadataDeclaration<int>> MaximumPropertyCounts =>
        maximumPropertyCounts;

    internal IReadOnlyList<MetadataDeclaration<string>> Patterns => patterns;

    internal IEnumerable<string> LengthBoundSourceIds =>
        BoundSourceIds(minimumLengths, maximumLengths);

    internal IEnumerable<string> ItemCountBoundSourceIds =>
        BoundSourceIds(minimumItemCounts, maximumItemCounts);

    internal IEnumerable<string> PropertyCountBoundSourceIds =>
        BoundSourceIds(minimumPropertyCounts, maximumPropertyCounts);

    internal IEnumerable<string> NumericBoundSourceIds =>
        SourceIds(minimums)
            .Concat(SourceIds(exclusiveMinimums))
            .Concat(SourceIds(maximums))
            .Concat(SourceIds(exclusiveMaximums));

    internal void AddTitle (
        string sourceId,
        string value)
    {
        titles.Add(new MetadataDeclaration<string>(sourceId, value));
    }

    internal void AddDescription (
        string sourceId,
        string value)
    {
        descriptions.Add(new MetadataDeclaration<string>(sourceId, value));
    }

    internal void AddExample (
        string sourceId,
        JsonElement value)
    {
        examples.Add(
            new MetadataDeclaration<JsonElement>(
                sourceId,
                JsonElementUtility.Clone(value)));
    }

    internal void AddConstant (
        string sourceId,
        JsonElement value)
    {
        constants.Add(
            new MetadataDeclaration<JsonElement>(
                sourceId,
                JsonElementUtility.Clone(value)));
    }

    internal void AddMinimum (
        string sourceId,
        JsonElement value)
    {
        minimums.Add(
            new MetadataDeclaration<JsonElement>(
                sourceId,
                JsonElementUtility.Clone(value)));
    }

    internal void AddExclusiveMinimum (
        string sourceId,
        JsonElement value)
    {
        exclusiveMinimums.Add(
            new MetadataDeclaration<JsonElement>(
                sourceId,
                JsonElementUtility.Clone(value)));
    }

    internal void AddMaximum (
        string sourceId,
        JsonElement value)
    {
        maximums.Add(
            new MetadataDeclaration<JsonElement>(
                sourceId,
                JsonElementUtility.Clone(value)));
    }

    internal void AddExclusiveMaximum (
        string sourceId,
        JsonElement value)
    {
        exclusiveMaximums.Add(
            new MetadataDeclaration<JsonElement>(
                sourceId,
                JsonElementUtility.Clone(value)));
    }

    internal void AddMinimumLength (
        string sourceId,
        int value)
    {
        minimumLengths.Add(new MetadataDeclaration<int>(sourceId, value));
    }

    internal void AddMaximumLength (
        string sourceId,
        int value)
    {
        maximumLengths.Add(new MetadataDeclaration<int>(sourceId, value));
    }

    internal void AddMinimumItemCount (
        string sourceId,
        int value)
    {
        minimumItemCounts.Add(new MetadataDeclaration<int>(sourceId, value));
    }

    internal void AddMaximumItemCount (
        string sourceId,
        int value)
    {
        maximumItemCounts.Add(new MetadataDeclaration<int>(sourceId, value));
    }

    internal void AddMinimumPropertyCount (
        string sourceId,
        int value)
    {
        minimumPropertyCounts.Add(
            new MetadataDeclaration<int>(sourceId, value));
    }

    internal void AddMaximumPropertyCount (
        string sourceId,
        int value)
    {
        maximumPropertyCounts.Add(
            new MetadataDeclaration<int>(sourceId, value));
    }

    internal void AddPattern (
        string sourceId,
        string value)
    {
        patterns.Add(new MetadataDeclaration<string>(sourceId, value));
    }

    internal void AddRange (MetadataDeclarationSet source)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        titles.AddRange(source.titles);
        descriptions.AddRange(source.descriptions);
        examples.AddRange(source.examples);
        constants.AddRange(source.constants);
        minimums.AddRange(source.minimums);
        exclusiveMinimums.AddRange(source.exclusiveMinimums);
        maximums.AddRange(source.maximums);
        exclusiveMaximums.AddRange(source.exclusiveMaximums);
        minimumLengths.AddRange(source.minimumLengths);
        maximumLengths.AddRange(source.maximumLengths);
        minimumItemCounts.AddRange(source.minimumItemCounts);
        maximumItemCounts.AddRange(source.maximumItemCounts);
        minimumPropertyCounts.AddRange(source.minimumPropertyCounts);
        maximumPropertyCounts.AddRange(source.maximumPropertyCounts);
        patterns.AddRange(source.patterns);
    }

    internal void AddRange (MetadataDeclarationSnapshot source)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        AddAnnotations(source.Annotations);
        AddValueConstraints(source.ValueConstraints);
        AddShapeConstraints(source.ShapeConstraints);
    }

    internal MetadataDeclarationSnapshot CreateSnapshot ()
    {
        return new MetadataDeclarationSnapshot(
            CreateAnnotationSnapshot(),
            CreateValueConstraintSnapshot(),
            CreateShapeConstraintSnapshot());
    }

    internal static MetadataDeclarationSet Merge (
        ResolvedContractMetadata baseline,
        ResolvedContractMetadata overlay)
    {
        var result = new MetadataDeclarationSet();
        result.AddRange(baseline.Declarations);
        result.AddRange(overlay.Declarations);
        return result;
    }

    private void AddAnnotations (
        MetadataDeclarationSnapshot.AnnotationDeclarations source)
    {
        AddRange(source.Titles, AddTitle);
        AddRange(source.Descriptions, AddDescription);
        AddRange(source.Examples, AddExample);
    }

    private void AddValueConstraints (
        MetadataDeclarationSnapshot.ValueConstraintDeclarations source)
    {
        AddRange(source.Constants, AddConstant);
        AddRange(source.NumericBounds.Minimums, AddMinimum);
        AddRange(
            source.NumericBounds.ExclusiveMinimums,
            AddExclusiveMinimum);
        AddRange(source.NumericBounds.Maximums, AddMaximum);
        AddRange(
            source.NumericBounds.ExclusiveMaximums,
            AddExclusiveMaximum);
    }

    private void AddShapeConstraints (
        MetadataDeclarationSnapshot.ShapeConstraintDeclarations source)
    {
        AddRange(source.Lengths.Minimums, AddMinimumLength);
        AddRange(source.Lengths.Maximums, AddMaximumLength);
        AddRange(source.ItemCounts.Minimums, AddMinimumItemCount);
        AddRange(source.ItemCounts.Maximums, AddMaximumItemCount);
        AddRange(
            source.PropertyCounts.Minimums,
            AddMinimumPropertyCount);
        AddRange(
            source.PropertyCounts.Maximums,
            AddMaximumPropertyCount);
        AddRange(source.Patterns, AddPattern);
    }

    private MetadataDeclarationSnapshot.AnnotationDeclarations
        CreateAnnotationSnapshot ()
    {
        return new MetadataDeclarationSnapshot.AnnotationDeclarations(
            Snapshot(titles),
            Snapshot(descriptions),
            Snapshot(examples));
    }

    private MetadataDeclarationSnapshot.ValueConstraintDeclarations
        CreateValueConstraintSnapshot ()
    {
        var numericBounds =
            new MetadataDeclarationSnapshot.NumericBoundDeclarations(
                Snapshot(minimums),
                Snapshot(exclusiveMinimums),
                Snapshot(maximums),
                Snapshot(exclusiveMaximums));
        return new MetadataDeclarationSnapshot.ValueConstraintDeclarations(
            Snapshot(constants),
            numericBounds);
    }

    private MetadataDeclarationSnapshot.ShapeConstraintDeclarations
        CreateShapeConstraintSnapshot ()
    {
        return new MetadataDeclarationSnapshot.ShapeConstraintDeclarations(
            CreateBounds(minimumLengths, maximumLengths),
            CreateBounds(minimumItemCounts, maximumItemCounts),
            CreateBounds(minimumPropertyCounts, maximumPropertyCounts),
            Snapshot(patterns));
    }

    private static MetadataDeclarationSnapshot.BoundDeclarations<TValue>
        CreateBounds<TValue> (
            IEnumerable<MetadataDeclaration<TValue>> minimums,
            IEnumerable<MetadataDeclaration<TValue>> maximums)
    {
        return new MetadataDeclarationSnapshot.BoundDeclarations<TValue>(
            Snapshot(minimums),
            Snapshot(maximums));
    }

    private static void AddRange<TValue> (
        IEnumerable<MetadataDeclarationSnapshotEntry<TValue>> source,
        Action<string, TValue> add)
    {
        foreach (MetadataDeclarationSnapshotEntry<TValue> declaration in source)
        {
            add(declaration.SourceId, declaration.Value);
        }
    }

    private static IReadOnlyList<MetadataDeclarationSnapshotEntry<TValue>>
        Snapshot<TValue> (
        IEnumerable<MetadataDeclaration<TValue>> source)
    {
        return Array.AsReadOnly(
            source
                .Select(
                    static declaration =>
                        new MetadataDeclarationSnapshotEntry<TValue>(
                            declaration.SourceId,
                            declaration.Value))
                .ToArray());
    }

    private static IEnumerable<string> BoundSourceIds<TValue> (
        IEnumerable<MetadataDeclaration<TValue>> minimumDeclarations,
        IEnumerable<MetadataDeclaration<TValue>> maximumDeclarations)
    {
        return SourceIds(minimumDeclarations)
            .Concat(SourceIds(maximumDeclarations));
    }

    private static IEnumerable<string> SourceIds<TValue> (
        IEnumerable<MetadataDeclaration<TValue>> declarations)
    {
        return declarations.Select(
            static declaration => declaration.SourceId);
    }
}
