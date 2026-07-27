namespace MackySoft.JsonSchema.Generation.ContractModel;

/// <summary> Describes one named property in an object contract. </summary>
public sealed class JsonContractProperty
{
    internal JsonContractProperty (
        string name,
        bool isRequired,
        JsonContractNode value,
        JsonContractSource source)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        IsRequired = isRequired;
        Value = value ?? throw new ArgumentNullException(nameof(value));
        Source = source ?? throw new ArgumentNullException(nameof(source));
    }

    /// <summary> Gets the exact serialized JSON property name. </summary>
    public string Name { get; }

    /// <summary> Gets whether the containing object requires the property to be present. </summary>
    public bool IsRequired { get; }

    /// <summary> Gets the property's value contract. </summary>
    public JsonContractNode Value { get; }

    /// <summary> Gets the CLR declaration from which the property was derived. </summary>
    public JsonContractSource Source { get; }
}
