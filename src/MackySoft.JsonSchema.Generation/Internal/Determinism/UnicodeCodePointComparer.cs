namespace MackySoft.JsonSchema.Generation.Internal.Determinism;

internal sealed class UnicodeCodePointComparer : IComparer<string>
{
    private UnicodeCodePointComparer ()
    {
    }

    public static UnicodeCodePointComparer Instance { get; } = new();

    public int Compare (string? x, string? y)
    {
        if (x == null)
        {
            return y == null ? 0 : -1;
        }

        if (y == null)
        {
            return 1;
        }

        ValidateUtf16(x, nameof(x));
        ValidateUtf16(y, nameof(y));

        int xIndex = 0;
        int yIndex = 0;
        while (xIndex < x.Length && yIndex < y.Length)
        {
            int xCodePoint = ReadCodePoint(x, ref xIndex);
            int yCodePoint = ReadCodePoint(y, ref yIndex);
            int codePointComparison = xCodePoint.CompareTo(yCodePoint);
            if (codePointComparison != 0)
            {
                return codePointComparison;
            }
        }

        return string.CompareOrdinal(x, y);
    }

    private static int ReadCodePoint (string value, ref int index)
    {
        char first = value[index++];
        if (!char.IsHighSurrogate(first))
        {
            return first;
        }

        char second = value[index++];
        return char.ConvertToUtf32(first, second);
    }

    private static void ValidateUtf16 (string value, string parameterName)
    {
        for (int index = 0; index < value.Length; index++)
        {
            char character = value[index];
            if (char.IsLowSurrogate(character))
            {
                throw InvalidUnicode(parameterName);
            }

            if (!char.IsHighSurrogate(character))
            {
                continue;
            }

            if (index + 1 >= value.Length || !char.IsLowSurrogate(value[index + 1]))
            {
                throw InvalidUnicode(parameterName);
            }

            index++;
        }
    }

    private static ArgumentException InvalidUnicode (string parameterName)
    {
        return new ArgumentException(
            "The value contains an unpaired UTF-16 surrogate.",
            parameterName);
    }
}
