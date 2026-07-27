using System.Globalization;
using System.Numerics;

namespace MackySoft.JsonSchema.Generation.Internal.Determinism;

/// <summary>
/// Represents the exact mathematical value of valid JSON number text as a
/// normalized decimal coefficient and an arbitrary-precision base-ten exponent.
/// </summary>
internal readonly struct ExactJsonNumber
{
    private const int MaximumExpandedIntegerZeros = 32;

    private const int MaximumEmbeddedFractionDigits = 32;

    private const int MaximumLeadingFractionZeros = 6;

    private ExactJsonNumber (
        bool isNegative,
        string digits,
        BigInteger exponent)
    {
        IsNegative = isNegative;
        Digits = digits;
        Exponent = exponent;
    }

    private static ExactJsonNumber Zero { get; } =
        new(false, "0", BigInteger.Zero);

    private bool IsNegative { get; }

    private bool IsZero => Digits == "0";

    private string Digits { get; }

    private BigInteger Exponent { get; }

    internal static ExactJsonNumber Parse (string jsonNumber)
    {
        int significandStart = jsonNumber[0] == '-' ? 1 : 0;
        int exponentMarker = FindExponentMarker(
            jsonNumber,
            significandStart);
        int significandEnd = exponentMarker < 0
            ? jsonNumber.Length
            : exponentMarker;
        string digits = ReadCoefficientDigits(
            jsonNumber,
            significandStart,
            significandEnd,
            out int fractionalDigits);
        BigInteger exponent = ReadExponent(jsonNumber, exponentMarker)
            - fractionalDigits;

        return Normalize(
            significandStart != 0,
            digits,
            exponent);
    }

    internal int CompareTo (ExactJsonNumber other)
    {
        if (IsZero || other.IsZero)
        {
            if (IsZero && other.IsZero)
            {
                return 0;
            }

            return IsZero
                ? other.IsNegative ? 1 : -1
                : IsNegative ? -1 : 1;
        }

        if (IsNegative != other.IsNegative)
        {
            return IsNegative ? -1 : 1;
        }

        int magnitudeComparison = CompareMagnitude(other);
        return IsNegative
            ? -magnitudeComparison
            : magnitudeComparison;
    }

    internal string ToCanonicalText ()
    {
        if (IsZero)
        {
            return "0";
        }

        return string.Concat(
            IsNegative ? "-" : string.Empty,
            Digits,
            "e",
            Exponent.ToString(CultureInfo.InvariantCulture));
    }

    internal string ToJsonText ()
    {
        if (IsZero)
        {
            return "0";
        }

        if (CanUseExpandedInteger())
        {
            return FormatExpandedInteger();
        }

        BigInteger decimalPoint = Digits.Length + Exponent;
        if (CanEmbedDecimalPoint(decimalPoint))
        {
            return FormatWithEmbeddedDecimalPoint(decimalPoint);
        }

        if (CanUseLeadingFraction(decimalPoint))
        {
            return FormatLeadingFraction(decimalPoint);
        }

        return FormatScientific();
    }

    private bool CanUseExpandedInteger ()
    {
        return Exponent >= BigInteger.Zero
            && Exponent <= MaximumExpandedIntegerZeros;
    }

    private bool CanEmbedDecimalPoint (BigInteger decimalPoint)
    {
        return Exponent < BigInteger.Zero
            && Exponent >= -MaximumEmbeddedFractionDigits
            && decimalPoint > BigInteger.Zero;
    }

    private static bool CanUseLeadingFraction (
        BigInteger decimalPoint)
    {
        return decimalPoint <= BigInteger.Zero
            && decimalPoint >= -MaximumLeadingFractionZeros;
    }

    private string FormatExpandedInteger ()
    {
        return Sign()
            + Digits
            + new string('0', (int)Exponent);
    }

    private string FormatWithEmbeddedDecimalPoint (
        BigInteger decimalPoint)
    {
        int point = (int)decimalPoint;
        return Sign()
            + Digits.Substring(0, point)
            + "."
            + Digits.Substring(point);
    }

    private string FormatLeadingFraction (BigInteger decimalPoint)
    {
        return Sign()
            + "0."
            + new string('0', (int)-decimalPoint)
            + Digits;
    }

    private string FormatScientific ()
    {
        BigInteger scientificExponent = Exponent + Digits.Length - 1;
        string significand = Digits.Length == 1
            ? Digits
            : Digits.Substring(0, 1)
                + "."
                + Digits.Substring(1);
        return Sign()
            + significand
            + "e"
            + scientificExponent.ToString(CultureInfo.InvariantCulture);
    }

    private string Sign ()
    {
        return IsNegative ? "-" : string.Empty;
    }

    private static int FindExponentMarker (
        string jsonNumber,
        int startIndex)
    {
        int lowercaseMarker = jsonNumber.IndexOf('e', startIndex);
        return lowercaseMarker >= 0
            ? lowercaseMarker
            : jsonNumber.IndexOf('E', startIndex);
    }

    private static string ReadCoefficientDigits (
        string jsonNumber,
        int startIndex,
        int endIndex,
        out int fractionalDigits)
    {
        int decimalPoint = jsonNumber.IndexOf(
            '.',
            startIndex,
            endIndex - startIndex);
        if (decimalPoint < 0)
        {
            fractionalDigits = 0;
            return jsonNumber.Substring(
                startIndex,
                endIndex - startIndex);
        }

        fractionalDigits = endIndex - decimalPoint - 1;
        return jsonNumber.Substring(
                startIndex,
                decimalPoint - startIndex)
            + jsonNumber.Substring(decimalPoint + 1, fractionalDigits);
    }

    private static BigInteger ReadExponent (
        string jsonNumber,
        int exponentMarker)
    {
        return exponentMarker < 0
            ? BigInteger.Zero
            : BigInteger.Parse(
                jsonNumber.Substring(exponentMarker + 1),
                NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture);
    }

    private static ExactJsonNumber Normalize (
        bool isNegative,
        string digits,
        BigInteger exponent)
    {
        digits = digits.TrimStart('0');
        if (digits.Length == 0)
        {
            return Zero;
        }

        int trailingZeros = CountTrailingZeros(digits);
        if (trailingZeros != 0)
        {
            digits = digits.Substring(0, digits.Length - trailingZeros);
            exponent += trailingZeros;
        }

        return new ExactJsonNumber(isNegative, digits, exponent);
    }

    private static int CountTrailingZeros (string digits)
    {
        int trailingZeros = 0;
        for (int index = digits.Length - 1;
            index >= 0 && digits[index] == '0';
            index--)
        {
            trailingZeros++;
        }

        return trailingZeros;
    }

    private int CompareMagnitude (ExactJsonNumber other)
    {
        BigInteger magnitude = Exponent + Digits.Length;
        BigInteger otherMagnitude = other.Exponent + other.Digits.Length;
        int magnitudeComparison = magnitude.CompareTo(otherMagnitude);
        if (magnitudeComparison != 0)
        {
            return magnitudeComparison;
        }

        int length = Math.Max(Digits.Length, other.Digits.Length);
        for (int index = 0; index < length; index++)
        {
            char digit = index < Digits.Length ? Digits[index] : '0';
            char otherDigit = index < other.Digits.Length
                ? other.Digits[index]
                : '0';
            int digitComparison = digit.CompareTo(otherDigit);
            if (digitComparison != 0)
            {
                return digitComparison;
            }
        }

        return 0;
    }
}
