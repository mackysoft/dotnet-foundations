namespace MackySoft.JsonSchema.Generation.Annotations;

/// <summary> Restricts a contract value to one declared JSON constant. </summary>
[AttributeUsage(
    AttributeTargets.Class
    | AttributeTargets.Struct
    | AttributeTargets.Enum
    | AttributeTargets.Interface
    | AttributeTargets.Property
    | AttributeTargets.Field,
    Inherited = true)]
public sealed class JsonContractConstAttribute : Attribute
{
    /// <summary> Initializes a constant declaration. </summary>
    /// <param name="json"> JSON text parsed by the contract generator. </param>
    /// <exception cref="ArgumentNullException"> <paramref name="json" /> is <see langword="null" />. </exception>
    public JsonContractConstAttribute (string json)
    {
        Json = json ?? throw new ArgumentNullException(nameof(json));
    }

    /// <summary> Gets the JSON text supplied to the declaration. </summary>
    public string Json { get; }
}
