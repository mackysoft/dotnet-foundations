namespace MackySoft.FileSystem;

/// <summary> Describes a failed physical filesystem operation without product-specific diagnostics. </summary>
public readonly struct FileSystemOperationFailure : IEquatable<FileSystemOperationFailure>
{
    private readonly AbsolutePath? path;
    private readonly string? message;

    private FileSystemOperationFailure (
        FileSystemOperationFailureKind kind,
        AbsolutePath path,
        string message)
    {
        Kind = kind;
        this.path = path;
        this.message = message;
    }

    /// <summary> Gets the typed failure classification, or <see cref="FileSystemOperationFailureKind.None" /> after success. </summary>
    public FileSystemOperationFailureKind Kind { get; }

    /// <summary> Gets the path at which the failure was observed, or <see langword="null" /> after success. </summary>
    public AbsolutePath? Path => path;

    /// <summary> Gets a diagnostic suitable for logs; an empty string indicates success. </summary>
    public string Message => message ?? string.Empty;

    internal static FileSystemOperationFailure Create (
        FileSystemOperationFailureKind kind,
        AbsolutePath path,
        string message)
    {
        if (kind == FileSystemOperationFailureKind.None
            || !Enum.IsDefined(typeof(FileSystemOperationFailureKind), kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Failure kind must be defined and non-None.");
        }

        ArgumentNullException.ThrowIfNull(path);
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Failure message must not be empty.", nameof(message));
        }

        return new FileSystemOperationFailure(kind, path, message);
    }

    /// <inheritdoc />
    public bool Equals (FileSystemOperationFailure other)
    {
        return Kind == other.Kind
            && Equals(Path, other.Path)
            && string.Equals(Message, other.Message, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public override bool Equals (object? obj)
    {
        return obj is FileSystemOperationFailure other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode ()
    {
        return HashCode.Combine(Kind, Path, StringComparer.Ordinal.GetHashCode(Message));
    }

    /// <summary> Formats the CLR failure name, path, and diagnostic for troubleshooting. </summary>
    /// <remarks> The returned text is not a stable external token. </remarks>
    public override string ToString ()
    {
        return Kind == FileSystemOperationFailureKind.None
            ? nameof(FileSystemOperationFailureKind.None)
            : $"{Kind} at {Path}: {Message}";
    }

    /// <summary> Compares two failures by kind, path, and diagnostic message. </summary>
    public static bool operator == (
        FileSystemOperationFailure left,
        FileSystemOperationFailure right)
    {
        return left.Equals(right);
    }

    /// <summary> Compares two failures by kind, path, and diagnostic message. </summary>
    public static bool operator != (
        FileSystemOperationFailure left,
        FileSystemOperationFailure right)
    {
        return !left.Equals(right);
    }
}
