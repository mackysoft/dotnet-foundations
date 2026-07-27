using System.Text.Json;
using MackySoft.JsonSchema.Generation.ContractModel;
using MackySoft.JsonSchema.Generation.Internal.Common;

namespace MackySoft.JsonSchema.Generation.Internal.Metadata.Contracts;

internal sealed class ResolvedOneOfBranch
{
    internal ResolvedOneOfBranch (
        string name,
        IEnumerable<string> requiredProperties,
        JsonElement? discriminatorValue,
        JsonContractAnnotations annotations)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        RequiredProperties = JsonContractCollections.Copy(
            requiredProperties,
            nameof(requiredProperties));
        DiscriminatorValue = JsonContractCollections.CloneNullableJsonElement(
            discriminatorValue);
        Annotations = annotations ?? throw new ArgumentNullException(nameof(annotations));
    }

    public string Name { get; }

    public IReadOnlyList<string> RequiredProperties { get; }

    public JsonElement? DiscriminatorValue { get; }

    public JsonContractAnnotations Annotations { get; }
}
