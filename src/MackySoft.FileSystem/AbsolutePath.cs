using System.Diagnostics.CodeAnalysis;
using MackySoft.FileSystem.Internal;

namespace MackySoft.FileSystem;

/// <summary>
/// Represents non-empty, normalized, fully qualified path text using current-platform identity semantics.
/// </summary>
/// <remarks>
/// The running operating system defines lexical separators, roots, fully-qualified syntax, and case identity.
/// The value is not converted to a portable common-path string. These operating-system rules are centralized
/// for equality, hashing, and containment, but they do not inspect the actual volume's case sensitivity.
/// Windows device-namespace path text is not accepted because that syntax bypasses ordinary lexical normalization.
/// Windows short-name-looking segments such as <c>PROGRA~1</c> are retained without consulting the filesystem
/// to expand them to an existing long name.
/// Windows alternate-data-stream syntax, reserved DOS device names, structurally incomplete UNC roots, and
/// characters that are invalid in ordinary Windows path segments are rejected. UNC host reachability and
/// remote-provider naming policies are not validated.
/// On Windows, relative components and endpoint trimming are evaluated before the normalized result is
/// inspected. A component that still ends in a space or period while followed by a directory separator,
/// including a trailing separator, is rejected because its endpoint identity would differ. A separator-free
/// final component can be trimmed to a stable endpoint, and a component removed by navigation does not remain
/// in the guarded value. The same rule applies to a UNC share when a separator follows it; UNC roots with or
/// without a trailing separator otherwise normalize to one structurally complete root value that remains
/// stable when its <see cref="Value" /> is parsed again.
/// Construction does not access the filesystem and does not guarantee existence, node kind, accessibility,
/// identity after symbolic-link resolution, or physical containment.
/// </remarks>
public sealed class AbsolutePath : IEquatable<AbsolutePath>
{
    private readonly string value;

    private AbsolutePath (string normalizedValue)
    {
        value = normalizedValue;
    }

    /// <summary> Gets normalized path text using current-platform separators while preserving input casing. </summary>
    public string Value => value;

    /// <summary> Parses fully qualified path text without accessing the filesystem. </summary>
    /// <param name="path"> Non-empty path text that is fully qualified on the current platform. </param>
    /// <returns> A normalized absolute path. </returns>
    /// <exception cref="PathValidationException"> The input violates the absolute path contract. </exception>
    public static AbsolutePath Parse (string path)
    {
        if (TryParse(path, out var result, out var failure))
        {
            return result;
        }

        throw new PathValidationException(failure, nameof(path));
    }

    /// <summary> Attempts to parse fully qualified path text without accessing the filesystem. </summary>
    /// <param name="path"> Path text to validate on the current platform. </param>
    /// <param name="result"> The guarded value when this method returns <see langword="true" />. </param>
    /// <param name="failure">
    /// <see cref="PathValidationFailureKind.None" /> on success; otherwise the violated input contract.
    /// </param>
    /// <returns> <see langword="true" /> when <paramref name="path" /> is a valid fully qualified path; otherwise <see langword="false" />. </returns>
    public static bool TryParse (
        string? path,
        [NotNullWhen(true)] out AbsolutePath? result,
        out PathValidationFailure failure)
    {
        return TryCreate(path, relativeBase: null, out result, out failure);
    }

    /// <summary>
    /// Resolves fully qualified absolute or non-rooted relative path text from a guarded base path
    /// without asserting a containment boundary.
    /// </summary>
    /// <param name="basePath"> The absolute base used only when <paramref name="path" /> is relative. </param>
    /// <param name="path"> Non-empty fully qualified absolute or non-rooted relative path text. </param>
    /// <returns> A normalized absolute path. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="basePath" /> is <see langword="null" />. </exception>
    /// <exception cref="PathValidationException"> <paramref name="path" /> violates a path contract. </exception>
    public static AbsolutePath Resolve (
        AbsolutePath basePath,
        string path)
    {
        if (TryResolve(basePath, path, out var result, out var failure))
        {
            return result;
        }

        throw new PathValidationException(failure, nameof(path));
    }

    /// <summary>
    /// Attempts to resolve fully qualified absolute or non-rooted relative path text from a guarded base path
    /// without asserting a containment boundary.
    /// </summary>
    /// <param name="basePath"> The absolute base used only when <paramref name="path" /> is relative. </param>
    /// <param name="path"> Fully qualified absolute or non-rooted relative path text to validate. </param>
    /// <param name="result"> The normalized absolute path when this method returns <see langword="true" />. </param>
    /// <param name="failure">
    /// <see cref="PathValidationFailureKind.None" /> on success; otherwise the violated path contract.
    /// </param>
    /// <returns> <see langword="true" /> when the input can be resolved; otherwise <see langword="false" />. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="basePath" /> is <see langword="null" />. </exception>
    public static bool TryResolve (
        AbsolutePath basePath,
        string? path,
        [NotNullWhen(true)] out AbsolutePath? result,
        out PathValidationFailure failure)
    {
        if (basePath is null)
        {
            throw new ArgumentNullException(nameof(basePath));
        }
        return TryCreate(path, basePath, out result, out failure);
    }

    /// <summary>
    /// Determines whether this absolute path and <paramref name="candidate" /> identify the same normalized path
    /// under current-platform lexical identity rules.
    /// </summary>
    /// <param name="candidate"> The guarded absolute path to compare with this path. </param>
    /// <returns>
    /// <see langword="true" /> when both paths have the same current-platform lexical identity;
    /// otherwise <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// This comparison does not access the filesystem or inspect the actual volume's case sensitivity.
    /// </remarks>
    /// <exception cref="ArgumentNullException"> <paramref name="candidate" /> is <see langword="null" />. </exception>
    public bool IsSameAs (AbsolutePath candidate)
    {
        if (candidate is null)
        {
            throw new ArgumentNullException(nameof(candidate));
        }
        return PlatformPath.IdentityComparer.Equals(value, candidate.value);
    }

    /// <summary>
    /// Determines whether <paramref name="candidate" /> is lexically equal to or below this path.
    /// </summary>
    /// <remarks> This operation does not resolve symbolic links or access the filesystem. </remarks>
    /// <exception cref="ArgumentNullException"> <paramref name="candidate" /> is <see langword="null" />. </exception>
    public bool IsSameOrAncestorOf (AbsolutePath candidate)
    {
        return IsSameAs(candidate) || IsAncestorCore(candidate);
    }

    /// <summary> Determines whether <paramref name="candidate" /> is lexically below this path. </summary>
    /// <remarks> This operation does not resolve symbolic links or access the filesystem. </remarks>
    /// <exception cref="ArgumentNullException"> <paramref name="candidate" /> is <see langword="null" />. </exception>
    public bool IsAncestorOf (AbsolutePath candidate)
    {
        if (candidate is null)
        {
            throw new ArgumentNullException(nameof(candidate));
        }
        return !PlatformPath.IdentityComparer.Equals(value, candidate.value)
            && IsAncestorCore(candidate);
    }

    /// <summary>
    /// Gets the normalized filesystem root that lexically contains this path.
    /// </summary>
    /// <remarks>
    /// This operation preserves the running operating system's root spelling and does not access the filesystem.
    /// </remarks>
    /// <returns> The guarded drive, share, or Unix root for this path. </returns>
    public AbsolutePath GetRoot ()
    {
        // AbsolutePath construction has already proved that a current-platform root exists.
        // Deriving that root cannot invalidate the absolute or normalized structure.
        return new AbsolutePath(
            PlatformPath.TrimTrailingSeparatorsUnlessRoot(
                Path.GetPathRoot(value)!));
    }

    /// <summary>
    /// Attempts to get the normalized immediate lexical parent without accessing the filesystem.
    /// </summary>
    /// <param name="parent">
    /// The normalized immediate lexical parent when this method returns <see langword="true" />;
    /// otherwise <see langword="null" /> when this path is a filesystem root.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when this path is not a filesystem root; otherwise <see langword="false" />.
    /// </returns>
    public bool TryGetParent ([NotNullWhen(true)] out AbsolutePath? parent)
    {
        var root = Path.GetPathRoot(value);
        if (!string.IsNullOrEmpty(root)
            && PlatformPath.IdentityComparer.Equals(value, root))
        {
            parent = null;
            return false;
        }

        var parentValue = Path.GetDirectoryName(value);
        if (string.IsNullOrEmpty(parentValue))
        {
            parent = null;
            return false;
        }

        // Construction rejects every retained Windows intermediate component whose identity would
        // change in endpoint position. The immediate parent therefore remains a guarded value.
        parent = new AbsolutePath(parentValue);
        return true;
    }

    internal AbsolutePath Combine (RootRelativePath relativePath)
    {
        if (relativePath is null)
        {
            throw new ArgumentNullException(nameof(relativePath));
        }
        if (relativePath.IsRoot)
        {
            return this;
        }

        // Both operands are already normalized. A root-relative value cannot replace the absolute
        // root or traverse above it, so Path.Combine preserves every AbsolutePath invariant.
        return new AbsolutePath(Path.Combine(value, relativePath.ToPlatformPath()));
    }

    private static bool TryCreate (
        string? path,
        AbsolutePath? relativeBase,
        [NotNullWhen(true)] out AbsolutePath? result,
        out PathValidationFailure failure)
    {
        result = null;
        if (!ClassifiedPathText.TryCreate(path, out var classifiedPath, out failure))
        {
            return false;
        }

        return TryCreateFromClassifiedText(
            classifiedPath,
            relativeBase,
            out result,
            out failure);
    }

    internal static bool TryCreateFromClassifiedText (
        ClassifiedPathText path,
        AbsolutePath? relativeBase,
        [NotNullWhen(true)] out AbsolutePath? result,
        out PathValidationFailure failure)
    {
        result = null;
        if (!TryResolveFullPath(
                path,
                relativeBase,
                out var fullPath,
                out failure))
        {
            return false;
        }

        result = new AbsolutePath(fullPath);
        failure = default;
        return true;
    }

    private static bool TryResolveFullPath (
        ClassifiedPathText path,
        AbsolutePath? relativeBase,
        [NotNullWhen(true)] out string? fullPath,
        out PathValidationFailure failure)
    {
        fullPath = null;
        var isFullyQualified = path.Kind == ClassifiedPathKind.FullyQualified;
        if (!isFullyQualified && relativeBase is null)
        {
            failure = PathValidationFailure.Create(
                PathValidationFailureKind.ExpectedAbsolutePath,
                "Path must be fully qualified on the current platform.");
            return false;
        }

        if (path.Kind == ClassifiedPathKind.PartiallyQualifiedRooted)
        {
            failure = PathValidationFailure.Create(
                PathValidationFailureKind.ExpectedAbsolutePath,
                "Partially qualified rooted path text is not supported.");
            return false;
        }

        return TryNormalizeFullPath(
            path.Value,
            isFullyQualified ? null : relativeBase,
            out fullPath,
            out failure);
    }

    private static bool TryNormalizeFullPath (
        string platformPath,
        AbsolutePath? relativeBase,
        [NotNullWhen(true)] out string? fullPath,
        out PathValidationFailure failure)
    {
        fullPath = null;
        try
        {
            fullPath = PlatformPath.NormalizeAbsolutePathLexically(
                platformPath,
                relativeBase?.value);
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

    private bool IsAncestorCore (AbsolutePath candidate)
    {
        var prefix = value.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
            ? value
            : value + Path.DirectorySeparatorChar;
        return candidate.value.StartsWith(prefix, PlatformPath.IdentityComparison);
    }

    /// <inheritdoc />
    public bool Equals (AbsolutePath? other)
    {
        return other is not null && IsSameAs(other);
    }

    /// <inheritdoc />
    public override bool Equals (object? obj)
    {
        return ReferenceEquals(this, obj)
            || (obj is AbsolutePath other && Equals(other));
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

    /// <summary> Compares two guarded absolute paths by current-platform identity. </summary>
    public static bool operator == (
        AbsolutePath? left,
        AbsolutePath? right)
    {
        return ReferenceEquals(left, right)
            || (left is not null && left.Equals(right));
    }

    /// <summary> Compares two guarded absolute paths by current-platform identity. </summary>
    public static bool operator != (
        AbsolutePath? left,
        AbsolutePath? right)
    {
        return !(left == right);
    }
}
