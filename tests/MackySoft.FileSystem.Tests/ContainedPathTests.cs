using System.Reflection;

namespace MackySoft.FileSystem.Tests;

public sealed class ContainedPathTests
{
    [Fact]
    [Trait("Size", "Small")]
    public void Create_WithRootAndTarget_DerivesMatchingRelativePath ()
    {
        var root = CreateAbsolutePath("contained-root");
        var target = AbsolutePath.Parse(Path.Combine(root.Value, "directory", "file.txt"));

        var path = ContainedPath.Create(root, target);

        Assert.Equal(root, path.BoundaryRoot);
        Assert.Equal(target, path.Target);
        Assert.Equal("directory/file.txt", path.RelativePath.Value);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Create_WithRootRelativePath_DerivesMatchingTarget ()
    {
        var root = CreateAbsolutePath("contained-relative-root");
        var relative = RootRelativePath.Parse("directory/file.txt");

        var path = ContainedPath.Create(root, relative);

        Assert.Equal(
            AbsolutePath.Parse(Path.Combine(root.Value, "directory", "file.txt")),
            path.Target);
        Assert.Equal(relative, path.RelativePath);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Create_WithBoundaryRoot_DerivesDotRelativePath ()
    {
        var root = CreateAbsolutePath("contained-self-root");

        var path = ContainedPath.Create(root, root);

        Assert.Equal(".", path.RelativePath.Value);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void TryCreate_RejectsSiblingPrefixAndOutsideTarget ()
    {
        var root = CreateAbsolutePath("contained-boundary");
        var sibling = AbsolutePath.Parse(root.Value + "-sibling");
        var outside = CreateAbsolutePath("contained-outside");

        Assert.False(ContainedPath.TryCreate(root, sibling, out var siblingPath, out var siblingFailure));
        Assert.Null(siblingPath);
        Assert.Equal(PathValidationFailureKind.OutsideBoundary, siblingFailure.Kind);

        Assert.False(ContainedPath.TryCreate(root, outside, out var outsidePath, out var outsideFailure));
        Assert.Null(outsidePath);
        Assert.Equal(PathValidationFailureKind.OutsideBoundary, outsideFailure.Kind);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void TryResolve_AcceptsRelativeOrAbsoluteInputAtFactoryBoundary ()
    {
        var root = CreateAbsolutePath("contained-resolve-root");
        var absoluteText = Path.Combine(root.Value, "absolute.txt");

        Assert.True(ContainedPath.TryResolve(
            root,
            "relative.txt",
            out var relativePath,
            out var relativeFailure));
        Assert.Equal(PathValidationFailureKind.None, relativeFailure.Kind);
        Assert.Equal("relative.txt", relativePath.RelativePath.Value);

        Assert.True(ContainedPath.TryResolve(
            root,
            absoluteText,
            out var absolutePath,
            out var absoluteFailure));
        Assert.Equal(PathValidationFailureKind.None, absoluteFailure.Kind);
        Assert.Equal(AbsolutePath.Parse(absoluteText), absolutePath.Target);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void TryResolve_RejectsTraversalOutsideBoundary ()
    {
        var root = CreateAbsolutePath("contained-traversal-root");

        Assert.False(ContainedPath.TryResolve(
            root,
            "../outside.txt",
            out var path,
            out var failure));
        Assert.Null(path);
        Assert.Equal(PathValidationFailureKind.OutsideBoundary, failure.Kind);
    }

    [Theory]
    [Trait("Size", "Small")]
    [InlineData("../contained-reentry-root/child.txt")]
    [InlineData("../__lexical_path_boundary__/child.txt")]
    public void TryResolve_RejectsRelativeInputThatLeavesAndReentersBoundary (
        string input)
    {
        var root = CreateAbsolutePath("contained-reentry-root");

        Assert.False(ContainedPath.TryResolve(
            root,
            input,
            out var path,
            out var failure));
        Assert.Null(path);
        Assert.Equal(PathValidationFailureKind.OutsideBoundary, failure.Kind);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void TryResolve_OnWindows_RejectsTraversalIntroducedByEndpointNormalization ()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var root = AbsolutePath.Parse(@"C:\guarded\boundary");

        Assert.False(ContainedPath.TryResolve(
            root,
            ".. ",
            out var path,
            out var failure));
        Assert.Null(path);
        Assert.Equal(PathValidationFailureKind.InvalidPathFormat, failure.Kind);
    }

    [Theory]
    [Trait("Size", "Small")]
    [InlineData("invalid*name")]
    [InlineData("file:stream")]
    [InlineData(@"\\server")]
    public void TryResolve_OnWindows_RejectsInvalidOrdinaryPathAndReturnsNull (
        string input)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var root = AbsolutePath.Parse(@"C:\guarded\boundary");

        Assert.False(ContainedPath.TryResolve(
            root,
            input,
            out var path,
            out var failure));
        Assert.Null(path);
        Assert.Equal(PathValidationFailureKind.InvalidPathFormat, failure.Kind);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void TryResolve_OnUnix_PreservesWhitespaceOnlyRelativePath ()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        var root = AbsolutePath.Parse("/guarded/boundary");

        Assert.True(ContainedPath.TryResolve(
            root,
            " ",
            out var path,
            out var failure));
        Assert.Equal(PathValidationFailureKind.None, failure.Kind);
        Assert.Equal(" ", path.RelativePath.Value);
        Assert.Equal("/guarded/boundary/ ", path.Target.Value);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void TryCreate_OnWindows_UsesCaseInsensitiveContainment ()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var root = AbsolutePath.Parse(@"C:\Guarded\Boundary");
        var differentlyCasedDescendant = AbsolutePath.Parse(
            @"c:\guarded\boundary\Child\File.txt");

        Assert.True(ContainedPath.TryCreate(
            root,
            differentlyCasedDescendant,
            out var path,
            out var failure));
        Assert.Equal(PathValidationFailureKind.None, failure.Kind);
        Assert.Equal("Child/File.txt", path.RelativePath.Value);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void TryCreate_OnUnix_UsesCaseSensitiveContainment ()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        var root = AbsolutePath.Parse("/guarded/Boundary");
        var differentlyCasedPrefix = AbsolutePath.Parse(
            "/guarded/boundary/child/file.txt");

        Assert.False(ContainedPath.TryCreate(
            root,
            differentlyCasedPrefix,
            out var path,
            out var failure));
        Assert.Null(path);
        Assert.Equal(PathValidationFailureKind.OutsideBoundary, failure.Kind);
    }

    [Theory]
    [Trait("Size", "Small")]
    [InlineData("name ")]
    [InlineData("name...")]
    public void CreateAndResolve_OnWindows_KeepEndpointCanonicalizationAligned (
        string input)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var root = AbsolutePath.Parse(@"C:\guarded\boundary");
        var relativePath = RootRelativePath.Parse(input);

        var path = ContainedPath.Create(root, relativePath);
        var derivedFromTarget = ContainedPath.Create(root, path.Target);
        var resolved = ContainedPath.Resolve(root, input);

        Assert.Equal("name", relativePath.Value);
        Assert.Equal(AbsolutePath.Parse(@"C:\guarded\boundary\name"), path.Target);
        Assert.Equal(relativePath, derivedFromTarget.RelativePath);
        Assert.Equal(path, resolved);
    }

    [Theory]
    [Trait("Size", "Small")]
    [InlineData(
        @"C:\guarded\boundary",
        @"C:\guarded\boundary\Directory\Child",
        "Directory/Child")]
    [InlineData(
        @"\\server\share",
        @"\\server\share\Directory\Child",
        "Directory/Child")]
    public void Create_OnWindows_PreservesParentClosureAcrossTypedDerivation (
        string boundaryValue,
        string targetValue,
        string expectedRelativeValue)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var boundary = AbsolutePath.Parse(boundaryValue);
        var target = AbsolutePath.Parse(targetValue);

        var path = ContainedPath.Create(
            boundary,
            target);
        var recreated = ContainedPath.Create(
            boundary,
            path.RelativePath);

        Assert.Equal(expectedRelativeValue, path.RelativePath.Value);
        Assert.Equal(
            path.RelativePath,
            RootRelativePath.Parse(path.RelativePath.Value));
        Assert.Equal(target, recreated.Target);
        Assert.Equal(target, AbsolutePath.Parse(target.Value));
        Assert.True(boundary.IsAncestorOf(target));
        Assert.True(target.TryGetParent(out var parent));
        Assert.NotNull(parent);
        Assert.True(parent.IsAncestorOf(target));
    }

    [Theory]
    [Trait("Size", "Small")]
    [InlineData("directory /child")]
    [InlineData("directory.../child")]
    public void TryResolve_OnWindows_RejectsRelativeComponentWithoutStableParentIdentity (
        string input)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var boundary = AbsolutePath.Parse(@"C:\guarded\boundary");

        Assert.False(ContainedPath.TryResolve(
            boundary,
            input,
            out var path,
            out var failure));
        Assert.Null(path);
        Assert.Equal(PathValidationFailureKind.InvalidPathFormat, failure.Kind);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void TryResolve_OnWindows_RejectsComponentRetainedBeforeTrailingSeparator ()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var root = AbsolutePath.Parse(@"C:\guarded\boundary");

        Assert.False(ContainedPath.TryResolve(
            root,
            "name /",
            out var path,
            out var failure));
        Assert.Null(path);
        Assert.Equal(PathValidationFailureKind.InvalidPathFormat, failure.Kind);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void ResolveAndCreate_OnWindows_KeepNavigationPromotedEndpointAligned ()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var root = AbsolutePath.Parse(@"C:\guarded\boundary");
        var relativePath = RootRelativePath.Parse("name /child/..");

        var resolved = ContainedPath.Resolve(
            root,
            "name /child/..");
        var combined = ContainedPath.Create(
            root,
            relativePath);

        Assert.Equal("name", relativePath.Value);
        Assert.Equal(relativePath, RootRelativePath.Parse(relativePath.Value));
        Assert.Equal(
            AbsolutePath.Parse(@"C:\guarded\boundary\name"),
            resolved.Target);
        Assert.Equal(resolved, combined);
        Assert.Equal(combined.Target, AbsolutePath.Parse(combined.Target.Value));
        Assert.True(root.IsSameOrAncestorOf(combined.Target));
    }

    [Fact]
    [Trait("Size", "Small")]
    public void TryResolve_OnWindows_NormalizesRemovedNavigationEndpointToBoundaryRoot ()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var root = AbsolutePath.Parse(@"C:\guarded\boundary");
        var relativePath = RootRelativePath.Parse(".. /child/..");
        var created = ContainedPath.Create(root, relativePath);

        Assert.True(ContainedPath.TryResolve(
            root,
            ".. /child/..",
            out var path,
            out var failure));
        Assert.Equal(PathValidationFailureKind.None, failure.Kind);
        Assert.NotNull(path);
        Assert.True(path.RelativePath.IsRoot);
        Assert.Equal(root, path.Target);
        Assert.Equal(created, path);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Equality_IncludesBoundaryRelationship ()
    {
        var outerRoot = CreateAbsolutePath("contained-equality");
        var innerRoot = AbsolutePath.Parse(Path.Combine(outerRoot.Value, "inner"));
        var target = AbsolutePath.Parse(Path.Combine(innerRoot.Value, "file.txt"));

        var fromOuter = ContainedPath.Create(outerRoot, target);
        var fromInner = ContainedPath.Create(innerRoot, target);

        Assert.NotEqual(fromOuter, fromInner);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void EqualityOperators_HandleNullReferences ()
    {
        var root = CreateAbsolutePath("contained-null-equality");
        var path = ContainedPath.Create(root, root);
        ContainedPath? missing = null;

        Assert.True(path != missing);
        Assert.False(path == missing);
        Assert.True(missing == null);
        Assert.False(missing != null);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void PublicSurface_DoesNotAllowUncheckedOrImplicitConstruction ()
    {
        var publicConstructors = typeof(ContainedPath).GetConstructors(
            BindingFlags.Public | BindingFlags.Instance);
        var conversionOperators = typeof(ContainedPath)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(static method => method.Name is "op_Implicit" or "op_Explicit");

        Assert.Empty(publicConstructors);
        Assert.Empty(conversionOperators);
        Assert.True(typeof(ContainedPath).IsSealed);
        Assert.False(typeof(ContainedPath).IsValueType);

        ContainedPath? absentPath = default;
        Assert.Null(absentPath);
    }

    private static AbsolutePath CreateAbsolutePath (string name)
    {
        return AbsolutePath.Parse(Path.Combine(Path.GetTempPath(), name));
    }

}
