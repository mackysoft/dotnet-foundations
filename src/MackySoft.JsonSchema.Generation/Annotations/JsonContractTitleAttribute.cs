namespace MackySoft.JsonSchema.Generation.Annotations;

/// <summary> Declares a human-readable title for a JSON contract type or member. </summary>
[AttributeUsage(
    AttributeTargets.Class
    | AttributeTargets.Struct
    | AttributeTargets.Enum
    | AttributeTargets.Interface
    | AttributeTargets.Property
    | AttributeTargets.Field,
    Inherited = true)]
public sealed class JsonContractTitleAttribute : Attribute
{
    /// <summary> Initializes a title declaration. </summary>
    /// <param name="title"> Non-empty display text. </param>
    /// <exception cref="ArgumentException"> <paramref name="title" /> is empty or whitespace. </exception>
    public JsonContractTitleAttribute (string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException(
                "The contract title must not be null, empty, or whitespace.",
                nameof(title));
        }

        Title = title;
    }

    /// <summary> Gets the declared display text. </summary>
    public string Title { get; }
}
