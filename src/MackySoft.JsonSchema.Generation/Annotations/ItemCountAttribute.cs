namespace MackySoft.JsonSchema.Generation.Annotations;

/// <summary> Declares inclusive item-count bounds for an array value. </summary>
[AttributeUsage(
    AttributeTargets.Property
    | AttributeTargets.Field
    | AttributeTargets.Class
    | AttributeTargets.Struct,
    Inherited = true)]
public sealed class ItemCountAttribute : Attribute
{
    /// <summary> Initializes an array item-count declaration. </summary>
    /// <param name="minimum"> The non-negative minimum item count. </param>
    /// <param name="maximum"> The maximum count, which must not be less than <paramref name="minimum" />. </param>
    /// <exception cref="ArgumentOutOfRangeException"> A bound is negative or the bounds are reversed. </exception>
    public ItemCountAttribute (
        int minimum,
        int maximum)
    {
        if (minimum < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(minimum),
                minimum,
                "The minimum item count must be non-negative.");
        }

        if (maximum < minimum)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximum),
                maximum,
                "The maximum item count must not be less than the minimum item count.");
        }

        Minimum = minimum;
        Maximum = maximum;
    }

    /// <summary> Gets the inclusive minimum item count. </summary>
    public int Minimum { get; }

    /// <summary> Gets the inclusive maximum item count. </summary>
    public int Maximum { get; }
}
