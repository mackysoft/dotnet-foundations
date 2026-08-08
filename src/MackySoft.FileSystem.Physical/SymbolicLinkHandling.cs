namespace MackySoft.FileSystem;

/// <summary> Specifies how an operation handles symbolic links, Windows junctions, and other reparse points. </summary>
public enum SymbolicLinkHandling
{
    /// <summary> Fail when the supplied boundary entry or a path segment below it is a link or reparse point. </summary>
    Reject = 0,

    /// <summary>
    /// Resolve symbolic links and Windows junctions. Other Windows reparse points fail as an unexpected entry kind.
    /// </summary>
    Follow,
}
