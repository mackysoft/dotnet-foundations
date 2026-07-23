namespace MackySoft.Json.Canonicalization;

/// <summary>
/// Identifies why a JSON value could not be canonicalized.
/// </summary>
public enum JsonCanonicalizationFailureKind
{
    /// <summary>
    /// The input is not a single JSON value accepted by the strict parser.
    /// </summary>
    InvalidJson,

    /// <summary>
    /// An object contains the same decoded property name more than once.
    /// </summary>
    DuplicateProperty,

    /// <summary>
    /// The input contains invalid UTF-8 or an unpaired UTF-16 surrogate.
    /// </summary>
    InvalidUnicode,

    /// <summary>
    /// A JSON number cannot be represented as a finite IEEE 754 binary64 value.
    /// </summary>
    NumberNotRepresentable,

    /// <summary>
    /// A JSON number is represented as negative zero.
    /// </summary>
    NegativeZero,

    /// <summary>
    /// The raw UTF-8 JSON value exceeds the supported nesting depth.
    /// </summary>
    MaximumDepthExceeded,
}
