namespace MackySoft.FileSystem;

/// <summary> Classifies product-independent filesystem operation failures. </summary>
public enum FileSystemOperationFailureKind
{
    /// <summary> The operation succeeded. </summary>
    None = 0,

    /// <summary> A required filesystem entry does not exist. </summary>
    EntryNotFound,

    /// <summary> The operating system denied access to a required entry or operation. </summary>
    AccessDenied,

    /// <summary> The selected policy does not permit a symbolic link or Windows reparse point. </summary>
    LinkNotAllowed,

    /// <summary> Symbolic-link resolution encountered a cycle or exceeded the supported link depth. </summary>
    LinkCycle,

    /// <summary>
    /// The link-resolved target is outside the link-resolved boundary under current-platform lexical identity rules.
    /// </summary>
    OutsideBoundary,

    /// <summary> An entry has a node kind that the operation cannot use. </summary>
    UnexpectedEntryKind,

    /// <summary> The operation requires a missing target but an entry already exists. </summary>
    AlreadyExists,

    /// <summary> A link-resolved path value or required entry changed while an operation was in progress. </summary>
    ConcurrentChange,

    /// <summary> The running platform does not provide a required filesystem capability. </summary>
    PlatformNotSupported,

    /// <summary> The operating system reported another input/output failure. </summary>
    IoFailure,
}
