using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace MackySoft.Text.Vocabularies;

/// <summary> Validates and resolves finite mappings between typed values and canonical texts. </summary>
/// <remarks>
/// Resolution uses ordinal text comparison without trimming, case folding, aliases, or Unicode normalization.
/// The current definition carrier is an enum marked with <see cref="VocabularyDefinitionAttribute" />.
/// </remarks>
public static class Vocabulary
{
    private static readonly MethodInfo GetEntriesMethod = typeof(Vocabulary)
        .GetMethod(nameof(GetEntries), BindingFlags.Public | BindingFlags.Static)!;

    /// <summary> Gets the canonical text mapped to a declared value. </summary>
    /// <typeparam name="TValue"> The vocabulary value type. </typeparam>
    /// <param name="value"> The declared value. </param>
    /// <returns> The canonical text. </returns>
    /// <exception cref="ArgumentOutOfRangeException"> Thrown when <paramref name="value" /> is not declared. </exception>
    /// <exception cref="InvalidOperationException"> Thrown when the vocabulary definition is invalid. </exception>
    public static string GetText<TValue> (TValue value)
        where TValue : struct, Enum
    {
        if (TryGetText(value, out var text))
        {
            return text;
        }

        throw new ArgumentOutOfRangeException(
            nameof(value),
            value,
            $"Value is not declared by vocabulary '{typeof(TValue).FullName}'.");
    }

    /// <summary> Tries to get the canonical text mapped to a declared value. </summary>
    /// <typeparam name="TValue"> The vocabulary value type. </typeparam>
    /// <param name="value"> The value to resolve. </param>
    /// <param name="text"> The canonical text when resolution succeeds; otherwise <see langword="null" />. </param>
    /// <returns> <see langword="true" /> when the value is declared; otherwise <see langword="false" />. </returns>
    /// <exception cref="InvalidOperationException"> Thrown when the vocabulary definition is invalid. </exception>
    public static bool TryGetText<TValue> (
        TValue value,
        [NotNullWhen(true)]
        out string? text)
        where TValue : struct, Enum
    {
        return Cache<TValue>.Table.TryGetText(value, out text);
    }

    /// <summary> Gets the typed value mapped to canonical text. </summary>
    /// <typeparam name="TValue"> The vocabulary value type. </typeparam>
    /// <param name="text"> The canonical text to resolve. </param>
    /// <returns> The mapped typed value. </returns>
    /// <exception cref="ArgumentException"> Thrown when <paramref name="text" /> is not declared. </exception>
    /// <exception cref="ArgumentNullException"> Thrown when <paramref name="text" /> is <see langword="null" />. </exception>
    /// <exception cref="InvalidOperationException"> Thrown when the vocabulary definition is invalid. </exception>
    public static TValue GetValue<TValue> (string text)
        where TValue : struct, Enum
    {
        if (text is null)
        {
            throw new ArgumentNullException(nameof(text));
        }
        if (TryGetValue(text, out TValue value))
        {
            return value;
        }

        throw new ArgumentException(
            $"Text '{text}' is not declared by vocabulary '{typeof(TValue).FullName}'.",
            nameof(text));
    }

    /// <summary> Tries to get the typed value mapped to canonical text. </summary>
    /// <typeparam name="TValue"> The vocabulary value type. </typeparam>
    /// <param name="text"> The canonical text to resolve. </param>
    /// <param name="value"> The mapped value when resolution succeeds; otherwise the default value. </param>
    /// <returns> <see langword="true" /> when the text is declared; otherwise <see langword="false" />. </returns>
    /// <exception cref="InvalidOperationException"> Thrown when the vocabulary definition is invalid. </exception>
    public static bool TryGetValue<TValue> (
        string? text,
        out TValue value)
        where TValue : struct, Enum
    {
        return Cache<TValue>.Table.TryGetValue(text, out value);
    }

    /// <summary> Determines whether canonical text is declared by a vocabulary definition. </summary>
    /// <typeparam name="TValue"> The vocabulary value type. </typeparam>
    /// <param name="text"> The canonical text to inspect. </param>
    /// <returns> <see langword="true" /> when the text is declared; otherwise <see langword="false" />. </returns>
    /// <exception cref="InvalidOperationException"> Thrown when the vocabulary definition is invalid. </exception>
    public static bool IsDefined<TValue> (string? text)
        where TValue : struct, Enum
    {
        return text is not null && Cache<TValue>.Table.ContainsText(text);
    }

    /// <summary> Determines whether a typed value is declared by a vocabulary definition. </summary>
    /// <typeparam name="TValue"> The vocabulary value type. </typeparam>
    /// <param name="value"> The typed value to inspect. </param>
    /// <returns> <see langword="true" /> when the value is declared; otherwise <see langword="false" />. </returns>
    /// <exception cref="InvalidOperationException"> Thrown when the vocabulary definition is invalid. </exception>
    public static bool IsDefined<TValue> (TValue value)
        where TValue : struct, Enum
    {
        return Cache<TValue>.Table.ContainsValue(value);
    }

    /// <summary> Determines whether canonical text maps to an expected typed value. </summary>
    /// <typeparam name="TValue"> The vocabulary value type. </typeparam>
    /// <param name="text"> The canonical text to resolve. </param>
    /// <param name="value"> The expected typed value. </param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="text" /> maps to <paramref name="value" />;
    /// otherwise <see langword="false" />.
    /// </returns>
    /// <exception cref="InvalidOperationException"> Thrown when the vocabulary definition is invalid. </exception>
    public static bool Matches<TValue> (
        string? text,
        TValue value)
        where TValue : struct, Enum
    {
        return TryGetValue(text, out TValue parsedValue)
            && EqualityComparer<TValue>.Default.Equals(parsedValue, value);
    }

    /// <summary> Gets all typed value and canonical text pairs in declaration order. </summary>
    /// <typeparam name="TValue"> The vocabulary value type. </typeparam>
    /// <returns> A read-only list of vocabulary entries. </returns>
    /// <exception cref="InvalidOperationException"> Thrown when the vocabulary definition is invalid. </exception>
    public static IReadOnlyList<VocabularyEntry<TValue>> GetEntries<TValue> ()
        where TValue : struct, Enum
    {
        return Cache<TValue>.Table.Entries;
    }

    /// <summary> Gets all canonical texts in declaration order. </summary>
    /// <typeparam name="TValue"> The vocabulary value type. </typeparam>
    /// <returns> A read-only list of canonical texts. </returns>
    /// <exception cref="InvalidOperationException"> Thrown when the vocabulary definition is invalid. </exception>
    public static IReadOnlyList<string> GetTexts<TValue> ()
        where TValue : struct, Enum
    {
        return Cache<TValue>.Table.Texts;
    }

    /// <summary> Determines whether a runtime type declares a valid vocabulary definition. </summary>
    /// <param name="valueType"> The runtime type to inspect. </param>
    /// <returns>
    /// <see langword="true" /> when the type declares a valid vocabulary definition; otherwise <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"> Thrown when <paramref name="valueType" /> is <see langword="null" />. </exception>
    /// <exception cref="InvalidOperationException"> Thrown when the type declares an invalid vocabulary definition. </exception>
    public static bool IsVocabulary (Type valueType)
    {
        if (valueType is null)
        {
            throw new ArgumentNullException(nameof(valueType));
        }
        if (!valueType.IsEnum || !HasDefinitionAttribute(valueType))
        {
            return false;
        }

        Validate(valueType);
        return true;
    }

    /// <summary> Validates a complete vocabulary definition known only at runtime. </summary>
    /// <param name="valueType"> The runtime value type to validate. </param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="valueType" /> is not an enum or does not declare a vocabulary definition.
    /// </exception>
    /// <exception cref="ArgumentNullException"> Thrown when <paramref name="valueType" /> is <see langword="null" />. </exception>
    /// <exception cref="InvalidOperationException"> Thrown when the vocabulary definition is invalid. </exception>
    public static void Validate (Type valueType)
    {
        if (valueType is null)
        {
            throw new ArgumentNullException(nameof(valueType));
        }
        if (!valueType.IsEnum)
        {
            throw new ArgumentException($"Type '{valueType.FullName}' is not an enum.", nameof(valueType));
        }
        if (!HasDefinitionAttribute(valueType))
        {
            throw new ArgumentException(
                $"Type '{valueType.FullName}' does not declare {nameof(VocabularyDefinitionAttribute)}.",
                nameof(valueType));
        }

        try
        {
            _ = GetEntriesMethod.MakeGenericMethod(valueType).Invoke(null, null);
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }

    private static bool HasDefinitionAttribute (MemberInfo member)
    {
        return CustomAttributeData
            .GetCustomAttributes(member)
            .Any(static attribute => attribute.AttributeType == typeof(VocabularyDefinitionAttribute));
    }

    private static class Cache<TValue>
        where TValue : struct, Enum
    {
        private static readonly Lazy<Table<TValue>> TableSource = new(Build);

        public static Table<TValue> Table => TableSource.Value;

        private static Table<TValue> Build ()
        {
            var valueType = typeof(TValue);
            if (!HasDefinitionAttribute(valueType))
            {
                throw new InvalidOperationException(
                    $"Type '{valueType.FullName}' does not declare {nameof(VocabularyDefinitionAttribute)}.");
            }

            var fields = valueType.GetFields(BindingFlags.Public | BindingFlags.Static);
            Array.Sort(fields, static (left, right) => left.MetadataToken.CompareTo(right.MetadataToken));

            if (fields.Length == 0)
            {
                throw new InvalidOperationException(
                    $"Vocabulary '{valueType.FullName}' does not declare any values.");
            }

            var valueToText = new Dictionary<TValue, string>();
            var textToValue = new Dictionary<string, TValue>(StringComparer.Ordinal);
            var entries = new List<VocabularyEntry<TValue>>(fields.Length);
            var texts = new List<string>(fields.Length);

            foreach (var field in fields)
            {
                var value = (TValue)field.GetValue(null)!;
                if (valueToText.ContainsKey(value))
                {
                    throw new InvalidOperationException(
                        $"Vocabulary '{valueType.FullName}' declares duplicate value '{value}'.");
                }

                var text = GetDeclaredText(valueType, field);
                if (textToValue.ContainsKey(text))
                {
                    throw new InvalidOperationException(
                        $"Vocabulary '{valueType.FullName}' declares duplicate text '{text}'.");
                }

                valueToText.Add(value, text);
                textToValue.Add(text, value);
                entries.Add(new VocabularyEntry<TValue>(value, text));
                texts.Add(text);
            }

            return new Table<TValue>(
                valueToText,
                textToValue,
                entries.AsReadOnly(),
                texts.AsReadOnly());
        }

        private static string GetDeclaredText (
            Type valueType,
            FieldInfo field)
        {
            var textAttributes = CustomAttributeData
                .GetCustomAttributes(field)
                .Where(static attribute => attribute.AttributeType == typeof(VocabularyTextAttribute))
                .ToArray();
            if (textAttributes.Length != 1)
            {
                throw new InvalidOperationException(
                    $"Vocabulary member '{valueType.FullName}.{field.Name}' must declare exactly one {nameof(VocabularyTextAttribute)}.");
            }

            var constructorArguments = textAttributes[0].ConstructorArguments;
            var text = constructorArguments.Count == 1
                ? constructorArguments[0].Value as string
                : null;
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new InvalidOperationException(
                    $"Vocabulary member '{valueType.FullName}.{field.Name}' declares empty or whitespace text.");
            }
            if (HasOuterWhitespace(text))
            {
                throw new InvalidOperationException(
                    $"Vocabulary member '{valueType.FullName}.{field.Name}' declares text with leading or trailing whitespace.");
            }

            return text;
        }

        private static bool HasOuterWhitespace (string text)
        {
            return char.IsWhiteSpace(text[0]) || char.IsWhiteSpace(text[text.Length - 1]);
        }
    }

    private sealed class Table<TValue>
        where TValue : struct, Enum
    {
        private readonly Dictionary<string, TValue> textToValue;
        private readonly Dictionary<TValue, string> valueToText;

        public Table (
            Dictionary<TValue, string> valueToText,
            Dictionary<string, TValue> textToValue,
            IReadOnlyList<VocabularyEntry<TValue>> entries,
            IReadOnlyList<string> texts)
        {
            this.valueToText = valueToText;
            this.textToValue = textToValue;
            Entries = entries;
            Texts = texts;
        }

        public IReadOnlyList<VocabularyEntry<TValue>> Entries { get; }

        public IReadOnlyList<string> Texts { get; }

        public bool TryGetText (
            TValue value,
            [NotNullWhen(true)]
            out string? text)
        {
            return valueToText.TryGetValue(value, out text);
        }

        public bool TryGetValue (
            string? text,
            out TValue value)
        {
            if (text is null)
            {
                value = default;
                return false;
            }

            return textToValue.TryGetValue(text, out value);
        }

        public bool ContainsText (string text)
        {
            return textToValue.ContainsKey(text);
        }

        public bool ContainsValue (TValue value)
        {
            return valueToText.ContainsKey(value);
        }
    }
}
