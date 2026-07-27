namespace MackySoft.JsonSchema.Generation.Annotations;

/// <summary> Declares inclusive property-count bounds for an object value. </summary>
[AttributeUsage(
    AttributeTargets.Property
    | AttributeTargets.Field
    | AttributeTargets.Class
    | AttributeTargets.Struct,
    Inherited = true)]
public sealed class JsonContractPropertyCountAttribute : Attribute
{
    /// <summary> Initializes an object property-count declaration. </summary>
    /// <param name="minimum"> The non-negative minimum property count. </param>
    /// <param name="maximum"> The maximum count, which must not be less than <paramref name="minimum" />. </param>
    /// <exception cref="ArgumentOutOfRangeException"> A bound is negative or the bounds are reversed. </exception>
    public JsonContractPropertyCountAttribute (
        int minimum,
        int maximum)
    {
        if (minimum < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(minimum),
                minimum,
                "The minimum property count must be non-negative.");
        }

        if (maximum < minimum)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximum),
                maximum,
                "The maximum property count must not be less than the minimum property count.");
        }

        Minimum = minimum;
        Maximum = maximum;
    }

    /// <summary> Gets the inclusive minimum property count. </summary>
    public int Minimum { get; }

    /// <summary> Gets the inclusive maximum property count. </summary>
    public int Maximum { get; }
}
