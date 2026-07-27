namespace MackySoft.JsonSchema.Generation.ContractModel;

/// <summary> Identifies the JSON property that selects a tagged-union branch. </summary>
public sealed class JsonContractDiscriminator
{
    internal JsonContractDiscriminator (string propertyName)
    {
        PropertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
    }

    /// <summary> Gets the exact serialized JSON property name used as the discriminator. </summary>
    public string PropertyName { get; }
}
