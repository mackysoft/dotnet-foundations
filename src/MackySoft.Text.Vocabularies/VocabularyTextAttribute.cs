namespace MackySoft.Text.Vocabularies;

/// <summary> Declares the canonical text mapped to one vocabulary member. </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class VocabularyTextAttribute : Attribute
{
    /// <summary> Initializes a new instance of the <see cref="VocabularyTextAttribute" /> class. </summary>
    /// <param name="text"> The canonical text. </param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="text" /> is empty, contains only whitespace, or has leading or trailing whitespace.
    /// </exception>
    /// <exception cref="ArgumentNullException"> Thrown when <paramref name="text" /> is <see langword="null" />. </exception>
    public VocabularyTextAttribute (string text)
    {
        if (text is null)
        {
            throw new ArgumentNullException(nameof(text));
        }
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Vocabulary text must not be empty or whitespace.", nameof(text));
        }
        if (HasOuterWhitespace(text))
        {
            throw new ArgumentException("Vocabulary text must not have leading or trailing whitespace.", nameof(text));
        }

        Text = text;
    }

    /// <summary> Gets the canonical text. </summary>
    public string Text { get; }

    private static bool HasOuterWhitespace (string text)
    {
        return char.IsWhiteSpace(text[0]) || char.IsWhiteSpace(text[text.Length - 1]);
    }
}
