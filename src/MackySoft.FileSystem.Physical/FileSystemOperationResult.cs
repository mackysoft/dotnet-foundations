namespace MackySoft.FileSystem;

/// <summary> Represents the success or classified failure of a physical filesystem operation. </summary>
public readonly struct FileSystemOperationResult
{
    private FileSystemOperationResult (FileSystemOperationFailure failure)
    {
        Failure = failure;
    }

    /// <summary> Gets whether the operation completed successfully. </summary>
    public bool IsSuccess => Failure.Kind == FileSystemOperationFailureKind.None;

    /// <summary> Gets <see cref="FileSystemOperationFailureKind.None" /> on success; otherwise the operation failure. </summary>
    public FileSystemOperationFailure Failure { get; }

    internal static FileSystemOperationResult Success ()
    {
        return default;
    }

    internal static FileSystemOperationResult FailureResult (FileSystemOperationFailure failure)
    {
        if (failure.Kind == FileSystemOperationFailureKind.None)
        {
            throw new ArgumentException("A failed result requires a non-None failure.", nameof(failure));
        }

        return new FileSystemOperationResult(failure);
    }
}
