using System.Text.Json;
using MackySoft.JsonSchema.Generation.Internal.Common;

namespace MackySoft.JsonSchema.Generation.ContractModel;

/// <summary> Carries normalized validation constraints for a contract node. </summary>
public sealed class JsonContractConstraints
{
    internal JsonContractConstraints (
        JsonElement? minimum,
        JsonElement? exclusiveMinimum,
        JsonElement? maximum,
        JsonElement? exclusiveMaximum,
        int? minimumLength,
        int? maximumLength,
        int? minimumItems,
        int? maximumItems,
        int? minimumProperties,
        int? maximumProperties,
        string? pattern,
        string? format)
    {
        Minimum = JsonContractCollections.CloneNullableJsonElement(minimum);
        ExclusiveMinimum = JsonContractCollections.CloneNullableJsonElement(exclusiveMinimum);
        Maximum = JsonContractCollections.CloneNullableJsonElement(maximum);
        ExclusiveMaximum = JsonContractCollections.CloneNullableJsonElement(exclusiveMaximum);
        MinimumLength = minimumLength;
        MaximumLength = maximumLength;
        MinimumItems = minimumItems;
        MaximumItems = maximumItems;
        MinimumProperties = minimumProperties;
        MaximumProperties = maximumProperties;
        Pattern = pattern;
        Format = format;
    }

    /// <summary> Gets the inclusive numeric lower bound. </summary>
    public JsonElement? Minimum { get; }

    /// <summary> Gets the exclusive numeric lower bound. </summary>
    public JsonElement? ExclusiveMinimum { get; }

    /// <summary> Gets the inclusive numeric upper bound. </summary>
    public JsonElement? Maximum { get; }

    /// <summary> Gets the exclusive numeric upper bound. </summary>
    public JsonElement? ExclusiveMaximum { get; }

    /// <summary> Gets the minimum string length. </summary>
    public int? MinimumLength { get; }

    /// <summary> Gets the maximum string length. </summary>
    public int? MaximumLength { get; }

    /// <summary> Gets the minimum array item count. </summary>
    public int? MinimumItems { get; }

    /// <summary> Gets the maximum array item count. </summary>
    public int? MaximumItems { get; }

    /// <summary> Gets the minimum object property count. </summary>
    public int? MinimumProperties { get; }

    /// <summary> Gets the maximum object property count. </summary>
    public int? MaximumProperties { get; }

    /// <summary> Gets the JSON Schema regular-expression pattern. </summary>
    public string? Pattern { get; }

    /// <summary> Gets the semantic string format annotation. </summary>
    public string? Format { get; }
}
