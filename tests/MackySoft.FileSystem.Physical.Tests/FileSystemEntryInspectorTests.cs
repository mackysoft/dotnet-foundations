using System.Net.Sockets;

namespace MackySoft.FileSystem.Physical.Tests;

public sealed class FileSystemEntryInspectorTests
{
    [Theory]
    [Trait("Size", "Small")]
    [InlineData(false, FileSystemEntryState.RegularFile)]
    [InlineData(true, FileSystemEntryState.Directory)]
    public void TryInspect_ClassifiesExistingRegularEntries (
        bool createDirectory,
        FileSystemEntryState expectedState)
    {
        using var scope = TemporaryDirectory.Create();
        var path = scope.Resolve("entry").Target;
        if (createDirectory)
        {
            Directory.CreateDirectory(path.Value);
        }
        else
        {
            File.WriteAllText(path.Value, "contents");
        }

        var succeeded = FileSystemEntryInspector.TryInspect(path, out var observation, out var failure);

        Assert.True(succeeded, failure.Message);
        Assert.NotNull(observation);
        Assert.Equal(expectedState, observation.State);
        Assert.Equal(path, observation.Path);
        Assert.Equal(FileSystemOperationFailureKind.None, failure.Kind);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void TryInspect_ClassifiesMissingEntryAsSuccessfulObservation ()
    {
        using var scope = TemporaryDirectory.Create();
        var path = scope.Resolve("missing").Target;

        var succeeded = FileSystemEntryInspector.TryInspect(path, out var observation, out var failure);

        Assert.True(succeeded, failure.Message);
        Assert.NotNull(observation);
        Assert.Equal(FileSystemEntryState.Missing, observation.State);
        Assert.Equal(FileSystemOperationFailureKind.None, failure.Kind);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void TryInspect_DoesNotFollowSymbolicLink ()
    {
        using var scope = TemporaryDirectory.Create();
        var target = scope.Resolve("target").Target;
        var link = scope.Resolve("link").Target;
        File.WriteAllText(target.Value, "contents");
        SymbolicLinkTestSupport.CreateFile(link.Value, target.Value);

        var succeeded = FileSystemEntryInspector.TryInspect(link, out var observation, out var failure);

        Assert.True(succeeded, failure.Message);
        Assert.NotNull(observation);
        Assert.Equal(FileSystemEntryState.SymbolicLink, observation.State);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void TryInspect_ClassifiesDanglingSymbolicLink ()
    {
        using var scope = TemporaryDirectory.Create();
        var link = scope.Resolve("link").Target;
        SymbolicLinkTestSupport.CreateFile(link.Value, "missing-target");

        var succeeded = FileSystemEntryInspector.TryInspect(link, out var observation, out var failure);

        Assert.True(succeeded, failure.Message);
        Assert.NotNull(observation);
        Assert.Equal(FileSystemEntryState.SymbolicLink, observation.State);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void TryInspect_ClassifiesUnixDomainSocketAsOther ()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        using var scope = TemporaryDirectory.Create();
        var socketPath = scope.Resolve("socket").Target;
        using var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        socket.Bind(new UnixDomainSocketEndPoint(socketPath.Value));

        var succeeded = FileSystemEntryInspector.TryInspect(socketPath, out var observation, out var failure);

        Assert.True(succeeded, failure.Message);
        Assert.NotNull(observation);
        Assert.Equal(FileSystemEntryState.Other, observation.State);
    }
}
