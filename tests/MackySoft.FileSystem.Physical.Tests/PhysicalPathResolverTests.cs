namespace MackySoft.FileSystem.Physical.Tests;

public sealed class PhysicalPathResolverTests
{
    [Fact]
    [Trait("Size", "Small")]
    public void TryResolve_AllowsMissingTailWhenRequested ()
    {
        using var scope = TemporaryDirectory.Create();
        var requestedPath = scope.Resolve("missing/child.txt");

        var succeeded = PhysicalPathResolver.TryResolve(
            requestedPath,
            SymbolicLinkHandling.Reject,
            MissingPathHandling.AllowMissingTail,
            out var resolution,
            out var failure);

        Assert.True(succeeded, failure.Message);
        Assert.NotNull(resolution);
        Assert.Equal(requestedPath.RelativePath, resolution.ResolvedPath.RelativePath);
        Assert.Equal(FileSystemEntryState.Missing, resolution.TargetObservation.State);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void TryResolve_RejectsMissingTailWhenRequired ()
    {
        using var scope = TemporaryDirectory.Create();
        var requestedPath = scope.Resolve("missing/child.txt");

        var succeeded = PhysicalPathResolver.TryResolve(
            requestedPath,
            SymbolicLinkHandling.Reject,
            MissingPathHandling.Reject,
            out var resolution,
            out var failure);

        Assert.False(succeeded);
        Assert.Null(resolution);
        Assert.Equal(FileSystemOperationFailureKind.EntryNotFound, failure.Kind);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void TryResolve_RejectsExistingSymbolicLinkSegment ()
    {
        using var scope = TemporaryDirectory.Create();
        var targetDirectory = scope.Resolve("target").Target;
        var linkDirectory = scope.Resolve("link").Target;
        Directory.CreateDirectory(targetDirectory.Value);
        SymbolicLinkTestSupport.CreateDirectory(linkDirectory.Value, targetDirectory.Value);
        var requestedPath = scope.Resolve("link/file.txt");

        var succeeded = PhysicalPathResolver.TryResolve(
            requestedPath,
            SymbolicLinkHandling.Reject,
            MissingPathHandling.AllowMissingTail,
            out _,
            out var failure);

        Assert.False(succeeded);
        Assert.Equal(FileSystemOperationFailureKind.LinkNotAllowed, failure.Kind);
        Assert.Equal(Path.GetFileName(linkDirectory.Value), Path.GetFileName(failure.Path!.Value));
    }

    [Fact]
    [Trait("Size", "Small")]
    public void TryResolve_FollowsSymbolicLinkThatRemainsUnderResolvedBoundary ()
    {
        using var scope = TemporaryDirectory.Create();
        var targetDirectory = scope.Resolve("target").Target;
        var linkDirectory = scope.Resolve("link").Target;
        Directory.CreateDirectory(targetDirectory.Value);
        SymbolicLinkTestSupport.CreateDirectory(linkDirectory.Value, targetDirectory.Value);
        var requestedPath = scope.Resolve("link/file.txt");

        var succeeded = PhysicalPathResolver.TryResolve(
            requestedPath,
            SymbolicLinkHandling.Follow,
            MissingPathHandling.AllowMissingTail,
            out var resolution,
            out var failure);

        Assert.True(succeeded, failure.Message);
        Assert.NotNull(resolution);
        Assert.Equal("target/file.txt", resolution.ResolvedPath.RelativePath.Value);
        Assert.Equal(FileSystemEntryState.Missing, resolution.TargetObservation.State);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void TryResolve_RejectsSymbolicLinkThatLeavesResolvedBoundary ()
    {
        using var scope = TemporaryDirectory.Create();
        using var outsideScope = TemporaryDirectory.Create();
        var linkDirectory = scope.Resolve("link").Target;
        SymbolicLinkTestSupport.CreateDirectory(linkDirectory.Value, outsideScope.FullPath);
        var requestedPath = scope.Resolve("link/file.txt");

        var succeeded = PhysicalPathResolver.TryResolve(
            requestedPath,
            SymbolicLinkHandling.Follow,
            MissingPathHandling.AllowMissingTail,
            out _,
            out var failure);

        Assert.False(succeeded);
        Assert.Equal(FileSystemOperationFailureKind.OutsideBoundary, failure.Kind);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void TryResolve_RejectsSymbolicLinkBoundaryWhenLinksAreDisabled ()
    {
        using var scope = TemporaryDirectory.Create();
        var actualRoot = scope.Resolve("actual").Target;
        var linkedRoot = scope.Resolve("linked").Target;
        Directory.CreateDirectory(actualRoot.Value);
        SymbolicLinkTestSupport.CreateDirectory(linkedRoot.Value, actualRoot.Value);
        var requestedPath = ContainedPath.Resolve(linkedRoot, "file.txt");

        var succeeded = PhysicalPathResolver.TryResolve(
            requestedPath,
            SymbolicLinkHandling.Reject,
            MissingPathHandling.AllowMissingTail,
            out _,
            out var failure);

        Assert.False(succeeded);
        Assert.Equal(FileSystemOperationFailureKind.LinkNotAllowed, failure.Kind);
        Assert.Equal(linkedRoot, failure.Path);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void TryResolve_ClassifiesSymbolicLinkCycle ()
    {
        using var scope = TemporaryDirectory.Create();
        var firstLink = scope.Resolve("first").Target;
        var secondLink = scope.Resolve("second").Target;
        SymbolicLinkTestSupport.CreateFile(firstLink.Value, secondLink.Value);
        SymbolicLinkTestSupport.CreateFile(secondLink.Value, firstLink.Value);

        var succeeded = PhysicalPathResolver.TryResolve(
            scope.Resolve("first"),
            SymbolicLinkHandling.Follow,
            MissingPathHandling.Reject,
            out _,
            out var failure);

        Assert.False(succeeded);
        Assert.Equal(FileSystemOperationFailureKind.LinkCycle, failure.Kind);
    }
}
