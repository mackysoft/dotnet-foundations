namespace MackySoft.JsonSchema.Generation.Annotations;

/// <summary> Declares the JSON property that selects a tagged-union branch. </summary>
[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface,
    Inherited = true)]
public sealed class DiscriminatorAttribute : Attribute
{
    /// <summary> Initializes a discriminator declaration. </summary>
    /// <param name="propertyName"> The exact non-empty serialized JSON property name. </param>
    /// <exception cref="ArgumentException"> <paramref name="propertyName" /> is empty or whitespace. </exception>
    public DiscriminatorAttribute (string propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            throw new ArgumentException(
                "The discriminator property name must not be null, empty, or whitespace.",
                nameof(propertyName));
        }

        PropertyName = propertyName;
    }

    /// <summary> Gets the exact serialized JSON property name. </summary>
    public string PropertyName { get; }
}
