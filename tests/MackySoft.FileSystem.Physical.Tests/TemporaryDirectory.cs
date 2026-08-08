namespace MackySoft.FileSystem.Physical.Tests;

internal sealed class TemporaryDirectory : IDisposable
{
    private TemporaryDirectory (string fullPath)
    {
        FullPath = fullPath;
        Path = AbsolutePath.Parse(fullPath);
    }

    internal string FullPath { get; }

    internal AbsolutePath Path { get; }

    internal static TemporaryDirectory Create ()
    {
        var directory = Directory.CreateTempSubdirectory("MackySoft.FileSystem.Physical.Tests.");
        return new TemporaryDirectory(directory.FullName);
    }

    internal ContainedPath Resolve (string relativePath)
    {
        return ContainedPath.Resolve(Path, relativePath);
    }

    public void Dispose ()
    {
        Directory.Delete(FullPath, recursive: true);
    }
}
