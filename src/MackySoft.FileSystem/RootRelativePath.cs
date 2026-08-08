using System.Diagnostics.CodeAnalysis;
using MackySoft.FileSystem.Internal;

namespace MackySoft.FileSystem;

/// <summary>
/// Represents normalized path text relative to an unspecified boundary root without traversal above that root.
/// </summary>
/// <remarks>
/// The canonical separator for recognized directory separators is <c>/</c>, and <c>.</c> represents the
/// boundary root itself. Separator recognition and case identity follow the running operating system;
/// on Unix, <c>\</c> and whitespace remain ordinary filename characters. Construction does not access
/// the filesystem or inspect the actual volume's case sensitivity.
/// On Windows, segments follow the package's ordinary-name syntax, only exact <c>.</c> and <c>..</c>
/// are navigation, and an endpoint that would disappear entirely after trailing space and period
/// normalization is rejected. Relative components and endpoint trimming are evaluated before validation.
/// A component that still ends in a space or period while followed by a directory separator, including a
/// trailing separator, is rejected. A separator-free final component can normalize to a stable endpoint,
/// and a component removed by navigation does not remain in the guarded value. This ensures that combining
/// the result with an <see cref="AbsolutePath" /> preserves that type's parent-closure invariant.
/// </remarks>
public sealed class RootRelativePath : IEquatable<RootRelativePath>
{
    private const string RootValue = ".";

    private readonly string value;

    private RootRelativePath (string normalizedValue)
    {
        value = normalizedValue;
    }

    /// <summary> Gets canonical root-relative text using <c>/</c> as the separator. </summary>
    public string Value => value;

    /// <summary> Gets whether this value identifies the boundary root itself. </summary>
    public bool IsRoot => string.Equals(value, RootValue, StringComparison.Ordinal);

    /// <summary> Parses root-relative path text without accessing the filesystem. </summary>
    /// <param name="path"> Non-empty, non-rooted path text that does not traverse above its boundary. </param>
    /// <returns> Canonical root-relative path text. </returns>
    /// <exception cref="PathValidationException"> The input violates the root-relative path contract. </exception>
    public static RootRelativePath Parse (string path)
    {
        if (TryParse(path, out var result, out var failure))
        {
            return result;
        }

        throw new PathValidationException(failure, nameof(path));
    }

    /// <summary> Attempts to parse root-relative path text without accessing the filesystem. </summary>
    /// <param name="path"> Path text to validate on the current platform. </param>
    /// <param name="result"> The guarded value when this method returns <see langword="true" />. </param>
    /// <param name="failure">
    /// <see cref="PathValidationFailureKind.None" /> on success; otherwise the violated input contract.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="path" /> is non-rooted and cannot traverse above its boundary;
    /// otherwise <see langword="false" />.
    /// </returns>
    public static bool TryParse (
        string? path,
        [NotNullWhen(true)] out RootRelativePath? result,
        out PathValidationFailure failure)
    {
        result = null;
        if (!ClassifiedPathText.TryCreate(path, out var classifiedPath, out failure))
        {
            return false;
        }

        return TryCreateFromClassifiedText(
            classifiedPath,
            out result,
            out failure);
    }

    internal static bool TryCreateFromClassifiedText (
        ClassifiedPathText path,
        [NotNullWhen(true)] out RootRelativePath? result,
        out PathValidationFailure failure)
    {
        result = null;
        if (path.Kind != ClassifiedPathKind.Relative)
        {
            failure = PathValidationFailure.Create(
                PathValidationFailureKind.ExpectedRootRelativePath,
                "Path must not be rooted on the current platform.");
            return false;
        }

        if (!TryNormalizeNonRootedPath(
                path.Value,
                out var normalizedPath,
                out failure))
        {
            return false;
        }

        result = new RootRelativePath(normalizedPath);
        failure = default;
        return true;
    }

    private static bool TryNormalizeNonRootedPath (
        string platformPath,
        [NotNullWhen(true)] out string? normalizedPath,
        out PathValidationFailure failure)
    {
        normalizedPath = null;
        try
        {
            if (!PlatformPath.TryNormalizeRootRelativePath(
                    platformPath,
                    out var candidate))
            {
                failure = PathValidationFailure.Create(
                    PathValidationFailureKind.OutsideBoundary,
                    "Path must not traverse above its boundary.");
                return false;
            }

            normalizedPath = candidate;
            failure = default;
            return true;
        }
        catch (Exception exception) when (PlatformPath.IsPathFormatException(exception))
        {
            failure = PathValidationFailure.Create(
                PathValidationFailureKind.InvalidPathFormat,
                exception.Message);
            return false;
        }
    }

    internal static RootRelativePath DeriveFromContainedPaths (
        AbsolutePath boundaryRoot,
        AbsolutePath target)
    {
        // ContainedPath proves the relationship before calling this derivation path, so the relative
        // text can be sliced from the retained normalized values without another raw-input validation pass.
        return new RootRelativePath(
            PlatformPath.DeriveRootRelativePath(
                boundaryRoot.Value,
                target.Value));
    }

    internal string ToPlatformPath ()
    {
        return PlatformPath.ToPlatformSeparators(Value);
    }

    /// <summary>
    /// Determines whether this root-relative path and <paramref name="candidate" /> identify the same position
    /// relative to an unspecified boundary under current-platform lexical identity rules.
    /// </summary>
    /// <param name="candidate"> The guarded root-relative path to compare with this path. </param>
    /// <returns>
    /// <see langword="true" /> when both paths have the same current-platform lexical identity;
    /// otherwise <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// This comparison uses canonical separators and current-platform case identity. It does not access the
    /// filesystem or inspect the actual volume's case sensitivity.
    /// </remarks>
    /// <exception cref="ArgumentNullException"> <paramref name="candidate" /> is <see langword="null" />. </exception>
    public bool IsSameAs (RootRelativePath candidate)
    {
        if (candidate is null)
        {
            throw new ArgumentNullException(nameof(candidate));
        }
        return PlatformPath.IdentityComparer.Equals(value, candidate.value);
    }

    /// <inheritdoc />
    public bool Equals (RootRelativePath? other)
    {
        return other is not null && IsSameAs(other);
    }

    /// <inheritdoc />
    public override bool Equals (object? obj)
    {
        return ReferenceEquals(this, obj)
            || (obj is RootRelativePath other && Equals(other));
    }

    /// <inheritdoc />
    public override int GetHashCode ()
    {
        return PlatformPath.IdentityComparer.GetHashCode(value);
    }

    /// <inheritdoc />
    public override string ToString ()
    {
        return Value;
    }

    /// <summary> Compares two guarded root-relative paths by current-platform identity. </summary>
    public static bool operator == (
        RootRelativePath? left,
        RootRelativePath? right)
    {
        return ReferenceEquals(left, right)
            || (left is not null && left.Equals(right));
    }

    /// <summary> Compares two guarded root-relative paths by current-platform identity. </summary>
    public static bool operator != (
        RootRelativePath? left,
        RootRelativePath? right)
    {
        return !(left == right);
    }
}
