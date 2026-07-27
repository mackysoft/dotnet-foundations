namespace MackySoft.JsonSchema.Generation.Annotations;

/// <summary> Declares one JSON example for a contract type or member. </summary>
[AttributeUsage(
    AttributeTargets.Class
    | AttributeTargets.Struct
    | AttributeTargets.Enum
    | AttributeTargets.Interface
    | AttributeTargets.Property
    | AttributeTargets.Field,
    AllowMultiple = true,
    Inherited = true)]
public sealed class JsonContractExampleAttribute : Attribute
{
    /// <summary> Initializes a JSON example declaration. </summary>
    /// <param name="json"> JSON text parsed by the contract generator. </param>
    /// <exception cref="ArgumentNullException"> <paramref name="json" /> is <see langword="null" />. </exception>
    public JsonContractExampleAttribute (string json)
    {
        Json = json ?? throw new ArgumentNullException(nameof(json));
    }

    /// <summary> Gets the JSON text supplied to the declaration. </summary>
    public string Json { get; }
}
