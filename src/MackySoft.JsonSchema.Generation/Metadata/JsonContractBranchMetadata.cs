using System.Text.Json;
using MackySoft.JsonSchema.Generation.Internal.Common;

namespace MackySoft.JsonSchema.Generation.Metadata;

/// <summary>
/// Declares one product-independent oneOf branch supplied by a metadata provider.
/// </summary>
public sealed class JsonContractBranchMetadata
{
    /// <summary> Initializes one branch declaration. </summary>
    /// <param name="name"> The stable branch name. </param>
    /// <param name="requiredPropertyNames"> JSON property names required by this branch. </param>
    /// <param name="discriminatorValue"> The optional discriminator constant. </param>
    /// <param name="description"> Optional explanatory text. </param>
    /// <param name="examples"> Optional branch examples. </param>
    /// <exception cref="ArgumentException"> A required text value is empty or whitespace. </exception>
    /// <exception cref="ArgumentNullException"> A required argument is <see langword="null" />. </exception>
    public JsonContractBranchMetadata (
        string name,
        IEnumerable<string> requiredPropertyNames,
        JsonElement? discriminatorValue = null,
        string? description = null,
        IEnumerable<JsonElement>? examples = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "A branch name must not be null, empty, or whitespace.",
                nameof(name));
        }

        if (requiredPropertyNames is null)
        {
            throw new ArgumentNullException(nameof(requiredPropertyNames));
        }

        string[] properties = requiredPropertyNames.ToArray();
        if (properties.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException(
                "Required JSON property names must not be null, empty, or whitespace.",
                nameof(requiredPropertyNames));
        }

        if (description is not null && string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException(
                "A branch description must not be empty or whitespace.",
                nameof(description));
        }

        Name = name;
        RequiredPropertyNames = JsonContractCollections.Copy(
            properties,
            nameof(requiredPropertyNames));
        DiscriminatorValue =
            JsonContractCollections.CloneNullableJsonElement(
                discriminatorValue);
        Description = description;
        Examples = examples is null
            ? Array.AsReadOnly(Array.Empty<JsonElement>())
            : JsonContractCollections.CloneJsonElements(
                examples,
                nameof(examples));
    }

    /// <summary> Gets the stable branch name. </summary>
    public string Name { get; }

    /// <summary> Gets JSON property names required by this branch. </summary>
    public IReadOnlyList<string> RequiredPropertyNames { get; }

    /// <summary> Gets the optional discriminator constant. </summary>
    public JsonElement? DiscriminatorValue { get; }

    /// <summary> Gets optional explanatory text. </summary>
    public string? Description { get; }

    /// <summary> Gets branch examples. </summary>
    public IReadOnlyList<JsonElement> Examples { get; }
}
