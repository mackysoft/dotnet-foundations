using System.Diagnostics.CodeAnalysis;

namespace MackySoft.FileSystem;

/// <summary>
/// Carries an absolute boundary root, a lexically contained absolute target, and their matching root-relative path.
/// </summary>
/// <remarks>
/// This value proves only normalized lexical containment under the running operating system's separator,
/// root, fully-qualified, and case-identity rules. It does not inspect the actual volume's case sensitivity
/// and does not guarantee existence, node kind, accessibility, identity after symbolic-link resolution,
/// or physical containment. Callers must observe mutable filesystem state immediately before an operation
/// that depends on it.
/// </remarks>
public sealed class ContainedPath : IEquatable<ContainedPath>
{
    private readonly AbsolutePath boundaryRoot;
    private readonly AbsolutePath target;
    private readonly RootRelativePath relativePath;

    private ContainedPath (
        AbsolutePath boundaryRoot,
        AbsolutePath target,
        RootRelativePath relativePath)
    {
        this.boundaryRoot = boundaryRoot;
        this.target = target;
        this.relativePath = relativePath;
    }

    /// <summary> Gets the normalized absolute boundary root. </summary>
    public AbsolutePath BoundaryRoot => boundaryRoot;

    /// <summary> Gets the normalized absolute target that is lexically contained by <see cref="BoundaryRoot" />. </summary>
    public AbsolutePath Target => target;

    /// <summary> Gets the canonical path from <see cref="BoundaryRoot" /> to <see cref="Target" />. </summary>
    public RootRelativePath RelativePath => relativePath;

    /// <summary> Creates a lexical containment relation from two guarded absolute paths. </summary>
    /// <param name="boundaryRoot"> The absolute path that defines the lexical boundary. </param>
    /// <param name="target"> The absolute path that must equal or descend from <paramref name="boundaryRoot" />. </param>
    /// <returns> A guarded relation with a matching root-relative path. </returns>
    /// <exception cref="ArgumentNullException"> Either input is <see langword="null" />. </exception>
    /// <exception cref="PathValidationException"> <paramref name="target" /> is outside <paramref name="boundaryRoot" />. </exception>
    public static ContainedPath Create (
        AbsolutePath boundaryRoot,
        AbsolutePath target)
    {
        if (TryCreate(boundaryRoot, target, out var result, out var failure))
        {
            return result;
        }

        throw new PathValidationException(failure, nameof(target));
    }

    /// <summary> Creates a lexical containment relation from a guarded boundary and root-relative path. </summary>
    /// <param name="boundaryRoot"> The absolute path that defines the lexical boundary. </param>
    /// <param name="relativePath"> A canonical path that cannot traverse above the boundary. </param>
    /// <returns> A guarded relation with a matching absolute target. </returns>
    /// <exception cref="ArgumentNullException"> Either input is <see langword="null" />. </exception>
    public static ContainedPath Create (
        AbsolutePath boundaryRoot,
        RootRelativePath relativePath)
    {
        if (boundaryRoot is null)
        {
            throw new ArgumentNullException(nameof(boundaryRoot));
        }

        if (relativePath is null)
        {
            throw new ArgumentNullException(nameof(relativePath));
        }
        return new ContainedPath(
            boundaryRoot,
            boundaryRoot.Combine(relativePath),
            relativePath);
    }

    /// <summary> Attempts to create a lexical containment relation from two guarded absolute paths. </summary>
    /// <param name="boundaryRoot"> The absolute path that defines the lexical boundary. </param>
    /// <param name="target"> The absolute path to compare with the boundary. </param>
    /// <param name="result"> The guarded relation when this method returns <see langword="true" />. </param>
    /// <param name="failure">
    /// <see cref="PathValidationFailureKind.None" /> on success; otherwise
    /// <see cref="PathValidationFailureKind.OutsideBoundary" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="target" /> equals or lexically descends from
    /// <paramref name="boundaryRoot" />; otherwise <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"> Either input is <see langword="null" />. </exception>
    public static bool TryCreate (
        AbsolutePath boundaryRoot,
        AbsolutePath target,
        [NotNullWhen(true)] out ContainedPath? result,
        out PathValidationFailure failure)
    {
        if (boundaryRoot is null)
        {
            throw new ArgumentNullException(nameof(boundaryRoot));
        }

        if (target is null)
        {
            throw new ArgumentNullException(nameof(target));
        }
        result = null;

        if (!boundaryRoot.IsSameOrAncestorOf(target))
        {
            failure = PathValidationFailure.Create(
                PathValidationFailureKind.OutsideBoundary,
                "Target path must equal or lexically descend from the boundary root.");
            return false;
        }

        var relativePath = RootRelativePath.DeriveFromContainedPaths(
            boundaryRoot,
            target);
        result = new ContainedPath(boundaryRoot, target, relativePath);
        failure = default;
        return true;
    }

    /// <summary>
    /// Resolves absolute or root-relative path text against a guarded boundary without accessing the filesystem.
    /// </summary>
    /// <param name="boundaryRoot"> The absolute path that defines the lexical boundary and relative base. </param>
    /// <param name="path"> Non-empty absolute or root-relative path text. </param>
    /// <returns> A guarded lexical containment relation. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="boundaryRoot" /> is <see langword="null" />. </exception>
    /// <exception cref="PathValidationException"> <paramref name="path" /> violates a path or containment contract. </exception>
    public static ContainedPath Resolve (
        AbsolutePath boundaryRoot,
        string path)
    {
        if (TryResolve(boundaryRoot, path, out var result, out var failure))
        {
            return result;
        }

        throw new PathValidationException(failure, nameof(path));
    }

    /// <summary>
    /// Attempts to resolve absolute or root-relative path text against a guarded boundary without accessing the filesystem.
    /// </summary>
    /// <param name="boundaryRoot"> The absolute path that defines the lexical boundary and relative base. </param>
    /// <param name="path">
    /// Absolute or root-relative path text to validate. Root-relative input cannot traverse above
    /// <paramref name="boundaryRoot" />, including by leaving and later re-entering the boundary.
    /// </param>
    /// <param name="result"> The guarded relation when this method returns <see langword="true" />. </param>
    /// <param name="failure">
    /// <see cref="PathValidationFailureKind.None" /> on success; otherwise the violated path or containment contract.
    /// </param>
    /// <returns> <see langword="true" /> when the input resolves within the boundary; otherwise <see langword="false" />. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="boundaryRoot" /> is <see langword="null" />. </exception>
    public static bool TryResolve (
        AbsolutePath boundaryRoot,
        string? path,
        [NotNullWhen(true)] out ContainedPath? result,
        out PathValidationFailure failure)
    {
        if (boundaryRoot is null)
        {
            throw new ArgumentNullException(nameof(boundaryRoot));
        }

        result = null;
        if (!ClassifiedPathText.TryCreate(path, out var classifiedPath, out failure))
        {
            return false;
        }

        if (classifiedPath.Kind == ClassifiedPathKind.FullyQualified)
        {
            return TryResolveAbsolute(
                boundaryRoot,
                classifiedPath,
                out result,
                out failure);
        }

        return TryResolveRelative(
            boundaryRoot,
            classifiedPath,
            out result,
            out failure);
    }

    private static bool TryResolveAbsolute (
        AbsolutePath boundaryRoot,
        ClassifiedPathText path,
        [NotNullWhen(true)] out ContainedPath? result,
        out PathValidationFailure failure)
    {
        result = null;
        if (!AbsolutePath.TryCreateFromClassifiedText(
                path,
                relativeBase: null,
                out var target,
                out failure))
        {
            return false;
        }

        return TryCreate(boundaryRoot, target, out result, out failure);
    }

    private static bool TryResolveRelative (
        AbsolutePath boundaryRoot,
        ClassifiedPathText path,
        [NotNullWhen(true)] out ContainedPath? result,
        out PathValidationFailure failure)
    {
        result = null;
        if (!RootRelativePath.TryCreateFromClassifiedText(
                path,
                out var relativePath,
                out failure))
        {
            return false;
        }

        result = Create(boundaryRoot, relativePath);
        failure = default;
        return true;
    }

    /// <summary>
    /// Determines whether this containment relation and <paramref name="candidate" /> have the same boundary root
    /// and target under current-platform lexical identity rules.
    /// </summary>
    /// <param name="candidate"> The guarded containment relation to compare with this relation. </param>
    /// <returns>
    /// <see langword="true" /> when both boundary roots are the same and both targets are the same;
    /// otherwise <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// <see cref="RelativePath" /> is derived from the boundary and target identities and is not compared
    /// independently. This comparison does not access the filesystem or inspect the actual volume's case sensitivity.
    /// </remarks>
    /// <exception cref="ArgumentNullException"> <paramref name="candidate" /> is <see langword="null" />. </exception>
    public bool HasSameBoundaryAndTargetAs (ContainedPath candidate)
    {
        if (candidate is null)
        {
            throw new ArgumentNullException(nameof(candidate));
        }
        return boundaryRoot.IsSameAs(candidate.boundaryRoot)
            && target.IsSameAs(candidate.target);
    }

    /// <inheritdoc />
    public bool Equals (ContainedPath? other)
    {
        return other is not null && HasSameBoundaryAndTargetAs(other);
    }

    /// <inheritdoc />
    public override bool Equals (object? obj)
    {
        return ReferenceEquals(this, obj)
            || (obj is ContainedPath other && Equals(other));
    }

    /// <inheritdoc />
    public override int GetHashCode ()
    {
        unchecked
        {
            return (boundaryRoot.GetHashCode() * 397) ^ target.GetHashCode();
        }
    }

    /// <inheritdoc />
    public override string ToString ()
    {
        return Target.Value;
    }

    /// <summary> Compares two guarded containment relations by boundary and target identity. </summary>
    public static bool operator == (
        ContainedPath? left,
        ContainedPath? right)
    {
        return ReferenceEquals(left, right)
            || (left is not null && left.Equals(right));
    }

    /// <summary> Compares two guarded containment relations by boundary and target identity. </summary>
    public static bool operator != (
        ContainedPath? left,
        ContainedPath? right)
    {
        return !(left == right);
    }
}
