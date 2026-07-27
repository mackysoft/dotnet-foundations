using System.Text.Json;
using MackySoft.JsonSchema.Generation.Internal.Common;

namespace MackySoft.JsonSchema.Generation.ContractModel;

/// <summary> Describes one exclusive alternative in a <c>oneOf</c> contract. </summary>
public sealed class JsonContractVariant
{
    internal JsonContractVariant (
        string name,
        JsonContractNode? value,
        IEnumerable<string> requiredProperties,
        JsonElement? discriminatorValue,
        JsonContractAnnotations annotations)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Value = value;
        RequiredProperties = JsonContractCollections.Copy(
            requiredProperties,
            nameof(requiredProperties));
        DiscriminatorValue = JsonContractCollections.CloneNullableJsonElement(discriminatorValue);
        Annotations = annotations ?? throw new ArgumentNullException(nameof(annotations));
    }

    /// <summary> Gets the stable branch name used by contract metadata consumers. </summary>
    public string Name { get; }

    /// <summary> Gets the branch value contract, or <see langword="null" /> for a property-requirement branch. </summary>
    public JsonContractNode? Value { get; }

    /// <summary> Gets the JSON properties whose presence selects this branch. </summary>
    public IReadOnlyList<string> RequiredProperties { get; }

    /// <summary> Gets the discriminator constant associated with this branch. </summary>
    public JsonElement? DiscriminatorValue { get; }

    /// <summary> Gets descriptive metadata for this branch. </summary>
    public JsonContractAnnotations Annotations { get; }
}
