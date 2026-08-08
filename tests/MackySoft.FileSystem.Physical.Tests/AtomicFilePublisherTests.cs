using System.Text;

namespace MackySoft.FileSystem.Physical.Tests;

public sealed class AtomicFilePublisherTests
{
    [Fact]
    [Trait("Size", "Small")]
    public async Task PublishAsync_CreatesCompleteFileAndLeavesBorrowedStreamOpen ()
    {
        using var scope = TemporaryDirectory.Create();
        var target = scope.Resolve("nested/file.txt");
        var publication = new AtomicFilePublication(
            target,
            SymbolicLinkHandling.Reject,
            ExistingTargetHandling.Reject,
            MissingParentHandling.Create);
        using var contents = new MemoryStream(Encoding.UTF8.GetBytes("complete contents"));

        var result = await AtomicFilePublisher.PublishAsync(publication, contents, CancellationToken.None);

        Assert.True(result.IsSuccess, result.Failure.Message);
        Assert.Equal("complete contents", File.ReadAllText(target.Target.Value));
        Assert.True(contents.CanRead);
    }

    [Fact]
    [Trait("Size", "Small")]
    public async Task PublishAsync_RejectsExistingTargetWithoutChangingIt ()
    {
        using var scope = TemporaryDirectory.Create();
        var target = scope.Resolve("file.txt");
        File.WriteAllText(target.Target.Value, "original");
        var publication = new AtomicFilePublication(
            target,
            SymbolicLinkHandling.Reject,
            ExistingTargetHandling.Reject,
            MissingParentHandling.Reject);
        using var contents = new MemoryStream(Encoding.UTF8.GetBytes("replacement"));

        var result = await AtomicFilePublisher.PublishAsync(publication, contents, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(FileSystemOperationFailureKind.AlreadyExists, result.Failure.Kind);
        Assert.Equal("original", File.ReadAllText(target.Target.Value));
    }

    [Fact]
    [Trait("Size", "Small")]
    public async Task PublishAsync_ReplacesExistingRegularFile ()
    {
        using var scope = TemporaryDirectory.Create();
        var target = scope.Resolve("file.txt");
        File.WriteAllText(target.Target.Value, "original");
        var publication = new AtomicFilePublication(
            target,
            SymbolicLinkHandling.Reject,
            ExistingTargetHandling.Replace,
            MissingParentHandling.Reject);
        using var contents = new MemoryStream(Encoding.UTF8.GetBytes("replacement"));

        var result = await AtomicFilePublisher.PublishAsync(publication, contents, CancellationToken.None);

        Assert.True(result.IsSuccess, result.Failure.Message);
        Assert.Equal("replacement", File.ReadAllText(target.Target.Value));
    }

    [Fact]
    [Trait("Size", "Small")]
    public async Task PublishAsync_RejectsMissingParentWhenCreationIsDisabled ()
    {
        using var scope = TemporaryDirectory.Create();
        var target = scope.Resolve("missing/file.txt");
        var publication = new AtomicFilePublication(
            target,
            SymbolicLinkHandling.Reject,
            ExistingTargetHandling.Reject,
            MissingParentHandling.Reject);
        using var contents = new MemoryStream(Encoding.UTF8.GetBytes("contents"));

        var result = await AtomicFilePublisher.PublishAsync(publication, contents, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(FileSystemOperationFailureKind.EntryNotFound, result.Failure.Kind);
        Assert.False(File.Exists(target.Target.Value));
    }

    [Fact]
    [Trait("Size", "Small")]
    public async Task PublishAsync_ObservesPreCanceledTokenBeforeCreatingParent ()
    {
        using var scope = TemporaryDirectory.Create();
        var target = scope.Resolve("missing/file.txt");
        var publication = new AtomicFilePublication(
            target,
            SymbolicLinkHandling.Reject,
            ExistingTargetHandling.Reject,
            MissingParentHandling.Create);
        using var contents = new MemoryStream(Encoding.UTF8.GetBytes("contents"));
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await AtomicFilePublisher.PublishAsync(publication, contents, cancellationSource.Token));

        Assert.False(Directory.Exists(Path.GetDirectoryName(target.Target.Value)));
    }

    [Fact]
    [Trait("Size", "Small")]
    public async Task PublishAsync_PropagatesBorrowedStreamReadFailureAndCleansTemporaryFile ()
    {
        using var scope = TemporaryDirectory.Create();
        var target = scope.Resolve("nested/file.txt");
        var targetParent = Path.GetDirectoryName(target.Target.Value)!;
        var publication = new AtomicFilePublication(
            target,
            SymbolicLinkHandling.Reject,
            ExistingTargetHandling.Reject,
            MissingParentHandling.Create);
        var readFailure = new IOException("Borrowed stream read failed.");
        using var contents = new FailingReadStream(readFailure);

        var observedFailure = await Assert.ThrowsAsync<IOException>(
            async () => await AtomicFilePublisher.PublishAsync(publication, contents, CancellationToken.None));

        Assert.Same(readFailure, observedFailure);
        Assert.True(contents.CanRead);
        Assert.True(Directory.Exists(targetParent));
        Assert.Empty(Directory.EnumerateFileSystemEntries(targetParent));
    }

    [Fact]
    [Trait("Size", "Small")]
    public async Task PublishAsync_CleansTemporaryFileAndRetainsCreatedParentWhenCopyIsCanceled ()
    {
        using var scope = TemporaryDirectory.Create();
        var target = scope.Resolve("nested/file.txt");
        var targetParent = Path.GetDirectoryName(target.Target.Value)!;
        var publication = new AtomicFilePublication(
            target,
            SymbolicLinkHandling.Reject,
            ExistingTargetHandling.Reject,
            MissingParentHandling.Create);
        using var cancellationSource = new CancellationTokenSource();
        using var contents = new CancelBeforeSecondReadStream(
            Encoding.UTF8.GetBytes("partial contents"),
            cancellationSource);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await AtomicFilePublisher.PublishAsync(publication, contents, cancellationSource.Token));

        Assert.True(contents.CanRead);
        Assert.True(Directory.Exists(targetParent));
        Assert.Empty(Directory.EnumerateFileSystemEntries(targetParent));
    }

    [Fact]
    [Trait("Size", "Small")]
    public async Task PublishAsync_FollowsFinalLinkAndPublishesBesideResolvedTarget ()
    {
        using var scope = TemporaryDirectory.Create();
        var actualDirectory = scope.Resolve("actual").Target;
        var actualTarget = scope.Resolve("actual/file.txt").Target;
        var linkedTarget = scope.Resolve("linked-file.txt");
        Directory.CreateDirectory(actualDirectory.Value);
        File.WriteAllText(actualTarget.Value, "original");
        SymbolicLinkTestSupport.CreateFile(linkedTarget.Target.Value, actualTarget.Value);
        var publication = new AtomicFilePublication(
            linkedTarget,
            SymbolicLinkHandling.Follow,
            ExistingTargetHandling.Replace,
            MissingParentHandling.Reject);
        using var contents = new MemoryStream(Encoding.UTF8.GetBytes("replacement"));

        var result = await AtomicFilePublisher.PublishAsync(publication, contents, CancellationToken.None);

        Assert.True(result.IsSuccess, result.Failure.Message);
        Assert.Equal("replacement", File.ReadAllText(actualTarget.Value));
        Assert.True(FileSystemEntryInspector.TryInspect(linkedTarget.Target, out var observation, out var failure), failure.Message);
        Assert.NotNull(observation);
        Assert.Equal(FileSystemEntryState.SymbolicLink, observation.State);
    }

    private sealed class FailingReadStream : MemoryStream
    {
        private readonly IOException failure;

        internal FailingReadStream (IOException failure)
        {
            this.failure = failure;
        }

        public override ValueTask<int> ReadAsync (
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromException<int>(failure);
        }
    }

    private sealed class CancelBeforeSecondReadStream : MemoryStream
    {
        private readonly CancellationTokenSource cancellationSource;
        private bool completedFirstRead;

        internal CancelBeforeSecondReadStream (
            byte[] buffer,
            CancellationTokenSource cancellationSource)
            : base(buffer)
        {
            this.cancellationSource = cancellationSource;
        }

        public override ValueTask<int> ReadAsync (
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            if (completedFirstRead)
            {
                cancellationSource.Cancel();
                cancellationToken.ThrowIfCancellationRequested();
            }

            completedFirstRead = true;
            return base.ReadAsync(buffer, cancellationToken);
        }
    }
}
