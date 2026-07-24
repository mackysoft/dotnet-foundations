namespace MackySoft.FileSystem;

/// <summary> Describes a failed guarded path factory operation without product-specific diagnostics. </summary>
public readonly struct PathValidationFailure : IEquatable<PathValidationFailure>
{
    private readonly string? message;

    private PathValidationFailure (
        PathValidationFailureKind kind,
        string message)
    {
        Kind = kind;
        this.message = message;
    }

    /// <summary> Gets the typed failure classification, or <see cref="PathValidationFailureKind.None" /> after a successful operation. </summary>
    public PathValidationFailureKind Kind { get; }

    /// <summary> Gets a diagnostic suitable for logs; an empty string indicates that validation succeeded. </summary>
    public string Message => message ?? string.Empty;

    internal static PathValidationFailure Create (
        PathValidationFailureKind kind,
        string message)
    {
        if (kind == PathValidationFailureKind.None
            || !Enum.IsDefined(typeof(PathValidationFailureKind), kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Failure kind must be defined and non-None.");
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Failure message must not be empty.", nameof(message));
        }

        return new PathValidationFailure(kind, message);
    }

    /// <inheritdoc />
    public bool Equals (PathValidationFailure other)
    {
        return Kind == other.Kind
            && string.Equals(Message, other.Message, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public override bool Equals (object? obj)
    {
        return obj is PathValidationFailure other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode ()
    {
        unchecked
        {
            return ((int)Kind * 397) ^ StringComparer.Ordinal.GetHashCode(Message);
        }
    }

    /// <summary>
    /// Formats the CLR enum member name and diagnostic message for troubleshooting.
    /// </summary>
    /// <remarks>
    /// The returned text is not a stable external token. Transport adapters that need a text vocabulary must
    /// define and own that vocabulary separately.
    /// </remarks>
    public override string ToString ()
    {
        return Kind == PathValidationFailureKind.None
            ? nameof(PathValidationFailureKind.None)
            : $"{Kind}: {Message}";
    }

    /// <summary> Compares two failures by kind and diagnostic message. </summary>
    public static bool operator == (
        PathValidationFailure left,
        PathValidationFailure right)
    {
        return left.Equals(right);
    }

    /// <summary> Compares two failures by kind and diagnostic message. </summary>
    public static bool operator != (
        PathValidationFailure left,
        PathValidationFailure right)
    {
        return !left.Equals(right);
    }
}
