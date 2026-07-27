using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;

namespace MackySoft.JsonSchema.Generation.Internal.Metadata.Validation;

/// <summary>
/// Validates the interoperable ECMA-262 token subset recommended by JSON Schema
/// Draft 2020-12 before a pattern becomes part of the Contract Model.
/// </summary>
internal static class JsonSchemaPatternContract
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

    internal static void Validate (string pattern)
    {
        if (pattern is null)
        {
            throw new ArgumentNullException(nameof(pattern));
        }

        int groupDepth = 0;
        bool canQuantify = false;
        bool canMakeLazy = false;
        for (int index = 0; index < pattern.Length; index++)
        {
            char token = pattern[index];
            if (token == '\\')
            {
                index = ValidateEscape(pattern, index);
                canQuantify = true;
                canMakeLazy = false;
                continue;
            }

            if (token == '[')
            {
                index = ValidateCharacterClass(pattern, index);
                canQuantify = true;
                canMakeLazy = false;
                continue;
            }

            if (token == '(')
            {
                if (index + 1 < pattern.Length
                    && pattern[index + 1] == '?')
                {
                    throw UnsupportedToken(pattern, index);
                }

                groupDepth++;
                canQuantify = false;
                canMakeLazy = false;
                continue;
            }

            if (token == ')')
            {
                if (groupDepth == 0)
                {
                    throw Malformed(pattern, index);
                }

                groupDepth--;
                canQuantify = true;
                canMakeLazy = false;
                continue;
            }

            if (token == '|')
            {
                canQuantify = false;
                canMakeLazy = false;
                continue;
            }

            if (token is '^' or '$')
            {
                canQuantify = false;
                canMakeLazy = false;
                continue;
            }

            if (token is '*' or '+' or '?')
            {
                if (token == '?' && canMakeLazy)
                {
                    canMakeLazy = false;
                    continue;
                }
                if (!canQuantify)
                {
                    throw Malformed(pattern, index);
                }

                canQuantify = false;
                canMakeLazy = true;
                continue;
            }

            if (token == '{')
            {
                if (!canQuantify)
                {
                    throw Malformed(pattern, index);
                }

                index = ValidateRangeQuantifier(pattern, index);
                canQuantify = false;
                canMakeLazy = true;
                continue;
            }

            if (token is ']' or '}'
                || char.IsControl(token)
                || char.IsSurrogate(token))
            {
                throw UnsupportedToken(pattern, index);
            }

            canQuantify = true;
            canMakeLazy = false;
        }

        if (groupDepth != 0)
        {
            throw Malformed(pattern, pattern.Length);
        }

        _ = new Regex(
            pattern,
            RegexOptions.ECMAScript,
            RegexTimeout);
    }

    private static int ValidateCharacterClass (
        string pattern,
        int openingIndex)
    {
        int index = openingIndex + 1;
        if (index < pattern.Length && pattern[index] == '^')
        {
            index++;
        }

        bool hasItem = false;
        for (; index < pattern.Length; index++)
        {
            char token = pattern[index];
            if (token == ']' && hasItem)
            {
                return index;
            }
            if (token == '\\')
            {
                index = ValidateEscape(pattern, index);
                hasItem = true;
                continue;
            }
            if (token == '['
                || char.IsControl(token)
                || char.IsSurrogate(token))
            {
                throw UnsupportedToken(pattern, index);
            }

            hasItem = true;
        }

        throw Malformed(pattern, openingIndex);
    }

    private static int ValidateEscape (
        string pattern,
        int escapeIndex)
    {
        int valueIndex = escapeIndex + 1;
        if (valueIndex >= pattern.Length)
        {
            throw Malformed(pattern, escapeIndex);
        }

        char escaped = pattern[valueIndex];
        if ("^$\\.*+?()[]{}|/-nrtf".IndexOf(escaped) >= 0)
        {
            return valueIndex;
        }
        if (escaped != 'u'
            || valueIndex + 4 >= pattern.Length)
        {
            throw UnsupportedToken(pattern, escapeIndex);
        }

        for (int index = valueIndex + 1; index <= valueIndex + 4; index++)
        {
            if (!Uri.IsHexDigit(pattern[index]))
            {
                throw Malformed(pattern, escapeIndex);
            }
        }

        return valueIndex + 4;
    }

    private static int ValidateRangeQuantifier (
        string pattern,
        int openingIndex)
    {
        int index = openingIndex + 1;
        int minimumStart = index;
        while (index < pattern.Length && IsAsciiDigit(pattern[index]))
        {
            index++;
        }
        if (index == minimumStart)
        {
            throw Malformed(pattern, openingIndex);
        }

        string minimumText = pattern.Substring(
            minimumStart,
            index - minimumStart);
        string? maximumText = null;
        if (index < pattern.Length && pattern[index] == ',')
        {
            index++;
            int maximumStart = index;
            while (index < pattern.Length && IsAsciiDigit(pattern[index]))
            {
                index++;
            }
            if (index != maximumStart)
            {
                maximumText = pattern.Substring(
                    maximumStart,
                    index - maximumStart);
            }
        }

        if (index >= pattern.Length || pattern[index] != '}')
        {
            throw Malformed(pattern, openingIndex);
        }

        if (maximumText is not null)
        {
            BigInteger minimum = BigInteger.Parse(
                minimumText,
                CultureInfo.InvariantCulture);
            BigInteger maximum = BigInteger.Parse(
                maximumText,
                CultureInfo.InvariantCulture);
            if (minimum > maximum)
            {
                throw Malformed(pattern, openingIndex);
            }
        }

        return index;
    }

    private static bool IsAsciiDigit (char value)
    {
        return value is >= '0' and <= '9';
    }

    private static ArgumentException UnsupportedToken (
        string pattern,
        int index)
    {
        return new ArgumentException(
            $"Pattern '{pattern}' uses a token outside the supported JSON Schema ECMA-262 subset at index {index}.",
            nameof(pattern));
    }

    private static ArgumentException Malformed (
        string pattern,
        int index)
    {
        return new ArgumentException(
            $"Pattern '{pattern}' is malformed at index {index}.",
            nameof(pattern));
    }
}
