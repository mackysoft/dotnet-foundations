namespace MackySoft.Json.Canonicalization;

/// <summary>
/// Represents a failure to produce RFC 8785 canonical JSON.
/// </summary>
public sealed class JsonCanonicalizationException : Exception
{
    internal JsonCanonicalizationException (
        JsonCanonicalizationFailureKind failureKind,
        string message,
        Exception? innerException = null)
        : base(message, innerException)
    {
        FailureKind = failureKind;
    }

    /// <summary>
    /// Gets the category of contract violation that prevented canonicalization.
    /// </summary>
    public JsonCanonicalizationFailureKind FailureKind { get; }
}
