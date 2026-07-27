using System.Text.Json;
using MackySoft.JsonSchema.Generation.Internal.Common;

namespace MackySoft.JsonSchema.Generation.Internal.Metadata.Contracts;

internal sealed class MetadataDeclarationSnapshot
{
    internal MetadataDeclarationSnapshot (
        AnnotationDeclarations annotations,
        ValueConstraintDeclarations valueConstraints,
        ShapeConstraintDeclarations shapeConstraints)
    {
        Annotations = annotations
            ?? throw new ArgumentNullException(nameof(annotations));
        ValueConstraints = valueConstraints
            ?? throw new ArgumentNullException(nameof(valueConstraints));
        ShapeConstraints = shapeConstraints
            ?? throw new ArgumentNullException(nameof(shapeConstraints));
    }

    internal AnnotationDeclarations Annotations { get; }

    internal ValueConstraintDeclarations ValueConstraints { get; }

    internal ShapeConstraintDeclarations ShapeConstraints { get; }

    internal sealed class AnnotationDeclarations
    {
        internal AnnotationDeclarations (
            IEnumerable<MetadataDeclarationSnapshotEntry<string>> titles,
            IEnumerable<MetadataDeclarationSnapshotEntry<string>>
                descriptions,
            IEnumerable<MetadataDeclarationSnapshotEntry<JsonElement>>
                examples)
        {
            Titles = Copy(titles, nameof(titles));
            Descriptions = Copy(descriptions, nameof(descriptions));
            Examples = CloneJson(examples, nameof(examples));
        }

        internal IReadOnlyList<MetadataDeclarationSnapshotEntry<string>>
            Titles
        { get; }

        internal IReadOnlyList<MetadataDeclarationSnapshotEntry<string>>
            Descriptions
        { get; }

        internal IReadOnlyList<MetadataDeclarationSnapshotEntry<JsonElement>>
            Examples
        { get; }
    }

    internal sealed class ValueConstraintDeclarations
    {
        internal ValueConstraintDeclarations (
            IEnumerable<MetadataDeclarationSnapshotEntry<JsonElement>>
                constants,
            NumericBoundDeclarations numericBounds)
        {
            Constants = CloneJson(constants, nameof(constants));
            NumericBounds = numericBounds
                ?? throw new ArgumentNullException(nameof(numericBounds));
        }

        internal IReadOnlyList<MetadataDeclarationSnapshotEntry<JsonElement>>
            Constants
        { get; }

        internal NumericBoundDeclarations NumericBounds { get; }
    }

    internal sealed class NumericBoundDeclarations
    {
        internal NumericBoundDeclarations (
            IEnumerable<MetadataDeclarationSnapshotEntry<JsonElement>>
                minimums,
            IEnumerable<MetadataDeclarationSnapshotEntry<JsonElement>>
                exclusiveMinimums,
            IEnumerable<MetadataDeclarationSnapshotEntry<JsonElement>>
                maximums,
            IEnumerable<MetadataDeclarationSnapshotEntry<JsonElement>>
                exclusiveMaximums)
        {
            Minimums = CloneJson(minimums, nameof(minimums));
            ExclusiveMinimums = CloneJson(
                exclusiveMinimums,
                nameof(exclusiveMinimums));
            Maximums = CloneJson(maximums, nameof(maximums));
            ExclusiveMaximums = CloneJson(
                exclusiveMaximums,
                nameof(exclusiveMaximums));
        }

        internal IReadOnlyList<MetadataDeclarationSnapshotEntry<JsonElement>>
            Minimums
        { get; }

        internal IReadOnlyList<MetadataDeclarationSnapshotEntry<JsonElement>>
            ExclusiveMinimums
        { get; }

        internal IReadOnlyList<MetadataDeclarationSnapshotEntry<JsonElement>>
            Maximums
        { get; }

        internal IReadOnlyList<MetadataDeclarationSnapshotEntry<JsonElement>>
            ExclusiveMaximums
        { get; }
    }

    internal sealed class ShapeConstraintDeclarations
    {
        internal ShapeConstraintDeclarations (
            BoundDeclarations<int> lengths,
            BoundDeclarations<int> itemCounts,
            BoundDeclarations<int> propertyCounts,
            IEnumerable<MetadataDeclarationSnapshotEntry<string>> patterns)
        {
            Lengths = lengths
                ?? throw new ArgumentNullException(nameof(lengths));
            ItemCounts = itemCounts
                ?? throw new ArgumentNullException(nameof(itemCounts));
            PropertyCounts = propertyCounts
                ?? throw new ArgumentNullException(nameof(propertyCounts));
            Patterns = Copy(patterns, nameof(patterns));
        }

        internal BoundDeclarations<int> Lengths { get; }

        internal BoundDeclarations<int> ItemCounts { get; }

        internal BoundDeclarations<int> PropertyCounts { get; }

        internal IReadOnlyList<MetadataDeclarationSnapshotEntry<string>>
            Patterns
        { get; }
    }

    internal sealed class BoundDeclarations<TValue>
    {
        internal BoundDeclarations (
            IEnumerable<MetadataDeclarationSnapshotEntry<TValue>> minimums,
            IEnumerable<MetadataDeclarationSnapshotEntry<TValue>> maximums)
        {
            Minimums = Copy(minimums, nameof(minimums));
            Maximums = Copy(maximums, nameof(maximums));
        }

        internal IReadOnlyList<MetadataDeclarationSnapshotEntry<TValue>>
            Minimums
        { get; }

        internal IReadOnlyList<MetadataDeclarationSnapshotEntry<TValue>>
            Maximums
        { get; }
    }

    private static IReadOnlyList<MetadataDeclarationSnapshotEntry<TValue>>
        Copy<TValue> (
            IEnumerable<MetadataDeclarationSnapshotEntry<TValue>> values,
            string parameterName)
    {
        return JsonContractCollections.Copy(values, parameterName);
    }

    private static
        IReadOnlyList<MetadataDeclarationSnapshotEntry<JsonElement>>
        CloneJson (
            IEnumerable<MetadataDeclarationSnapshotEntry<JsonElement>> values,
            string parameterName)
    {
        IReadOnlyList<MetadataDeclarationSnapshotEntry<JsonElement>> copy =
            JsonContractCollections.Copy(values, parameterName);
        return Array.AsReadOnly(
            copy.Select(CloneJson).ToArray());
    }

    private static MetadataDeclarationSnapshotEntry<JsonElement> CloneJson (
        MetadataDeclarationSnapshotEntry<JsonElement> value)
    {
        return new MetadataDeclarationSnapshotEntry<JsonElement>(
            value.SourceId,
            JsonContractCollections.CloneNullableJsonElement(
                value.Value)!.Value);
    }
}
