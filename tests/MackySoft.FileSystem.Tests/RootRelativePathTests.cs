using System.Reflection;

namespace MackySoft.FileSystem.Tests;

public sealed class RootRelativePathTests
{
    [Fact]
    [Trait("Size", "Small")]
    public void Parse_NormalizesSeparatorsAndSegmentsToCanonicalText ()
    {
        var path = RootRelativePath.Parse("directory/nested/../file.txt/");

        Assert.Equal("directory/file.txt", path.Value);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Parse_NormalizesRootSelfToDot ()
    {
        Assert.Equal(".", RootRelativePath.Parse(".").Value);
        Assert.Equal(".", RootRelativePath.Parse("directory/..").Value);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void TryParse_ClassifiesEmptyAbsoluteAndEscapingInputs ()
    {
        Assert.False(RootRelativePath.TryParse(string.Empty, out var emptyPath, out var emptyFailure));
        Assert.Null(emptyPath);
        Assert.Equal(PathValidationFailureKind.EmptyPath, emptyFailure.Kind);

        var absoluteInput = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "absolute"));
        Assert.False(RootRelativePath.TryParse(absoluteInput, out var absolutePath, out var absoluteFailure));
        Assert.Null(absolutePath);
        Assert.Equal(PathValidationFailureKind.ExpectedRootRelativePath, absoluteFailure.Kind);

        Assert.False(RootRelativePath.TryParse("../outside", out var outsidePath, out var outsideFailure));
        Assert.Null(outsidePath);
        Assert.Equal(PathValidationFailureKind.OutsideBoundary, outsideFailure.Kind);

        Assert.False(RootRelativePath.TryParse("invalid\0path", out var invalidPath, out var invalidFailure));
        Assert.Null(invalidPath);
        Assert.Equal(PathValidationFailureKind.InvalidPathFormat, invalidFailure.Kind);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void TryParse_RejectsTraversalThatReentersNormalizationBase ()
    {
        Assert.False(RootRelativePath.TryParse(
            "../__lexical_path_boundary__/child.txt",
            out var path,
            out var failure));
        Assert.Null(path);
        Assert.Equal(PathValidationFailureKind.OutsideBoundary, failure.Kind);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void IsSameAs_UsesCanonicalPlatformIdentity ()
    {
        var left = RootRelativePath.Parse("directory/file.txt");
        var right = RootRelativePath.Parse("directory/./file.txt/");
        var different = RootRelativePath.Parse("directory/other.txt");

        Assert.True(left.IsSameAs(right));
        Assert.False(left.IsSameAs(different));
        Assert.Throws<ArgumentNullException>(() => left.IsSameAs(null!));
        Assert.Equal(left, right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Fact]
    [Trait("Size", "Small")]
    public void EqualityOperators_HandleNullReferences ()
    {
        var path = RootRelativePath.Parse("directory/file.txt");
        RootRelativePath? missing = null;

        Assert.True(path != missing);
        Assert.False(path == missing);
        Assert.True(missing == null);
        Assert.False(missing != null);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Parse_PreservesBackslashFilenameCharacter_OnUnix ()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        var filename = RootRelativePath.Parse(@"directory\file.txt");
        var descendant = RootRelativePath.Parse("directory/file.txt");

        Assert.Equal(@"directory\file.txt", filename.Value);
        Assert.False(filename.IsSameAs(descendant));
        Assert.NotEqual(filename, descendant);
    }

    [Theory]
    [Trait("Size", "Small")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Parse_OnUnix_PreservesWhitespaceOnlyFilename (string input)
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        var path = RootRelativePath.Parse(input);

        Assert.Equal(input, path.Value);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void TryParse_OnWindows_RejectsDriveRelativeInputAsRooted ()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        Assert.False(RootRelativePath.TryParse(
            @"C:relative\file.txt",
            out var path,
            out var failure));
        Assert.Null(path);
        Assert.Equal(PathValidationFailureKind.ExpectedRootRelativePath, failure.Kind);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void TryParse_OnWindows_RejectsTraversalIntroducedByEndpointNormalization ()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        Assert.False(RootRelativePath.TryParse(
            ".. ",
            out var path,
            out var failure));
        Assert.Null(path);
        Assert.Equal(PathValidationFailureKind.InvalidPathFormat, failure.Kind);
    }

    [Theory]
    [Trait("Size", "Small")]
    [InlineData(" ")]
    [InlineData(". ")]
    [InlineData(".. ")]
    [InlineData("...")]
    [InlineData("invalid*name")]
    [InlineData("file:stream")]
    [InlineData("CON.txt")]
    public void TryParse_OnWindows_RejectsInvalidOrdinarySegments (string input)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        Assert.False(RootRelativePath.TryParse(
            input,
            out var path,
            out var failure));
        Assert.Null(path);
        Assert.Equal(PathValidationFailureKind.InvalidPathFormat, failure.Kind);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Parse_OnWindows_NormalizesTrailingSpacesAndPeriodsForIdentity ()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var canonical = RootRelativePath.Parse("name");
        var trailingSpace = RootRelativePath.Parse("name ");
        var trailingPeriods = RootRelativePath.Parse("name...");

        Assert.Equal("name", canonical.Value);
        Assert.Equal(canonical, trailingSpace);
        Assert.Equal(canonical, trailingPeriods);
        Assert.Equal(canonical.GetHashCode(), trailingSpace.GetHashCode());
        Assert.Equal(canonical.GetHashCode(), trailingPeriods.GetHashCode());
    }

    [Theory]
    [Trait("Size", "Small")]
    [InlineData("name /")]
    [InlineData("name.../")]
    public void TryParse_OnWindows_RejectsComponentRetainedBeforeTrailingSeparator (
        string input)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        Assert.False(RootRelativePath.TryParse(
            input,
            out var path,
            out var failure));
        Assert.Null(path);
        Assert.Equal(PathValidationFailureKind.InvalidPathFormat, failure.Kind);
    }

    [Theory]
    [Trait("Size", "Small")]
    [InlineData("name /.", "name")]
    [InlineData("name /child/..", "name")]
    public void Parse_OnWindows_NormalizesComponentPromotedToEndpointByNavigation (
        string input,
        string expected)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var path = RootRelativePath.Parse(input);

        Assert.Equal(expected, path.Value);
        Assert.Equal(path, RootRelativePath.Parse(path.Value));
    }

    [Fact]
    [Trait("Size", "Small")]
    public void TryParse_OnWindows_NormalizesRemovedNavigationEndpointToRoot ()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        Assert.True(RootRelativePath.TryParse(
            ".. /child/..",
            out var path,
            out var failure));
        Assert.Equal(PathValidationFailureKind.None, failure.Kind);
        Assert.NotNull(path);
        Assert.Equal(".", path.Value);
        Assert.Equal(path, RootRelativePath.Parse(path.Value));
    }

    [Theory]
    [Trait("Size", "Small")]
    [InlineData("directory /child.txt")]
    [InlineData("directory.../child.txt")]
    public void TryParse_OnWindows_RejectsIntermediateComponentWithoutStableParentIdentity (
        string input)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        Assert.False(RootRelativePath.TryParse(
            input,
            out var path,
            out var failure));
        Assert.Null(path);
        Assert.Equal(PathValidationFailureKind.InvalidPathFormat, failure.Kind);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Parse_OnWindows_TreatsSpacedDotDotAsIntermediateDirectoryName ()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        Assert.Equal(
            "segment",
            RootRelativePath.Parse("segment/.. /..").Value);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Identity_OnWindows_PreservesCasingAndComparesCaseInsensitively ()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var mixedCase = RootRelativePath.Parse("Directory/File.txt");
        var lowerCase = RootRelativePath.Parse("directory/file.txt");

        Assert.Equal("Directory/File.txt", mixedCase.Value);
        Assert.Equal("directory/file.txt", lowerCase.Value);
        Assert.True(mixedCase.IsSameAs(lowerCase));
        Assert.Equal(mixedCase, lowerCase);
        Assert.Equal(mixedCase.GetHashCode(), lowerCase.GetHashCode());
    }

    [Fact]
    [Trait("Size", "Small")]
    public void PublicSurface_DoesNotAllowUncheckedOrImplicitConstruction ()
    {
        var publicConstructors = typeof(RootRelativePath).GetConstructors(
            BindingFlags.Public | BindingFlags.Instance);
        var conversionOperators = typeof(RootRelativePath)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(static method => method.Name is "op_Implicit" or "op_Explicit");

        Assert.Empty(publicConstructors);
        Assert.Empty(conversionOperators);
        Assert.True(typeof(RootRelativePath).IsSealed);
        Assert.False(typeof(RootRelativePath).IsValueType);

        RootRelativePath? absentPath = default;
        Assert.Null(absentPath);
    }

}
