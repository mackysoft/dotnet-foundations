using System.Text.Json;
using MackySoft.JsonSchema.Generation.Internal.Common;

namespace MackySoft.JsonSchema.Generation.ContractModel;

/// <summary> Carries descriptive metadata shared by contract projections. </summary>
public sealed class JsonContractAnnotations
{
    internal JsonContractAnnotations (
        string? title,
        string? description,
        IEnumerable<JsonElement> examples)
    {
        Title = title;
        Description = description;
        Examples = JsonContractCollections.CloneJsonElements(examples, nameof(examples));
    }

    /// <summary> Gets the display title, or <see langword="null" /> when none was declared. </summary>
    public string? Title { get; }

    /// <summary> Gets the explanatory text, or <see langword="null" /> when none was declared. </summary>
    public string? Description { get; }

    /// <summary> Gets independent JSON example values in deterministic order. </summary>
    public IReadOnlyList<JsonElement> Examples { get; }
}
