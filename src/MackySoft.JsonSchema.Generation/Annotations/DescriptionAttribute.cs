namespace MackySoft.JsonSchema.Generation.Annotations;

/// <summary> Declares explanatory text for a JSON contract type or member. </summary>
[AttributeUsage(
    AttributeTargets.Class
    | AttributeTargets.Struct
    | AttributeTargets.Enum
    | AttributeTargets.Interface
    | AttributeTargets.Property
    | AttributeTargets.Field,
    Inherited = true)]
public sealed class DescriptionAttribute : Attribute
{
    /// <summary> Initializes a description declaration. </summary>
    /// <param name="description"> Non-empty explanatory text. </param>
    /// <exception cref="ArgumentException"> <paramref name="description" /> is empty or whitespace. </exception>
    public DescriptionAttribute (string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException(
                "The contract description must not be null, empty, or whitespace.",
                nameof(description));
        }

        Description = description;
    }

    /// <summary> Gets the declared explanatory text. </summary>
    public string Description { get; }
}
