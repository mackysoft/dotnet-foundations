namespace MackySoft.JsonSchema.Generation.Annotations;

/// <summary> Declares inclusive string-length bounds. </summary>
[AttributeUsage(
    AttributeTargets.Property
    | AttributeTargets.Field
    | AttributeTargets.Class
    | AttributeTargets.Struct,
    Inherited = true)]
public sealed class JsonContractLengthAttribute : Attribute
{
    /// <summary> Initializes a string-length declaration. </summary>
    /// <param name="minimum"> The non-negative minimum length. </param>
    /// <param name="maximum"> The maximum length, which must not be less than <paramref name="minimum" />. </param>
    /// <exception cref="ArgumentOutOfRangeException"> A bound is negative or the bounds are reversed. </exception>
    public JsonContractLengthAttribute (
        int minimum,
        int maximum)
    {
        if (minimum < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(minimum),
                minimum,
                "The minimum length must be non-negative.");
        }

        if (maximum < minimum)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximum),
                maximum,
                "The maximum length must not be less than the minimum length.");
        }

        Minimum = minimum;
        Maximum = maximum;
    }

    /// <summary> Gets the inclusive minimum string length. </summary>
    public int Minimum { get; }

    /// <summary> Gets the inclusive maximum string length. </summary>
    public int Maximum { get; }
}
