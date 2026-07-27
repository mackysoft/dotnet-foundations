using System.Text.Json;
using MackySoft.JsonSchema.Generation.Internal.Determinism;

namespace MackySoft.JsonSchema.Generation.ContractModel;

/// <summary> Describes one exclusive alternative in a <c>oneOf</c> contract. </summary>
public sealed class JsonContractVariant
{
    internal JsonContractVariant (
        string name,
        JsonContractNode value,
        JsonElement discriminatorValue)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Value = value ?? throw new ArgumentNullException(nameof(value));
        DiscriminatorValue = JsonElementUtility.Clone(discriminatorValue);
    }

    /// <summary> Gets the stable branch name used by contract metadata consumers. </summary>
    public string Name { get; }

    /// <summary> Gets the branch value contract. </summary>
    public JsonContractNode Value { get; }

    /// <summary> Gets the discriminator constant associated with this branch. </summary>
    public JsonElement DiscriminatorValue { get; }
}
