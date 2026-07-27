using System.Reflection;
using System.Runtime.ExceptionServices;
using MackySoft.JsonSchema.Generation.Extensibility;
using MackySoft.Text.Vocabularies;

namespace MackySoft.JsonSchema.Generation.Internal.ModelBuilding.TypeMappings;

/// <summary>
/// Resolves the finite strings of a text-vocabulary mapping from the mapped
/// type and its effective serializer converter.
/// </summary>
internal static class TextVocabularyMappingResolver
{
    private static readonly MethodInfo ReadEntriesMethod =
        typeof(TextVocabularyMappingResolver).GetMethod(
            nameof(ReadEntries),
            BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException(
            "The vocabulary entry reader could not be located.");

    internal static IReadOnlyList<string> ReadCanonicalTexts (
        JsonContractTypeMapperContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        IReadOnlyList<UntypedVocabularyEntry> entries =
            GetEntries(context.TypeInfo.Type);
        var serializationContract =
            new TextVocabularySerializationContract(context);
        var texts = new string[entries.Count];
        for (int index = 0; index < entries.Count; index++)
        {
            UntypedVocabularyEntry entry = entries[index];
            serializationContract.EnsureCanonicalRoundTrip(
                entry.Value,
                entry.Text);
            texts[index] = entry.Text;
        }

        return Array.AsReadOnly(texts);
    }

    private static IReadOnlyList<UntypedVocabularyEntry> GetEntries (
        Type vocabularyType)
    {
        try
        {
            return (IReadOnlyList<UntypedVocabularyEntry>)(
                ReadEntriesMethod
                    .MakeGenericMethod(vocabularyType)
                    .Invoke(null, null)
                ?? throw new InvalidOperationException(
                    "Vocabulary enumeration returned no entries."));
        }
        catch (TargetInvocationException exception)
            when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }

    private static IReadOnlyList<UntypedVocabularyEntry> ReadEntries<TValue> ()
        where TValue : struct, Enum
    {
        return Array.AsReadOnly(
            Vocabulary.GetEntries<TValue>()
                .Select(
                    static entry =>
                        new UntypedVocabularyEntry(
                            entry.Value,
                            entry.Text))
                .ToArray());
    }

    private sealed class UntypedVocabularyEntry
    {
        internal UntypedVocabularyEntry (object value, string text)
        {
            Value = value
                ?? throw new ArgumentNullException(nameof(value));
            Text = text
                ?? throw new ArgumentNullException(nameof(text));
        }

        internal object Value { get; }

        internal string Text { get; }
    }
}
