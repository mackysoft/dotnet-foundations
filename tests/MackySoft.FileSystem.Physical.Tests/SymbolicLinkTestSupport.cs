using Xunit.Sdk;

namespace MackySoft.FileSystem.Physical.Tests;

internal static class SymbolicLinkTestSupport
{
    private const int PrivilegeNotHeldError = 1314;

    public static void CreateFile (
        string linkPath,
        string targetPath)
    {
        Create(
            () => File.CreateSymbolicLink(linkPath, targetPath),
            "file symbolic links");
    }

    public static void CreateDirectory (
        string linkPath,
        string targetPath)
    {
        Create(
            () => Directory.CreateSymbolicLink(linkPath, targetPath),
            "directory symbolic links");
    }

    private static void Create (
        Action createLink,
        string capability)
    {
        try
        {
            createLink();
        }
        catch (UnauthorizedAccessException exception) when (CanSkipUnavailableWindowsCapability())
        {
            throw SkipException.ForSkip(
                $"The Windows test environment cannot create {capability}: {exception.Message}");
        }
        catch (IOException exception) when (CanSkipUnavailableWindowsCapability()
            && (exception.HResult & 0xFFFF) == PrivilegeNotHeldError)
        {
            throw SkipException.ForSkip(
                $"The Windows test environment cannot create {capability}: {exception.Message}");
        }
    }

    private static bool CanSkipUnavailableWindowsCapability ()
    {
        return OperatingSystem.IsWindows()
            && !string.Equals(
                Environment.GetEnvironmentVariable("GITHUB_ACTIONS"),
                "true",
                StringComparison.OrdinalIgnoreCase);
    }
}
