using System.Text.RegularExpressions;

namespace MackySoft.FileSystem.Tests;

public sealed class FileSystemProviderBoundaryTests
{
    private static readonly Regex MutableFileSystemIoPattern = new(
        @"\b(?:(?:Directory|File)\s*\.|(?:new\s+)?(?:DirectoryInfo|FileInfo|FileStream|FileSystemInfo|FileSystemWatcher)\b)",
        RegexOptions.CultureInvariant);

    private static readonly Regex ProductSpecificVocabularyPattern = new(
        @"\b(?:Dotmet|IPC|Ucli|Unity)\b",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly Regex RawFactoryReentryPattern = new(
        @"\b(?:AbsolutePath|ContainedPath|RootRelativePath)\s*\.\s*(?:Parse|Resolve|TryParse|TryResolve)\s*\(",
        RegexOptions.CultureInvariant);

    private static readonly Regex RuntimePathExpansionPattern = new(
        @"(?:\bPath\s*\.\s*GetRelativePath\s*\(|\bGetLongPathName\w*\s*\()",
        RegexOptions.CultureInvariant);

    private static readonly Regex RuntimeFullPathPattern = new(
        @"\bPath\s*\.\s*GetFullPath\s*\(",
        RegexOptions.CultureInvariant);

    [Fact]
    [Trait("Size", "Small")]
    public void ProviderSource_DoesNotOwnMutableFileSystemOperations ()
    {
        AssertNoSourceMatch(
            MutableFileSystemIoPattern,
            "Guarded path values must not own mutable filesystem I/O.");
    }

    [Fact]
    [Trait("Size", "Small")]
    public void ProviderSource_DoesNotOwnProductVocabulary ()
    {
        AssertNoSourceMatch(
            ProductSpecificVocabularyPattern,
            "The provider must remain independent of product domains and transports.");
    }

    [Fact]
    [Trait("Size", "Small")]
    public void GuardedDerivations_DoNotReenterPublicRawFactories ()
    {
        AssertNoSourceMatch(
            RawFactoryReentryPattern,
            "Typed derivation must use trusted construction instead of a public raw-text factory.");
    }

    [Fact]
    [Trait("Size", "Small")]
    public void ProviderSource_DoesNotUseRuntimePathExpansion ()
    {
        AssertNoSourceMatch(
            RuntimePathExpansionPattern,
            "Provider normalization must not query the filesystem to expand Windows short names.");
    }

    [Fact]
    [Trait("Size", "Small")]
    public void RuntimeFullPathNormalization_IsRestrictedToUnixPlatformPolicy ()
    {
        var matches = EnumerateProviderSourceFiles()
            .SelectMany(path =>
                RuntimeFullPathPattern
                    .Matches(File.ReadAllText(path))
                    .Cast<Match>()
                    .Select(_ => Path.GetRelativePath(FindRepositoryRoot(), path)))
            .ToArray();

        Assert.Equal(2, matches.Length);
        Assert.All(
            matches,
            path => Assert.Equal(
                Path.Combine(
                    "src",
                    "MackySoft.FileSystem",
                    "Internal",
                    "PlatformPath.cs"),
                path));
    }

    private static void AssertNoSourceMatch (
        Regex pattern,
        string message)
    {
        var violations = EnumerateProviderSourceFiles()
            .SelectMany(path =>
                pattern
                    .Matches(File.ReadAllText(path))
                    .Cast<Match>()
                    .Select(match => $"{Path.GetRelativePath(FindRepositoryRoot(), path)}: {match.Value}"))
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"{message}{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    private static IEnumerable<string> EnumerateProviderSourceFiles ()
    {
        return Directory.EnumerateFiles(
            Path.Combine(FindRepositoryRoot(), "src", "MackySoft.FileSystem"),
            "*.cs",
            SearchOption.AllDirectories);
    }

    private static string FindRepositoryRoot ()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "DotNetFoundations.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "dotnet-foundations repository root was not found from the test output directory.");
    }
}
