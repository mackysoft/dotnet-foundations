namespace MackySoft.FileSystem;

/// <summary> Describes the observed state of one absolute path at one point in time. </summary>
/// <remarks>
/// The observation does not reserve the path. A later filesystem operation must account for concurrent changes.
/// </remarks>
public sealed class FileSystemEntryObservation
{
    internal FileSystemEntryObservation (
        AbsolutePath path,
        FileSystemEntryState state)
    {
        Path = path ?? throw new ArgumentNullException(nameof(path));
        if (!Enum.IsDefined(typeof(FileSystemEntryState), state))
        {
            throw new ArgumentOutOfRangeException(nameof(state), state, "Entry state must be defined.");
        }

        State = state;
    }

    /// <summary> Gets the path whose state was observed. </summary>
    public AbsolutePath Path { get; }

    /// <summary> Gets the state observed without following a link at <see cref="Path" />. </summary>
    public FileSystemEntryState State { get; }
}
