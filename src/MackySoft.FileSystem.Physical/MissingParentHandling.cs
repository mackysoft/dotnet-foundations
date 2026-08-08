namespace MackySoft.FileSystem;

/// <summary> Specifies how single-file publication handles missing parent directories. </summary>
public enum MissingParentHandling
{
    /// <summary> Fail when the target parent directory does not exist. </summary>
    Reject = 0,

    /// <summary>
    /// Create the missing parent directory chain before publication and retain it if publication later fails or is canceled.
    /// </summary>
    Create,
}
