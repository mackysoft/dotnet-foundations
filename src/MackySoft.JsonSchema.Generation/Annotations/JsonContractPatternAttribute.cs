namespace MackySoft.JsonSchema.Generation.Annotations;

/// <summary>
/// Declares a JSON Schema regular-expression pattern from the interoperable
/// ECMA-262 token subset recommended by Draft 2020-12.
/// </summary>
/// <remarks>
/// Contract generation fails when the declared text uses syntax outside the
/// supported subset or is otherwise malformed.
/// </remarks>
[AttributeUsage(
    AttributeTargets.Property
    | AttributeTargets.Field
    | AttributeTargets.Class
    | AttributeTargets.Struct,
    Inherited = true)]
public sealed class JsonContractPatternAttribute : Attribute
{
    /// <summary> Initializes a pattern declaration. </summary>
    /// <param name="pattern">
    /// Pattern text using individual characters, character classes, simple or
    /// range quantifiers, anchors, grouping, alternation, and standard escapes.
    /// </param>
    /// <exception cref="ArgumentNullException"> <paramref name="pattern" /> is <see langword="null" />. </exception>
    public JsonContractPatternAttribute (string pattern)
    {
        Pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
    }

    /// <summary> Gets the declared JSON Schema pattern text. </summary>
    public string Pattern { get; }
}
