namespace MackySoft.FileSystem;

/// <summary> Classifies one filesystem entry without following a symbolic link at the inspected path. </summary>
public enum FileSystemEntryState
{
    /// <summary> No filesystem entry currently exists at the path. </summary>
    Missing = 0,

    /// <summary> The path currently identifies a regular file. </summary>
    RegularFile,

    /// <summary> The path currently identifies a directory. </summary>
    Directory,

    /// <summary> The path currently identifies a symbolic link or Windows junction. </summary>
    SymbolicLink,

    /// <summary> The path currently identifies a Windows reparse point that is not a symbolic link or junction. </summary>
    ReparsePoint,

    /// <summary> The path currently identifies another node kind, such as a device, socket, or named pipe. </summary>
    Other,
}
