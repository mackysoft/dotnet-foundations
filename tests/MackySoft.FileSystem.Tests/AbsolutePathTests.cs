using System.Reflection;

namespace MackySoft.FileSystem.Tests;

public sealed class AbsolutePathTests
{
    [Fact]
    [Trait("Size", "Small")]
    public void Parse_NormalizesSeparatorsSegmentsAndTrailingSeparator ()
    {
        var expected = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "guarded-path", "file.txt"));
        var input = Path.Combine(Path.GetTempPath(), "guarded-path", "nested", "..", "file.txt")
            + Path.DirectorySeparatorChar;

        var path = AbsolutePath.Parse(input);

        Assert.Equal(expected, path.Value);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Parse_PreservesFileSystemRootSeparator ()
    {
        var root = Path.GetPathRoot(Path.GetFullPath("."))!;

        var path = AbsolutePath.Parse(root);

        Assert.Equal(root, path.Value);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Parse_PreservesBackslashFilenameCharacter_OnUnix ()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        var input = Path.Combine(Path.GetTempPath(), @"directory\file.txt");

        var path = AbsolutePath.Parse(input);

        Assert.EndsWith(@"directory\file.txt", path.Value, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Parse_OnWindows_NormalizesForwardSlashAndUsesCaseInsensitiveIdentity ()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        const string root = @"C:\";
        var forwardSlashInput = root.Replace('\\', '/') + "Guarded/Path/File.txt";
        var differentlyCasedInput = root + @"guarded\path\file.TXT";

        var normalized = AbsolutePath.Parse(forwardSlashInput);
        var differentlyCased = AbsolutePath.Parse(differentlyCasedInput);

        Assert.DoesNotContain('/', normalized.Value);
        Assert.Equal(@"C:\Guarded\Path\File.txt", normalized.Value);
        Assert.Equal(@"C:\guarded\path\file.TXT", differentlyCased.Value);
        Assert.Equal(normalized, differentlyCased);
        Assert.Equal(normalized.GetHashCode(), differentlyCased.GetHashCode());
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Parse_OnWindows_PreservesShortNameLookingSegmentsWithoutLongNameExpansion ()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var shortNameRoot = Directory.Exists(@"C:\PROGRA~1")
            ? @"C:\PROGRA~1"
            : @"C:\NONEXI~1";
        var input = shortNameRoot + @"\Mixed~Case\File.TXT";

        var path = AbsolutePath.Parse(input);

        Assert.Equal(input, path.Value);
    }

    [Theory]
    [Trait("Size", "Small")]
    [InlineData(@"C:\guarded\name ", @"C:\guarded\name")]
    [InlineData(@"\\server\share\name...", @"\\server\share\name")]
    public void Parse_OnWindows_NormalizesSeparatorFreeOrdinaryEndpoint (
        string input,
        string expected)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var path = AbsolutePath.Parse(input);

        Assert.Equal(expected, path.Value);
    }

    [Theory]
    [Trait("Size", "Small")]
    [InlineData(@"C:\guarded\name \.", @"C:\guarded\name")]
    [InlineData(@"C:\guarded\name \child\..", @"C:\guarded\name")]
    public void Parse_OnWindows_NormalizesComponentPromotedToEndpointByNavigation (
        string input,
        string expected)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var path = AbsolutePath.Parse(input);

        Assert.Equal(expected, path.Value);
        Assert.Equal(path, AbsolutePath.Parse(path.Value));
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Parse_OnWindows_PreservesDriveAndUncShareRoots ()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        const string driveRoot = @"C:\";
        const string uncShareRoot = @"\\server\share";
        const string uncShareRootWithTrailingSeparator = @"\\server\share\";

        Assert.Equal(driveRoot, AbsolutePath.Parse(driveRoot).Value);
        var normalizedUncShareRoot = AbsolutePath.Parse(uncShareRoot);
        var normalizedUncShareRootWithTrailingSeparator = AbsolutePath.Parse(
            uncShareRootWithTrailingSeparator);
        Assert.Equal(uncShareRoot, normalizedUncShareRoot.Value);
        Assert.Equal(normalizedUncShareRoot, normalizedUncShareRootWithTrailingSeparator);
        Assert.Equal(
            normalizedUncShareRoot.GetHashCode(),
            normalizedUncShareRootWithTrailingSeparator.GetHashCode());
    }

    [Theory]
    [Trait("Size", "Small")]
    [InlineData("share ", "share")]
    [InlineData("share... ", "share")]
    public void Parse_OnWindows_NormalizesSeparatorFreeUncShareEndpoint (
        string inputShare,
        string expectedShare)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var expectedValue = $@"\\Server\{expectedShare}";
        var path = AbsolutePath.Parse(
            $@"\\Server\{inputShare}");

        Assert.Equal(expectedValue, path.Value);
        Assert.Equal(path, AbsolutePath.Parse(path.Value));
        Assert.Equal(path, path.GetRoot());
    }

    [Theory]
    [Trait("Size", "Small")]
    [InlineData("share ")]
    [InlineData("share... ")]
    public void TryParse_OnWindows_RejectsUncShareRetainedBeforeSeparator (
        string inputShare)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        foreach (var input in new[]
        {
            $@"\\Server\{inputShare}" + Path.DirectorySeparatorChar,
            $@"\\Server\{inputShare}\Guarded\Child",
        })
        {
            Assert.False(AbsolutePath.TryParse(
                input,
                out var path,
                out var failure));
            Assert.Null(path);
            Assert.Equal(
                PathValidationFailureKind.InvalidPathFormat,
                failure.Kind);
        }
    }

    [Theory]
    [Trait("Size", "Small")]
    [InlineData("   ")]
    [InlineData("...")]
    public void TryParse_OnWindows_RejectsUncShareRemovedByEndpointNormalization (
        string inputShare)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var inputs = new[]
        {
            $@"\\server\{inputShare}",
            $@"\\server\{inputShare}" + Path.DirectorySeparatorChar,
            $@"\\server\{inputShare}\child",
        };

        foreach (var input in inputs)
        {
            Assert.False(AbsolutePath.TryParse(
                input,
                out var path,
                out var failure));
            Assert.Null(path);
            Assert.Equal(
                PathValidationFailureKind.InvalidPathFormat,
                failure.Kind);
        }
    }

    [Fact]
    [Trait("Size", "Small")]
    public void TryParse_OnWindows_RejectsDriveRelativeInput ()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        Assert.False(AbsolutePath.TryParse(
            @"C:relative\file.txt",
            out var path,
            out var failure));
        Assert.Null(path);
        Assert.Equal(PathValidationFailureKind.ExpectedAbsolutePath, failure.Kind);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void TryResolve_OnWindows_RejectsPartiallyQualifiedRootedInput ()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var basePath = AbsolutePath.Parse(@"C:\guarded\base");

        foreach (var input in new[] { @"C:relative\file.txt", @"\current-drive-rooted\file.txt" })
        {
            Assert.False(AbsolutePath.TryResolve(
                basePath,
                input,
                out var path,
                out var failure));
            Assert.Null(path);
            Assert.Equal(PathValidationFailureKind.ExpectedAbsolutePath, failure.Kind);
        }
    }

    [Fact]
    [Trait("Size", "Small")]
    public void TryResolve_OnWindows_NormalizesSeparatorFreeRelativeEndpoint ()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var basePath = AbsolutePath.Parse(@"C:\guarded\base");

        Assert.True(AbsolutePath.TryResolve(
            basePath,
            "name ",
            out var path,
            out var failure));
        Assert.Equal(PathValidationFailureKind.None, failure.Kind);
        Assert.Equal(AbsolutePath.Parse(@"C:\guarded\base\name"), path);
    }

    [Theory]
    [Trait("Size", "Small")]
    [InlineData("name /")]
    [InlineData("directory /child")]
    public void TryResolve_OnWindows_RejectsRelativeComponentRetainedBeforeSeparator (
        string input)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var basePath = AbsolutePath.Parse(@"C:\guarded\base");

        Assert.False(AbsolutePath.TryResolve(
            basePath,
            input,
            out var path,
            out var failure));
        Assert.Null(path);
        Assert.Equal(PathValidationFailureKind.InvalidPathFormat, failure.Kind);
    }

    [Theory]
    [Trait("Size", "Small")]
    [InlineData(@"\\?\C:\guarded\child\..\target")]
    [InlineData(@"\\.\C:\guarded\target")]
    [InlineData(@"\??\C:\guarded\target")]
    [InlineData("//?/C:/guarded/target")]
    public void TryParse_OnWindows_RejectsDeviceNamespaceInput (string input)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        Assert.False(AbsolutePath.TryParse(
            input,
            out var path,
            out var failure));
        Assert.Null(path);
        Assert.Equal(PathValidationFailureKind.InvalidPathFormat, failure.Kind);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Identity_OnUnix_IsCaseSensitiveAndPreservesSlashRoot ()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        var upperCasePath = AbsolutePath.Parse("/guarded/Case");
        var lowerCasePath = AbsolutePath.Parse("/guarded/case");

        Assert.Equal("/", AbsolutePath.Parse("/").Value);
        Assert.NotEqual(upperCasePath, lowerCasePath);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Parse_OnUnix_PreservesWhitespaceSegments ()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        var input = Path.Combine(
            Path.GetTempPath(),
            " ",
            "file name.txt");

        var path = AbsolutePath.Parse(input);

        Assert.Equal(input, path.Value);
    }

    [Theory]
    [Trait("Size", "Small")]
    [InlineData(@"C:\invalid*name")]
    [InlineData(@"C:\invalid?name")]
    [InlineData("C:\\invalid\"name")]
    [InlineData(@"C:\invalid<name")]
    [InlineData(@"C:\invalid>name")]
    [InlineData(@"C:\invalid|name")]
    [InlineData("C:\\invalid\tname")]
    [InlineData(@"C:\file:stream")]
    [InlineData(@"C:\CON")]
    [InlineData(@"C:\nul.txt")]
    [InlineData(@"C:\CON .txt")]
    [InlineData(@"C:\COM1 .log")]
    [InlineData(@"C:\COM¹.log")]
    [InlineData(@"C:\LPT³... ")]
    [InlineData(@"C:\folder\. ")]
    [InlineData(@"C:\folder\.. ")]
    [InlineData(@"C:\folder\...")]
    public void TryParse_OnWindows_RejectsNonOrdinarySegments (string input)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        Assert.False(AbsolutePath.TryParse(
            input,
            out var path,
            out var failure));
        Assert.Null(path);
        Assert.Equal(PathValidationFailureKind.InvalidPathFormat, failure.Kind);
    }

    [Theory]
    [Trait("Size", "Small")]
    [InlineData(@"\\")]
    [InlineData(@"\\server")]
    [InlineData(@"\\server\")]
    [InlineData(@"\\server\\")]
    [InlineData(@"\\server\..")]
    public void TryParse_OnWindows_RejectsUncWithoutCompleteShareRoot (string input)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        Assert.False(AbsolutePath.TryParse(
            input,
            out var path,
            out var failure));
        Assert.Null(path);
        Assert.Equal(PathValidationFailureKind.InvalidPathFormat, failure.Kind);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Parse_OnWindows_DoesNotApplyRemoteUncProviderNamingPolicy ()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        const string ordinaryPath = @"C:\folder\name[1]+value=2;part,tail";
        var remotePolicyShare = new string('s', 81) + "[1]+value=2;part,tail";
        var uncPaths = new[]
        {
            @"\\server name\share\child",
            $@"\\server\{remotePolicyShare}\child",
        };

        Assert.Equal(ordinaryPath, AbsolutePath.Parse(ordinaryPath).Value);
        Assert.All(
            uncPaths,
            path => Assert.Equal(path, AbsolutePath.Parse(path).Value));
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Value_PreservesInputCasingUsedAtSystemIoBoundary ()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            "guarded-value-casing",
            Guid.NewGuid().ToString("N"));
        var input = Path.Combine(root, "MixedCase.txt");
        Directory.CreateDirectory(root);
        File.WriteAllText(input, "content");

        try
        {
            var path = AbsolutePath.Parse(input);

            Assert.Equal(Path.GetFullPath(input), path.Value);
            Assert.Equal("content", File.ReadAllText(path.Value));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    [Trait("Size", "Small")]
    public void TryParse_ClassifiesEmptyRelativeAndInvalidInputs ()
    {
        Assert.False(AbsolutePath.TryParse(null, out var nullPath, out var nullFailure));
        Assert.Null(nullPath);
        Assert.Equal(PathValidationFailureKind.EmptyPath, nullFailure.Kind);

        Assert.False(AbsolutePath.TryParse("relative/path", out var relativePath, out var relativeFailure));
        Assert.Null(relativePath);
        Assert.Equal(PathValidationFailureKind.ExpectedAbsolutePath, relativeFailure.Kind);

        Assert.False(AbsolutePath.TryParse("invalid\0path", out var invalidPath, out var invalidFailure));
        Assert.Null(invalidPath);
        Assert.Equal(PathValidationFailureKind.InvalidPathFormat, invalidFailure.Kind);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void TryResolve_ResolvesRelativeInputFromGuardedBase ()
    {
        var basePath = AbsolutePath.Parse(
            Path.Combine(Path.GetTempPath(), "guarded-resolution", "base"));

        var success = AbsolutePath.TryResolve(
            basePath,
            Path.Combine("..", "target"),
            out var result,
            out var failure);

        Assert.True(success);
        Assert.Equal(PathValidationFailureKind.None, failure.Kind);
        Assert.Equal(
            AbsolutePath.Parse(Path.Combine(Path.GetTempPath(), "guarded-resolution", "target")),
            result);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void EqualityAndContainment_UseNormalizedPlatformIdentity ()
    {
        var rootText = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "guarded-path-root"));
        var root = AbsolutePath.Parse(rootText);
        var same = AbsolutePath.Parse(rootText + Path.DirectorySeparatorChar);
        var child = AbsolutePath.Parse(Path.Combine(rootText, "child", "file.txt"));
        var sibling = AbsolutePath.Parse(rootText + "-sibling");

        Assert.Equal(root, same);
        Assert.Equal(root.GetHashCode(), same.GetHashCode());
        Assert.True(root.IsSameOrAncestorOf(child));
        Assert.True(root.IsAncestorOf(child));
        Assert.False(root.IsAncestorOf(same));
        Assert.False(root.IsSameOrAncestorOf(sibling));
    }

    [Fact]
    [Trait("Size", "Small")]
    public void EqualityOperators_HandleNullReferences ()
    {
        var path = AbsolutePath.Parse(
            Path.Combine(Path.GetTempPath(), "guarded-null-equality"));
        AbsolutePath? missing = null;

        Assert.True(path != missing);
        Assert.False(path == missing);
        Assert.True(missing == null);
        Assert.False(missing != null);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void GetRoot_ReturnsGuardedCurrentPlatformRoot ()
    {
        var path = AbsolutePath.Parse(
            Path.Combine(Path.GetTempPath(), "guarded-root", "child"));

        var root = path.GetRoot();

        Assert.Equal(Path.GetPathRoot(path.Value), root.Value);
        Assert.False(root.TryGetParent(out var parent));
        Assert.Null(parent);
        Assert.True(root.IsSameOrAncestorOf(path));
    }

    [Fact]
    [Trait("Size", "Small")]
    public void GetRoot_OnWindows_PreservesUncShareRootIdentity ()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var path = AbsolutePath.Parse(@"\\Server\Share\Guarded\Child");

        var root = path.GetRoot();

        Assert.Equal(@"\\Server\Share", root.Value);
        Assert.Equal(AbsolutePath.Parse(@"\\server\share"), root);
        Assert.Equal(root, AbsolutePath.Parse(root.Value));
        Assert.True(root.IsAncestorOf(path));
    }

    [Fact]
    [Trait("Size", "Small")]
    public void TryGetParent_ForNonRootPath_ReturnsNormalizedAbsoluteParent ()
    {
        var parentPath = AbsolutePath.Parse(
            Path.Combine(Path.GetTempPath(), "guarded-parent"));
        var childPath = AbsolutePath.Parse(
            Path.Combine(parentPath.Value, "child"));

        var success = childPath.TryGetParent(out var actualParent);

        Assert.True(success);
        Assert.NotNull(actualParent);
        Assert.Equal(parentPath, actualParent);
        Assert.Equal(actualParent, AbsolutePath.Parse(actualParent.Value));
        Assert.True(actualParent.IsAncestorOf(childPath));
    }

    [Fact]
    [Trait("Size", "Small")]
    public void TryGetParent_ForCurrentPlatformRoot_ReturnsFalseAndNull ()
    {
        var rootPath = AbsolutePath.Parse(
            Path.GetPathRoot(Path.GetFullPath("."))!);

        var success = rootPath.TryGetParent(out var parent);

        Assert.False(success);
        Assert.Null(parent);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void TryGetParent_ForWindowsUncShareRoot_ReturnsFalseAndNull ()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var rootPath = AbsolutePath.Parse(@"\\server\share\");

        var success = rootPath.TryGetParent(out var parent);

        Assert.False(success);
        Assert.Null(parent);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void TryGetParent_OnWindows_ReturnsRepresentableUncParentWithStableIdentity ()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var child = AbsolutePath.Parse(
            @"\\Server\Share\Guarded\Child");

        var success = child.TryGetParent(out var parent);

        Assert.True(success);
        Assert.NotNull(parent);
        Assert.Equal(@"\\Server\Share\Guarded", parent.Value);
        Assert.Equal(parent, AbsolutePath.Parse(parent.Value));
        Assert.True(parent.IsAncestorOf(child));
    }

    [Theory]
    [Trait("Size", "Small")]
    [InlineData(@"C:\guarded\name \")]
    [InlineData(@"C:\guarded\directory \child")]
    [InlineData(@"\\server\share\name...\")]
    [InlineData(@"\\server\share\directory...\child")]
    public void TryParse_OnWindows_RejectsComponentRetainedBeforeSeparator (
        string input)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        Assert.False(AbsolutePath.TryParse(
            input,
            out var path,
            out var failure));
        Assert.Null(path);
        Assert.Equal(PathValidationFailureKind.InvalidPathFormat, failure.Kind);
    }

    [Theory]
    [Trait("Size", "Small")]
    [InlineData(@"C:\Guarded\Nested\File.txt")]
    [InlineData(@"C:\Guarded\name \child\..")]
    [InlineData(@"\\Server\Share\Guarded\Nested\File.txt")]
    public void TryGetParent_OnWindows_PreservesIdentityUntilRoot (
        string input)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var current = AbsolutePath.Parse(input);
        var expectedRoot = current.GetRoot();

        while (current.TryGetParent(out var parent))
        {
            Assert.Equal(parent, AbsolutePath.Parse(parent.Value));
            Assert.True(parent.IsAncestorOf(current));
            current = parent;
        }

        Assert.Equal(expectedRoot, current);
        Assert.False(current.TryGetParent(out var rootParent));
        Assert.Null(rootParent);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void PublicSurface_DoesNotAllowUncheckedOrImplicitConstruction ()
    {
        var publicConstructors = typeof(AbsolutePath).GetConstructors(
            BindingFlags.Public | BindingFlags.Instance);
        var conversionOperators = typeof(AbsolutePath)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(static method => method.Name is "op_Implicit" or "op_Explicit");

        Assert.Empty(publicConstructors);
        Assert.Empty(conversionOperators);
        Assert.True(typeof(AbsolutePath).IsSealed);
        Assert.False(typeof(AbsolutePath).IsValueType);

        AbsolutePath? absentPath = default;
        Assert.Null(absentPath);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Value_DoesNotClaimPhysicalFileSystemState ()
    {
        var nonexistentPath = Path.Combine(
            Path.GetTempPath(),
            "guarded-path-does-not-exist",
            Guid.NewGuid().ToString("N"));

        var path = AbsolutePath.Parse(nonexistentPath);

        Assert.False(File.Exists(path.Value));
        Assert.False(Directory.Exists(path.Value));
    }

}
