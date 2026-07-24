namespace MackySoft.FileSystem;

/// <summary> Reports a guarded path factory failure from an operation that cannot return a structured failure value. </summary>
public sealed class PathValidationException : ArgumentException
{
    internal PathValidationException (
        PathValidationFailure failure,
        string parameterName)
        : base(failure.Message, parameterName)
    {
        Failure = failure;
    }

    /// <summary> Gets the typed failure classification and product-neutral diagnostic. </summary>
    public PathValidationFailure Failure { get; }
}
