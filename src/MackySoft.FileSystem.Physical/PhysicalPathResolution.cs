namespace MackySoft.FileSystem;

/// <summary> Describes a link-resolved path snapshot produced under a lexical boundary. </summary>
/// <remarks>
/// The resolved path and target observation are snapshots. They do not prevent a later link or entry change.
/// </remarks>
public sealed class PhysicalPathResolution
{
    internal PhysicalPathResolution (
        ContainedPath requestedPath,
        ContainedPath resolvedPath,
        FileSystemEntryObservation targetObservation)
    {
        RequestedPath = requestedPath ?? throw new ArgumentNullException(nameof(requestedPath));
        ResolvedPath = resolvedPath ?? throw new ArgumentNullException(nameof(resolvedPath));
        TargetObservation = targetObservation ?? throw new ArgumentNullException(nameof(targetObservation));
    }

    /// <summary> Gets the lexical boundary and target supplied by the caller. </summary>
    public ContainedPath RequestedPath { get; }

    /// <summary>
    /// Gets the resolved boundary and target after applying the selected link policy and current-platform lexical identity rules.
    /// </summary>
    public ContainedPath ResolvedPath { get; }

    /// <summary> Gets the state observed at <see cref="ResolvedPath" />. </summary>
    public FileSystemEntryObservation TargetObservation { get; }
}
