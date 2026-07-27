namespace MackySoft.JsonSchema.Generation.ContractModel;

/// <summary> Associates a reusable contract node with its stable definition identifier. </summary>
public sealed class JsonContractDefinition
{
    internal JsonContractDefinition (
        string id,
        JsonContractNode value,
        JsonContractSource source)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Value = value ?? throw new ArgumentNullException(nameof(value));
        Source = source ?? throw new ArgumentNullException(nameof(source));
    }

    /// <summary> Gets the identifier referenced by contract reference nodes. </summary>
    public string Id { get; }

    /// <summary> Gets the reusable value contract. </summary>
    public JsonContractNode Value { get; }

    /// <summary> Gets the CLR declaration from which the definition was derived. </summary>
    public JsonContractSource Source { get; }
}
