namespace MackySoft.JsonSchema.Generation.Internal.Metadata.Declarations;

internal sealed class MetadataDeclaration<TValue>
{
    internal MetadataDeclaration (
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
