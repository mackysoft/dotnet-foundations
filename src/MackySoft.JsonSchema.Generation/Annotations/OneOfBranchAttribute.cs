using MackySoft.JsonSchema.Generation.Internal.Common;

namespace MackySoft.JsonSchema.Generation.Annotations;

/// <summary> Declares one named branch of an exclusive JSON contract union. </summary>
[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface,
    AllowMultiple = true,
    Inherited = true)]
public sealed class OneOfBranchAttribute : Attribute
{
    /// <summary> Initializes a branch declaration. </summary>
    /// <param name="name"> The stable non-empty branch name. </param>
    /// <param name="requiredPropertyNames"> JSON property names whose presence is required by the branch. </param>
    /// <exception cref="ArgumentException"> <paramref name="name" /> is empty or whitespace, or a property name is empty or whitespace. </exception>
    /// <exception cref="ArgumentNullException"> <paramref name="requiredPropertyNames" /> is <see langword="null" />. </exception>
    public OneOfBranchAttribute (
        string name,
        params string[] requiredPropertyNames)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "The branch name must not be null, empty, or whitespace.",
                nameof(name));
        }

        if (requiredPropertyNames is null)
        {
            throw new ArgumentNullException(nameof(requiredPropertyNames));
        }

        if (requiredPropertyNames.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException(
                "Required JSON property names must not be null, empty, or whitespace.",
                nameof(requiredPropertyNames));
        }

        Name = name;
        RequiredPropertyNames = JsonContractCollections.Copy(
            requiredPropertyNames,
            nameof(requiredPropertyNames));
    }

    /// <summary> Gets the stable branch name. </summary>
    public string Name { get; }

    /// <summary> Gets the JSON properties whose presence is required by this branch. </summary>
    public IReadOnlyList<string> RequiredPropertyNames { get; }

    /// <summary> Gets or sets JSON text for the discriminator constant associated with this branch. </summary>
    public string? DiscriminatorValueJson { get; set; }

    /// <summary> Gets or sets explanatory text for this branch. </summary>
    public string? Description { get; set; }

    /// <summary> Gets or sets JSON text for one branch-specific example. </summary>
    public string? ExampleJson { get; set; }
}
