using System.Diagnostics.CodeAnalysis;

namespace MackySoft.FileSystem;

/// <summary>
/// Resolves existing link segments and verifies link-resolved containment using current-platform lexical identity rules.
/// </summary>
public static class PhysicalPathResolver
{
    private const int MaximumLinkDepth = 64;

    /// <summary> Attempts to resolve a lexically contained path using explicit link and missing-entry policies. </summary>
    /// <param name="path"> The guarded lexical boundary and target to resolve. </param>
    /// <param name="symbolicLinkHandling">
    /// The required behavior for the boundary entry and existing links or reparse points below it.
    /// </param>
    /// <param name="missingPathHandling"> The required behavior after the first missing segment. </param>
    /// <param name="resolution"> The resolved snapshot when this method returns <see langword="true" />. </param>
    /// <param name="failure">
    /// <see cref="FileSystemOperationFailureKind.None" /> on success; otherwise the failed physical resolution.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when the target satisfies both policies and remains contained under current-platform
    /// lexical identity rules; otherwise <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// Resolution observes filesystem state, but containment does not inspect case-sensitivity overrides of an individual volume.
    /// The returned value is a snapshot and does not reserve any path entry against later replacement.
    /// </remarks>
    /// <exception cref="ArgumentNullException"> <paramref name="path" /> is <see langword="null" />. </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="symbolicLinkHandling" /> or <paramref name="missingPathHandling" /> is not defined.
    /// </exception>
    public static bool TryResolve (
        ContainedPath path,
        SymbolicLinkHandling symbolicLinkHandling,
        MissingPathHandling missingPathHandling,
        [NotNullWhen(true)] out PhysicalPathResolution? resolution,
        out FileSystemOperationFailure failure)
    {
        ArgumentNullException.ThrowIfNull(path);
        ValidatePolicy(symbolicLinkHandling, nameof(symbolicLinkHandling));
        ValidatePolicy(missingPathHandling, nameof(missingPathHandling));

        if (!FileSystemEntryInspector.TryInspect(path.BoundaryRoot, out var requestedRootObservation, out failure))
        {
            resolution = null;
            return false;
        }

        if (requestedRootObservation.State is FileSystemEntryState.SymbolicLink or FileSystemEntryState.ReparsePoint
            && symbolicLinkHandling == SymbolicLinkHandling.Reject)
        {
            resolution = null;
            failure = FileSystemOperationFailure.Create(
                FileSystemOperationFailureKind.LinkNotAllowed,
                path.BoundaryRoot,
                "The selected path policy does not permit the boundary root to be a symbolic link, junction, or reparse point.");
            return false;
        }

        // NOTE: Ancestors above the caller's boundary are resolved to establish one link-resolved root path.
        // The caller-selected link policy begins at the boundary itself and applies below that root.
        if (!TryResolveAbsolute(
                path.BoundaryRoot,
                SymbolicLinkHandling.Follow,
                missingPathHandling,
                out var resolvedRoot,
                out var rootObservation,
                out failure))
        {
            resolution = null;
            return false;
        }

        if (rootObservation.State is not FileSystemEntryState.Directory and not FileSystemEntryState.Missing)
        {
            resolution = null;
            failure = FileSystemOperationFailure.Create(
                FileSystemOperationFailureKind.UnexpectedEntryKind,
                resolvedRoot,
                "A link-resolution boundary must be a directory or a missing path.");
            return false;
        }

        var targetUnderResolvedRoot = ContainedPath.Create(resolvedRoot, path.RelativePath).Target;
        if (!TryResolveAbsolute(
                targetUnderResolvedRoot,
                symbolicLinkHandling,
                missingPathHandling,
                out var resolvedTarget,
                out var targetObservation,
                out failure))
        {
            resolution = null;
            return false;
        }

        // NOTE: The lexical path package deliberately centralizes current-platform identity rules. This
        // comparison does not claim to observe case-sensitivity overrides of an individual mounted volume.
        if (!ContainedPath.TryCreate(resolvedRoot, resolvedTarget, out var resolvedPath, out _))
        {
            resolution = null;
            failure = FileSystemOperationFailure.Create(
                FileSystemOperationFailureKind.OutsideBoundary,
                resolvedTarget,
                "The link-resolved target is outside the link-resolved boundary under current-platform path identity rules.");
            return false;
        }

        resolution = new PhysicalPathResolution(path, resolvedPath, targetObservation);
        failure = default;
        return true;
    }

    private static bool TryResolveAbsolute (
        AbsolutePath requestedPath,
        SymbolicLinkHandling symbolicLinkHandling,
        MissingPathHandling missingPathHandling,
        [NotNullWhen(true)] out AbsolutePath? resolvedPath,
        [NotNullWhen(true)] out FileSystemEntryObservation? targetObservation,
        out FileSystemOperationFailure failure)
    {
        var pendingPath = requestedPath;
        var visitedLinks = new HashSet<AbsolutePath>();
        var linkDepth = 0;

        while (true)
        {
            var rootText = Path.GetPathRoot(pendingPath.Value);
            if (string.IsNullOrEmpty(rootText)
                || !AbsolutePath.TryParse(rootText, out var currentPath, out _))
            {
                resolvedPath = null;
                targetObservation = null;
                failure = FileSystemOperationFailure.Create(
                    FileSystemOperationFailureKind.IoFailure,
                    pendingPath,
                    "The guarded path does not have a resolvable filesystem root.");
                return false;
            }

            var segments = pendingPath.Value[rootText.Length..]
                .Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0)
            {
                if (!FileSystemEntryInspector.TryInspect(currentPath, out targetObservation, out failure))
                {
                    resolvedPath = null;
                    return false;
                }

                resolvedPath = currentPath;
                return true;
            }

            var restart = false;
            for (var index = 0; index < segments.Length; index++)
            {
                var candidatePath = Append(currentPath, segments[index]);
                if (!FileSystemEntryInspector.TryInspect(candidatePath, out var observation, out failure))
                {
                    resolvedPath = null;
                    targetObservation = null;
                    return false;
                }

                if (observation.State == FileSystemEntryState.Missing)
                {
                    if (missingPathHandling == MissingPathHandling.Reject)
                    {
                        resolvedPath = null;
                        targetObservation = null;
                        failure = FileSystemOperationFailure.Create(
                            FileSystemOperationFailureKind.EntryNotFound,
                            candidatePath,
                            "A required path segment does not exist.");
                        return false;
                    }

                    resolvedPath = AppendTail(candidatePath, segments, index + 1);
                    targetObservation = new FileSystemEntryObservation(resolvedPath, FileSystemEntryState.Missing);
                    failure = default;
                    return true;
                }

                if (observation.State is FileSystemEntryState.SymbolicLink or FileSystemEntryState.ReparsePoint)
                {
                    if (symbolicLinkHandling == SymbolicLinkHandling.Reject)
                    {
                        resolvedPath = null;
                        targetObservation = null;
                        failure = FileSystemOperationFailure.Create(
                            FileSystemOperationFailureKind.LinkNotAllowed,
                            candidatePath,
                            "The selected path policy does not permit a symbolic link, junction, or reparse point.");
                        return false;
                    }

                    if (observation.State == FileSystemEntryState.ReparsePoint)
                    {
                        resolvedPath = null;
                        targetObservation = null;
                        failure = FileSystemOperationFailure.Create(
                            FileSystemOperationFailureKind.UnexpectedEntryKind,
                            candidatePath,
                            "The Windows reparse point is not a symbolic link or junction and cannot be followed.");
                        return false;
                    }

                    linkDepth++;
                    if (linkDepth > MaximumLinkDepth || !visitedLinks.Add(candidatePath))
                    {
                        resolvedPath = null;
                        targetObservation = null;
                        failure = FileSystemOperationFailure.Create(
                            FileSystemOperationFailureKind.LinkCycle,
                            candidatePath,
                            "Symbolic-link resolution contains a cycle or exceeds the supported link depth.");
                        return false;
                    }

                    if (!TryResolveLinkTarget(candidatePath, out var linkTarget, out failure))
                    {
                        resolvedPath = null;
                        targetObservation = null;
                        return false;
                    }

                    pendingPath = AppendTail(linkTarget, segments, index + 1);
                    restart = true;
                    break;
                }

                if (index < segments.Length - 1
                    && observation.State != FileSystemEntryState.Directory)
                {
                    resolvedPath = null;
                    targetObservation = null;
                    failure = FileSystemOperationFailure.Create(
                        FileSystemOperationFailureKind.UnexpectedEntryKind,
                        candidatePath,
                        "A non-directory entry cannot contain another path segment.");
                    return false;
                }

                currentPath = candidatePath;
                if (index == segments.Length - 1)
                {
                    resolvedPath = currentPath;
                    targetObservation = observation;
                    failure = default;
                    return true;
                }
            }

            if (!restart)
            {
                throw new InvalidOperationException("Physical path resolution did not produce a terminal state.");
            }
        }
    }

    private static bool TryResolveLinkTarget (
        AbsolutePath linkPath,
        [NotNullWhen(true)] out AbsolutePath? targetPath,
        out FileSystemOperationFailure failure)
    {
        try
        {
            FileSystemInfo link = Directory.Exists(linkPath.Value)
                ? new DirectoryInfo(linkPath.Value)
                : new FileInfo(linkPath.Value);
            var target = link.ResolveLinkTarget(returnFinalTarget: false);
            if (target is null)
            {
                targetPath = null;
                failure = FileSystemOperationFailure.Create(
                    FileSystemOperationFailureKind.IoFailure,
                    linkPath,
                    "The link target could not be resolved.");
                return false;
            }

            if (!AbsolutePath.TryParse(target.FullName, out targetPath, out var pathFailure))
            {
                failure = FileSystemOperationFailure.Create(
                    FileSystemOperationFailureKind.IoFailure,
                    linkPath,
                    $"The link target is outside the supported ordinary path contract: {pathFailure.Message}");
                return false;
            }

            failure = default;
            return true;
        }
        catch (FileNotFoundException exception)
        {
            targetPath = null;
            failure = FileSystemOperationFailure.Create(FileSystemOperationFailureKind.ConcurrentChange, linkPath, exception.Message);
            return false;
        }
        catch (DirectoryNotFoundException exception)
        {
            targetPath = null;
            failure = FileSystemOperationFailure.Create(FileSystemOperationFailureKind.ConcurrentChange, linkPath, exception.Message);
            return false;
        }
        catch (UnauthorizedAccessException exception)
        {
            targetPath = null;
            failure = FileSystemOperationFailure.Create(FileSystemOperationFailureKind.AccessDenied, linkPath, exception.Message);
            return false;
        }
        catch (PlatformNotSupportedException exception)
        {
            targetPath = null;
            failure = FileSystemOperationFailure.Create(FileSystemOperationFailureKind.PlatformNotSupported, linkPath, exception.Message);
            return false;
        }
        catch (IOException exception)
        {
            targetPath = null;
            failure = FileSystemOperationFailure.Create(FileSystemOperationFailureKind.IoFailure, linkPath, exception.Message);
            return false;
        }
    }

    private static AbsolutePath Append (
        AbsolutePath parent,
        string segment)
    {
        return ContainedPath.Create(parent, RootRelativePath.Parse(segment)).Target;
    }

    private static AbsolutePath AppendTail (
        AbsolutePath parent,
        IReadOnlyList<string> segments,
        int startIndex)
    {
        if (startIndex >= segments.Count)
        {
            return parent;
        }

        var relativeText = string.Join('/', segments.Skip(startIndex));
        return ContainedPath.Create(parent, RootRelativePath.Parse(relativeText)).Target;
    }

    private static void ValidatePolicy<T> (
        T policy,
        string parameterName)
        where T : struct, Enum
    {
        if (!Enum.IsDefined(typeof(T), policy))
        {
            throw new ArgumentOutOfRangeException(parameterName, policy, "Policy must be defined.");
        }
    }
}
