namespace MackySoft.Text.Vocabularies;

/// <summary> Represents one typed value and canonical text pair in a vocabulary definition. </summary>
/// <typeparam name="TValue"> The typed value carried by the vocabulary. </typeparam>
public sealed class VocabularyEntry<TValue>
{
    internal VocabularyEntry (
        TValue value,
        string text)
    {
        Value = value;
        Text = text;
    }

    /// <summary> Gets the typed value. </summary>
    public TValue Value { get; }

    /// <summary> Gets the canonical text. </summary>
    public string Text { get; }
}
