namespace MackySoft.FileSystem;

/// <summary> Identifies why path text could not produce a guarded path value. </summary>
/// <remarks>
/// This CLR enum is a typed classification, not a stable external text vocabulary. Member names must not be
/// serialized or parsed as transport tokens.
/// </remarks>
public enum PathValidationFailureKind
{
    /// <summary> Path validation succeeded. </summary>
    None,

    /// <summary> The path text was <see langword="null" /> or empty. </summary>
    EmptyPath,

    /// <summary> The path text violates the package's supported current-platform lexical format. </summary>
    InvalidPathFormat,

    /// <summary> An absolute path was required, but the path text was not fully qualified. </summary>
    ExpectedAbsolutePath,

    /// <summary> A root-relative path was required, but the path text was rooted. </summary>
    ExpectedRootRelativePath,

    /// <summary> The path traverses above or resolves outside the supplied boundary. </summary>
    OutsideBoundary,
}
