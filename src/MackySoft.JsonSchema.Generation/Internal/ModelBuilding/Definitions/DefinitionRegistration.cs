using System.Text.Json;
using MackySoft.JsonSchema.Generation.ContractModel;

namespace MackySoft.JsonSchema.Generation.Internal.ModelBuilding.Definitions;

/// <summary>
/// Tracks a definition from first graph encounter through completed model node.
/// </summary>
internal sealed class DefinitionRegistration
{
    internal DefinitionRegistration (
        DefinitionKey key,
        string id,
        int ordinal,
        JsonElement? discriminatorValue)
    {
        Key = key;
        Id = id;
        Ordinal = ordinal;
        DiscriminatorValue = discriminatorValue?.Clone();
    }

    internal DefinitionKey Key { get; }

    internal string Id { get; }

    internal int Ordinal { get; }

    internal JsonElement? DiscriminatorValue { get; }

    internal JsonContractNode? Value { get; private set; }

    internal void Complete (JsonContractNode value)
    {
        if (Value is not null)
        {
            throw new InvalidOperationException(
                $"Definition '{Id}' was completed more than once.");
        }

        Value = value
            ?? throw new ArgumentNullException(nameof(value));
    }
}
