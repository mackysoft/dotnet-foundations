using System.Text.Json;
using MackySoft.JsonSchema.Generation.Internal.Determinism;

namespace MackySoft.JsonSchema.Generation.ContractModel;

/// <summary> Declares product metadata to attach to a completed contract model. </summary>
public sealed class JsonContractModelContribution
{
    /// <summary> Initializes a product metadata declaration without changing the model's JSON structure. </summary>
    /// <param name="target"> The context-scoped model object that receives the metadata. </param>
    /// <param name="name"> The metadata property name. </param>
    /// <param name="value"> The metadata JSON value. </param>
    /// <param name="sourceId"> The stable identifier of the contributing extension. </param>
    /// <exception cref="ArgumentNullException"> <paramref name="target" /> or an input string is <see langword="null" />. </exception>
    /// <exception cref="ArgumentException"> <paramref name="name" /> or <paramref name="sourceId" /> is empty or whitespace. </exception>
    public JsonContractModelContribution (
        JsonContractModelTarget target,
        string name,
        JsonElement value,
        string sourceId)
    {
        Target = target ?? throw new ArgumentNullException(nameof(target));

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "The metadata property name must not be empty or whitespace.",
                nameof(name));
        }

        if (string.IsNullOrWhiteSpace(sourceId))
        {
            throw new ArgumentException(
                "The source identifier must not be empty or whitespace.",
                nameof(sourceId));
        }

        Name = name;
        Value = JsonElementUtility.Clone(value);
        SourceId = sourceId;
    }

    /// <summary> Gets the context-scoped target model object. </summary>
    public JsonContractModelTarget Target { get; }

    /// <summary> Gets the semantic JSON Pointer of the target model object. </summary>
    public string TargetPointer => Target.Pointer;

    /// <summary> Gets the metadata property name. </summary>
    public string Name { get; }

    /// <summary> Gets an independently owned copy of the metadata value. </summary>
    public JsonElement Value { get; }

    /// <summary> Gets the stable identifier of the contributing extension. </summary>
    public string SourceId { get; }
}
