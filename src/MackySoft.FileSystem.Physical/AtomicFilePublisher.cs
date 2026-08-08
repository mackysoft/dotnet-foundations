using System.Buffers;
using System.Runtime.ExceptionServices;

namespace MackySoft.FileSystem;

/// <summary> Publishes a complete file through a unique temporary sibling and a same-directory move or replacement. </summary>
public static class AtomicFilePublisher
{
    private const int CopyBufferSize = 81920;

    /// <summary> Publishes borrowed stream contents according to an explicit publication contract. </summary>
    /// <param name="publication"> The target boundary and required filesystem policies. </param>
    /// <param name="contents"> A readable stream borrowed for the duration of the operation. </param>
    /// <param name="cancellationToken"> Observes cancellation while copying and flushing the temporary file. </param>
    /// <returns>
    /// A result that succeeds only after the complete temporary file has been moved or replaced at the resolved target.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The stream is not disposed. Exceptions raised while reading it propagate to the caller and are not converted into
    /// target-filesystem failures.
    /// </para>
    /// <para>
    /// The operating-system move or replacement is atomic only when the filesystem provider gives that guarantee for the
    /// source and target sibling paths. This method does not detect or strengthen provider-specific atomicity guarantees.
    /// Publication does not guarantee preservation of target metadata or durability of directory metadata.
    /// </para>
    /// <para>
    /// Parent directories created by <see cref="MissingParentHandling.Create" /> remain after a later failure or cancellation.
    /// A detected resolved-path change is reported as <see cref="FileSystemOperationFailureKind.ConcurrentChange" />, but
    /// path-based operating-system APIs cannot reserve every segment against hostile concurrent replacement.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="publication" /> or <paramref name="contents" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException"> <paramref name="contents" /> is not readable. </exception>
    public static async ValueTask<FileSystemOperationResult> PublishAsync (
        AtomicFilePublication publication,
        Stream contents,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(publication);
        ArgumentNullException.ThrowIfNull(contents);
        if (!contents.CanRead)
        {
            throw new ArgumentException("Publication contents must be readable.", nameof(contents));
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (!TryGetParent(publication.TargetPath, out var requestedParent))
        {
            throw new ArgumentException("The publication target must have a parent below its boundary.", nameof(publication));
        }

        var parentMissingHandling = publication.MissingParentHandling == MissingParentHandling.Create
            ? MissingPathHandling.AllowMissingTail
            : MissingPathHandling.Reject;
        if (!PhysicalPathResolver.TryResolve(
                requestedParent,
                publication.SymbolicLinkHandling,
                parentMissingHandling,
                out var parentResolution,
                out var failure))
        {
            return FileSystemOperationResult.FailureResult(failure);
        }

        var resolvedParent = parentResolution.ResolvedPath.Target;
        if (parentResolution.TargetObservation.State == FileSystemEntryState.Missing)
        {
            var createResult = TryCreateParent(resolvedParent);
            if (!createResult.IsSuccess)
            {
                return createResult;
            }

            if (!PhysicalPathResolver.TryResolve(
                    requestedParent,
                    publication.SymbolicLinkHandling,
                    MissingPathHandling.Reject,
                    out parentResolution,
                    out failure))
            {
                return FileSystemOperationResult.FailureResult(failure);
            }

            resolvedParent = parentResolution.ResolvedPath.Target;
        }

        if (parentResolution.TargetObservation.State != FileSystemEntryState.Directory)
        {
            return Failure(
                FileSystemOperationFailureKind.UnexpectedEntryKind,
                resolvedParent,
                "The publication target parent must be a directory.");
        }

        if (!PhysicalPathResolver.TryResolve(
                publication.TargetPath,
                publication.SymbolicLinkHandling,
                MissingPathHandling.AllowMissingTail,
                out var targetResolution,
                out failure))
        {
            return FileSystemOperationResult.FailureResult(failure);
        }

        var targetPath = targetResolution.ResolvedPath.Target;
        var targetStateResult = ValidateTargetState(
            targetResolution.TargetObservation.State,
            targetPath,
            publication.ExistingTargetHandling);
        if (!targetStateResult.IsSuccess)
        {
            return targetStateResult;
        }

        if (!targetPath.TryGetParent(out var targetParent))
        {
            return Failure(
                FileSystemOperationFailureKind.UnexpectedEntryKind,
                targetPath,
                "The resolved publication target must have a parent directory.");
        }

        var temporaryPath = CreateTemporaryPath(targetParent, targetPath);
        var temporaryCreated = false;
        try
        {
            await using (var temporaryStream = new FileStream(
                temporaryPath.Value,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                CopyBufferSize,
                FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                temporaryCreated = true;
                await CopyContentsAsync(contents, temporaryStream, cancellationToken).ConfigureAwait(false);
                await temporaryStream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            if (!PhysicalPathResolver.TryResolve(
                    publication.TargetPath,
                    publication.SymbolicLinkHandling,
                    MissingPathHandling.AllowMissingTail,
                    out var currentResolution,
                    out failure))
            {
                return FileSystemOperationResult.FailureResult(failure);
            }

            if (!currentResolution.ResolvedPath.Target.IsSameAs(targetPath))
            {
                return Failure(
                    FileSystemOperationFailureKind.ConcurrentChange,
                    currentResolution.ResolvedPath.Target,
                    "The resolved publication target changed while the temporary file was written.");
            }

            var currentStateResult = ValidateTargetState(
                currentResolution.TargetObservation.State,
                targetPath,
                publication.ExistingTargetHandling);
            if (!currentStateResult.IsSuccess)
            {
                return currentStateResult;
            }

            var publishResult = PublishTemporaryFile(
                temporaryPath,
                targetPath,
                currentResolution.TargetObservation.State,
                publication.ExistingTargetHandling);
            if (publishResult.IsSuccess)
            {
                temporaryCreated = false;
            }

            return publishResult;
        }
        catch (BorrowedStreamReadException exception)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException!).Throw();
            throw;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (UnauthorizedAccessException exception)
        {
            return Failure(FileSystemOperationFailureKind.AccessDenied, targetPath, exception.Message);
        }
        catch (PlatformNotSupportedException exception)
        {
            return Failure(FileSystemOperationFailureKind.PlatformNotSupported, targetPath, exception.Message);
        }
        catch (IOException exception)
        {
            return Failure(FileSystemOperationFailureKind.IoFailure, targetPath, exception.Message);
        }
        finally
        {
            if (temporaryCreated)
            {
                TryDeleteTemporaryFile(temporaryPath);
            }
        }
    }

    private static async ValueTask CopyContentsAsync (
        Stream contents,
        Stream temporaryStream,
        CancellationToken cancellationToken)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(CopyBufferSize);
        try
        {
            while (true)
            {
                int bytesRead;
                try
                {
                    bytesRead = await contents
                        .ReadAsync(buffer.AsMemory(0, CopyBufferSize), cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (Exception exception) when (exception is IOException
                    or UnauthorizedAccessException
                    or PlatformNotSupportedException)
                {
                    // NOTE: The outer operation translates target filesystem failures. Preserve the borrowed
                    // stream's failure source so it is not attributed to the publication path.
                    throw new BorrowedStreamReadException(exception);
                }

                if (bytesRead == 0)
                {
                    return;
                }

                await temporaryStream
                    .WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static FileSystemOperationResult ValidateTargetState (
        FileSystemEntryState state,
        AbsolutePath targetPath,
        ExistingTargetHandling existingTargetHandling)
    {
        if (state == FileSystemEntryState.Missing)
        {
            return FileSystemOperationResult.Success();
        }

        if (state != FileSystemEntryState.RegularFile)
        {
            return Failure(
                FileSystemOperationFailureKind.UnexpectedEntryKind,
                targetPath,
                "A publication target must be missing or a regular file.");
        }

        return existingTargetHandling == ExistingTargetHandling.Reject
            ? Failure(
                FileSystemOperationFailureKind.AlreadyExists,
                targetPath,
                "The publication target already exists.")
            : FileSystemOperationResult.Success();
    }

    private static FileSystemOperationResult TryCreateParent (AbsolutePath parentPath)
    {
        try
        {
            Directory.CreateDirectory(parentPath.Value);
            return FileSystemOperationResult.Success();
        }
        catch (UnauthorizedAccessException exception)
        {
            return Failure(FileSystemOperationFailureKind.AccessDenied, parentPath, exception.Message);
        }
        catch (PlatformNotSupportedException exception)
        {
            return Failure(FileSystemOperationFailureKind.PlatformNotSupported, parentPath, exception.Message);
        }
        catch (IOException exception)
        {
            return Failure(FileSystemOperationFailureKind.IoFailure, parentPath, exception.Message);
        }
    }

    private static FileSystemOperationResult PublishTemporaryFile (
        AbsolutePath temporaryPath,
        AbsolutePath targetPath,
        FileSystemEntryState targetState,
        ExistingTargetHandling existingTargetHandling)
    {
        try
        {
            if (targetState == FileSystemEntryState.Missing)
            {
                try
                {
                    File.Move(temporaryPath.Value, targetPath.Value);
                    return FileSystemOperationResult.Success();
                }
                catch (IOException) when (existingTargetHandling == ExistingTargetHandling.Replace
                    && FileSystemEntryInspector.TryInspect(targetPath, out var observation, out _)
                    && observation.State == FileSystemEntryState.RegularFile)
                {
                    File.Replace(temporaryPath.Value, targetPath.Value, destinationBackupFileName: null, ignoreMetadataErrors: true);
                    return FileSystemOperationResult.Success();
                }
            }

            File.Replace(temporaryPath.Value, targetPath.Value, destinationBackupFileName: null, ignoreMetadataErrors: true);
            return FileSystemOperationResult.Success();
        }
        catch (UnauthorizedAccessException exception)
        {
            return Failure(FileSystemOperationFailureKind.AccessDenied, targetPath, exception.Message);
        }
        catch (PlatformNotSupportedException exception)
        {
            return Failure(FileSystemOperationFailureKind.PlatformNotSupported, targetPath, exception.Message);
        }
        catch (FileNotFoundException exception)
        {
            return Failure(FileSystemOperationFailureKind.ConcurrentChange, targetPath, exception.Message);
        }
        catch (DirectoryNotFoundException exception)
        {
            return Failure(FileSystemOperationFailureKind.ConcurrentChange, targetPath, exception.Message);
        }
        catch (IOException exception)
        {
            var kind = existingTargetHandling == ExistingTargetHandling.Reject
                && FileSystemEntryInspector.TryInspect(targetPath, out var observation, out _)
                && observation.State != FileSystemEntryState.Missing
                    ? FileSystemOperationFailureKind.AlreadyExists
                    : FileSystemOperationFailureKind.IoFailure;
            return Failure(kind, targetPath, exception.Message);
        }
    }

    private static AbsolutePath CreateTemporaryPath (
        AbsolutePath parentPath,
        AbsolutePath targetPath)
    {
        var targetName = Path.GetFileName(targetPath.Value);
        var temporaryName = $".{targetName}.{Guid.NewGuid():N}.tmp";
        return ContainedPath.Create(parentPath, RootRelativePath.Parse(temporaryName)).Target;
    }

    private static bool TryGetParent (
        ContainedPath targetPath,
        out ContainedPath parentPath)
    {
        if (!targetPath.Target.TryGetParent(out var absoluteParent)
            || !ContainedPath.TryCreate(targetPath.BoundaryRoot, absoluteParent, out var containedParent, out _))
        {
            parentPath = null!;
            return false;
        }

        parentPath = containedParent;
        return true;
    }

    private static FileSystemOperationResult Failure (
        FileSystemOperationFailureKind kind,
        AbsolutePath path,
        string message)
    {
        return FileSystemOperationResult.FailureResult(
            FileSystemOperationFailure.Create(kind, path, message));
    }

    private static void TryDeleteTemporaryFile (AbsolutePath temporaryPath)
    {
        try
        {
            File.Delete(temporaryPath.Value);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // NOTE: The publication result is authoritative; an unpublished temporary sibling can be cleaned up independently.
        }
    }

    private sealed class BorrowedStreamReadException : Exception
    {
        internal BorrowedStreamReadException (Exception innerException)
            : base("The borrowed stream could not be read.", innerException)
        {
        }
    }
}
