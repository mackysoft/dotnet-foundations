using MackySoft.JsonSchema.Generation.Internal.Determinism;

namespace MackySoft.JsonSchema.Generation.Internal.Metadata.Contracts;

internal static class MetadataTextContract
{
    internal static void Validate (
        string value,
        MetadataResolutionTarget target,
        string sourceId,
        string valueName,
        bool rejectWhitespaceOnly = true)
    {
        if (value.Length == 0
            || (rejectWhitespaceOnly && string.IsNullOrWhiteSpace(value)))
        {
            throw MetadataFailure.Invalid(
                target,
                new[] { sourceId },
                rejectWhitespaceOnly
                    ? $"{valueName} must not be null, empty, or whitespace."
                    : $"{valueName} must not be empty.");
        }

        try
        {
            _ = UnicodeCodePointComparer.Instance.Compare(value, value);
        }
        catch (ArgumentException exception)
        {
            throw MetadataFailure.Invalid(
                target,
                new[] { sourceId },
                $"{valueName} contains invalid Unicode.",
                exception);
        }
    }
}
