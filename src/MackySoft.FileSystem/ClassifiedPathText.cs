using System.Diagnostics.CodeAnalysis;

using MackySoft.FileSystem.Internal;

namespace MackySoft.FileSystem;

internal sealed class ClassifiedPathText
{
    private ClassifiedPathText (
        string value,
        ClassifiedPathKind kind)
    {
        Value = value;
        Kind = kind;
    }

    public string Value { get; }

    public ClassifiedPathKind Kind { get; }

    public static bool TryCreate (
        string? path,
        [NotNullWhen(true)] out ClassifiedPathText? result,
        out PathValidationFailure failure)
    {
        result = null;
        if (!TryGetPlatformPath(path, out var platformPath, out failure))
        {
            return false;
        }

        result = new ClassifiedPathText(
            platformPath,
            Classify(platformPath));
        failure = default;
        return true;
    }

    private static bool TryGetPlatformPath (
        string? path,
        [NotNullWhen(true)] out string? platformPath,
        out PathValidationFailure failure)
    {
        platformPath = null;
        if (path is null || path.Length == 0)
        {
            failure = PathValidationFailure.Create(
                PathValidationFailureKind.EmptyPath,
                "Path must not be null or empty.");
            return false;
        }

        platformPath = PlatformPath.ToPlatformSeparators(path);
        if (!PlatformPath.TryValidateInputPath(
                platformPath,
                out var validationMessage))
        {
            failure = PathValidationFailure.Create(
                PathValidationFailureKind.InvalidPathFormat,
                validationMessage);
            platformPath = null;
            return false;
        }

        failure = default;
        return true;
    }

    private static ClassifiedPathKind Classify (string platformPath)
    {
        if (Path.IsPathFullyQualified(platformPath))
        {
            return ClassifiedPathKind.FullyQualified;
        }

        return Path.IsPathRooted(platformPath)
            ? ClassifiedPathKind.PartiallyQualifiedRooted
            : ClassifiedPathKind.Relative;
    }
}
