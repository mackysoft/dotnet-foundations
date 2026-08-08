namespace MackySoft.FileSystem;

/// <summary> Specifies how single-file publication handles an existing regular-file target. </summary>
public enum ExistingTargetHandling
{
    /// <summary> Fail without changing the existing target. </summary>
    Reject = 0,

    /// <summary> Replace the existing regular file with the completed temporary file. </summary>
    Replace,
}
