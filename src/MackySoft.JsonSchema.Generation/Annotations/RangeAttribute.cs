namespace MackySoft.JsonSchema.Generation.Annotations;

/// <summary> Declares lower and upper numeric bounds using JSON number text. </summary>
[AttributeUsage(
    AttributeTargets.Property
    | AttributeTargets.Field
    | AttributeTargets.Class
    | AttributeTargets.Struct,
    Inherited = true)]
public sealed class RangeAttribute : Attribute
{
    /// <summary> Initializes a numeric range declaration. </summary>
    /// <param name="minimumJson"> The lower-bound JSON number text, or <see langword="null" /> when unbounded. </param>
    /// <param name="maximumJson"> The upper-bound JSON number text, or <see langword="null" /> when unbounded. </param>
    /// <exception cref="ArgumentException"> Both bounds are <see langword="null" />. </exception>
    public RangeAttribute (
        string? minimumJson,
        string? maximumJson)
    {
        if (minimumJson is null && maximumJson is null)
        {
            throw new ArgumentException("At least one numeric bound must be supplied.");
        }

        MinimumJson = minimumJson;
        MaximumJson = maximumJson;
    }

    /// <summary> Gets the lower-bound JSON text, or <see langword="null" /> when unbounded. </summary>
    public string? MinimumJson { get; }

    /// <summary> Gets the upper-bound JSON text, or <see langword="null" /> when unbounded. </summary>
    public string? MaximumJson { get; }

    /// <summary> Gets or sets whether the lower bound excludes its declared value. </summary>
    public bool ExclusiveMinimum { get; set; }

    /// <summary> Gets or sets whether the upper bound excludes its declared value. </summary>
    public bool ExclusiveMaximum { get; set; }
}
