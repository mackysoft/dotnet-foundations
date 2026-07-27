namespace MackySoft.JsonSchema.Generation.Internal.Metadata.Contracts;

internal sealed class MetadataDeclarationSnapshotEntry<TValue>
{
    internal MetadataDeclarationSnapshotEntry (
        string sourceId,
        TValue value)
    {
        SourceId = sourceId
            ?? throw new ArgumentNullException(nameof(sourceId));
        Value = value;
    }

    internal string SourceId { get; }

    internal TValue Value { get; }
}
