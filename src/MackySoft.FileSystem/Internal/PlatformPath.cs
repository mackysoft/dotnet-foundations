using System.Runtime.InteropServices;
using System.Text;

namespace MackySoft.FileSystem.Internal;

internal static class PlatformPath
{
    private const int InitialWindowsPathBufferCapacity = 260;

    public const char CanonicalRelativeSeparator = '/';

    public static StringComparison IdentityComparison =>
        IsWindows ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    public static StringComparer IdentityComparer =>
        IsWindows ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    private static bool IsWindows =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    public static bool TryValidateInputPath (
        string value,
        out string failureMessage)
    {
        if (!IsWindows)
        {
            if (value.IndexOf('\0') >= 0)
            {
                failureMessage = "Path contains a null character, which is invalid on Unix.";
                return false;
            }

            failureMessage = string.Empty;
            return true;
        }

        return TryValidateWindowsOrdinaryPath(
            value,
            out failureMessage);
    }

    private static bool TryValidateCanonicalAbsolutePath (
        string value,
        out string failureMessage)
    {
        if (!Path.IsPathFullyQualified(value))
        {
            failureMessage = "Normalized path text must remain fully qualified on the current platform.";
            return false;
        }

        if (IsWindows
            && value.Length >= 2
            && IsDirectorySeparator(value[0])
            && IsDirectorySeparator(value[1]))
        {
            var serverEnd = value.IndexOf(
                Path.DirectorySeparatorChar,
                2);
            if (serverEnd < 0)
            {
                failureMessage = "A normalized Windows UNC path must retain both a server and a share name.";
                return false;
            }

            var server = value.Substring(
                2,
                serverEnd - 2);
            if (server.Length == 0
                || IsExactWindowsNavigationSegment(server))
            {
                failureMessage = "A normalized Windows UNC server name must remain non-empty and non-relative.";
                return false;
            }

            // The pre-trim parent-stability check has already handled separators. The canonical
            // postcondition now requires a share that remains stable when Value is parsed again.
            var shareStart = serverEnd + 1;
            var shareEnd = value.IndexOf(
                Path.DirectorySeparatorChar,
                shareStart);
            if (shareEnd < 0)
            {
                shareEnd = value.Length;
            }

            var share = value.Substring(
                shareStart,
                shareEnd - shareStart);
            if (share.Length == 0
                || IsExactWindowsNavigationSegment(share))
            {
                failureMessage = "A normalized Windows UNC share name must remain non-empty and non-relative.";
                return false;
            }

            if (share.Length != share.TrimEnd(' ', '.').Length)
            {
                failureMessage = "A normalized Windows UNC share root must remain stable after endpoint normalization.";
                return false;
            }
        }

        failureMessage = string.Empty;
        return true;
    }

    public static string ToPlatformSeparators (string value)
    {
        return value.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
    }

    public static string ToCanonicalRelativeSeparators (string value)
    {
        return value
            .Replace(Path.DirectorySeparatorChar, CanonicalRelativeSeparator)
            .Replace(Path.AltDirectorySeparatorChar, CanonicalRelativeSeparator);
    }

    public static bool TryNormalizeRootRelativePath (
        string value,
        out string normalizedPath)
    {
        if (!StaysWithinLexicalBoundary(value))
        {
            normalizedPath = string.Empty;
            return false;
        }

        // Segment walking has already proved that lexical normalization does not escape the boundary.
        // The fixed base now applies only the platform's remaining endpoint rules, such as trimming
        // trailing spaces and periods on Windows, without depending on the process working directory.
        var lexicalBoundary = IsWindows
            ? @"C:\__lexical_path_boundary__"
            : "/__lexical_path_boundary__";
        var absolutePath = NormalizeAbsolutePathLexically(
            value,
            lexicalBoundary);
        if (!IsSameOrDescendant(lexicalBoundary, absolutePath))
        {
            normalizedPath = string.Empty;
            return false;
        }

        normalizedPath = DeriveRootRelativePath(
                lexicalBoundary,
                absolutePath)
            .TrimEnd(CanonicalRelativeSeparator);
        if (normalizedPath.Length == 0)
        {
            normalizedPath = ".";
        }
        return true;
    }

    private static bool StaysWithinLexicalBoundary (string value)
    {
        var depth = 0;
        foreach (var segment in value.Split(Path.DirectorySeparatorChar))
        {
            if (segment.Length == 0
                || string.Equals(segment, ".", StringComparison.Ordinal))
            {
                continue;
            }

            if (!string.Equals(segment, "..", StringComparison.Ordinal))
            {
                depth++;
                continue;
            }

            if (depth == 0)
            {
                return false;
            }
            depth--;
        }

        return true;
    }

    private static bool HasWindowsDeviceNamespacePrefix (string value)
    {
        if (!IsWindows || value.Length < 4)
        {
            return false;
        }

        return value[0] == Path.DirectorySeparatorChar
            && (
                (
                    value[1] == Path.DirectorySeparatorChar
                    && (value[2] == '?' || value[2] == '.')
                    && value[3] == Path.DirectorySeparatorChar
                )
                || (
                    value[1] == '?'
                    && value[2] == '?'
                    && value[3] == Path.DirectorySeparatorChar
                )
            );
    }

    public static string TrimTrailingSeparatorsUnlessRoot (string value)
    {
        var root = Path.GetPathRoot(value);
        if (!string.IsNullOrEmpty(root)
            && string.Equals(value, root, IdentityComparison))
        {
            // A drive root requires its trailing separator to remain fully qualified, while a UNC
            // share is fully qualified without one. Canonicalize both accepted UNC spellings to
            // the separator-free share root so factory output has one stable lexical form.
            return IsWindows
                && value.Length > 2
                && IsDirectorySeparator(value[0])
                && IsDirectorySeparator(value[1])
                ? value.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                : value;
        }

        return value.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private static bool IsDirectorySeparator (char value)
    {
        return value == Path.DirectorySeparatorChar
            || value == Path.AltDirectorySeparatorChar;
    }

    public static string NormalizeAbsolutePathLexically (
        string value,
        string? basePath)
    {
        if (!IsWindows)
        {
            var normalizedPath = basePath is null
                ? Path.GetFullPath(value)
                : Path.GetFullPath(value, basePath);
            normalizedPath = TrimTrailingSeparatorsUnlessRoot(normalizedPath);
            if (!TryValidateCanonicalAbsolutePath(
                    normalizedPath,
                    out var failureMessage))
            {
                throw new ArgumentException(
                    failureMessage,
                    nameof(value));
            }
            return normalizedPath;
        }

        // A relative value is combined with an already guarded absolute base before the native
        // normalization call. GetFullPathNameW therefore never depends on the process current directory.
        var absoluteCandidate = basePath is null
            ? value
            : Path.Combine(basePath, value);
        return NormalizeWindowsAbsolutePathLexically(absoluteCandidate);
    }

    public static string DeriveRootRelativePath (
        string boundaryRoot,
        string target)
    {
        if (IdentityComparer.Equals(boundaryRoot, target))
        {
            return ".";
        }

        var relativeStart = boundaryRoot.EndsWith(
            Path.DirectorySeparatorChar.ToString(),
            StringComparison.Ordinal)
            ? boundaryRoot.Length
            : boundaryRoot.Length + 1;
        return ToCanonicalRelativeSeparators(
            target.Substring(relativeStart));
    }

    private static bool IsSameOrDescendant (
        string boundaryRoot,
        string candidate)
    {
        if (IdentityComparer.Equals(boundaryRoot, candidate))
        {
            return true;
        }

        var prefix = boundaryRoot.EndsWith(
            Path.DirectorySeparatorChar.ToString(),
            StringComparison.Ordinal)
            ? boundaryRoot
            : boundaryRoot + Path.DirectorySeparatorChar;
        return candidate.StartsWith(prefix, IdentityComparison);
    }

    private static string NormalizeWindowsAbsolutePathLexically (string value)
    {
        var normalizedPath = NormalizeWindowsPathWithNativeApi(value);
        if (!TryValidateWindowsParentStableNormalizedPath(
                normalizedPath,
                out var failureMessage))
        {
            throw new ArgumentException(
                failureMessage,
                nameof(value));
        }

        normalizedPath = TrimTrailingSeparatorsUnlessRoot(normalizedPath);
        if (!TryValidateCanonicalAbsolutePath(
                normalizedPath,
                out failureMessage))
        {
            throw new ArgumentException(
                failureMessage,
                nameof(value));
        }

        return normalizedPath;
    }

    private static string NormalizeWindowsPathWithNativeApi (string value)
    {
        var capacity = Math.Max(
            InitialWindowsPathBufferCapacity,
            checked(value.Length + 1));

        while (true)
        {
            var buffer = new StringBuilder(capacity);
            var resultLength = GetFullPathNameW(
                value,
                checked((uint)buffer.Capacity),
                buffer,
                IntPtr.Zero);
            if (resultLength == 0)
            {
                var errorCode = Marshal.GetLastWin32Error();
                throw new ArgumentException(
                    $"Windows could not lexically normalize path text. Win32 error: {errorCode}.",
                    nameof(value));
            }

            if (resultLength < buffer.Capacity)
            {
                return buffer.ToString();
            }

            if (resultLength > int.MaxValue)
            {
                throw new PathTooLongException(
                    "The normalized Windows path exceeds the supported managed string length.");
            }
            capacity = checked((int)resultLength);
        }
    }

    private static bool TryValidateWindowsOrdinaryPath (
        string value,
        out string failureMessage)
    {
        if (HasWindowsDeviceNamespacePrefix(value))
        {
            failureMessage = "Windows device-namespace path text is not supported.";
            return false;
        }

        var segmentStart = 0;
        if (value.Length >= 2
            && IsDirectorySeparator(value[0])
            && IsDirectorySeparator(value[1]))
        {
            if (!TryValidateWindowsUncRoot(
                    value,
                    out segmentStart,
                    out failureMessage))
            {
                return false;
            }
        }
        else if (value.Length >= 2 && value[1] == ':')
        {
            if (!IsAsciiDriveLetter(value[0]))
            {
                failureMessage = "A Windows drive designator must begin with an ASCII letter.";
                return false;
            }
            segmentStart = 2;
        }
        else if (value.Length > 0 && IsDirectorySeparator(value[0]))
        {
            segmentStart = 1;
        }

        if (!TryValidateWindowsSegments(
                value,
                segmentStart,
                out failureMessage))
        {
            return false;
        }

        failureMessage = string.Empty;
        return true;
    }

    private static bool TryValidateWindowsUncRoot (
        string value,
        out int segmentStart,
        out string failureMessage)
    {
        segmentStart = 0;
        var serverStart = 2;
        var serverEnd = value.IndexOf(
            Path.DirectorySeparatorChar,
            serverStart);
        if (serverEnd < 0)
        {
            failureMessage = "A Windows UNC path must include both a server and a share name.";
            return false;
        }

        var shareStart = serverEnd + 1;
        var shareEnd = value.IndexOf(
            Path.DirectorySeparatorChar,
            shareStart);
        if (shareEnd < 0)
        {
            shareEnd = value.Length;
        }

        var server = value.Substring(
            serverStart,
            serverEnd - serverStart);
        var share = value.Substring(
            shareStart,
            shareEnd - shareStart);
        if (!TryValidateWindowsUncRootComponent(
                server,
                "server",
                out failureMessage)
            || !TryValidateWindowsUncRootComponent(
                share,
                "share",
                out failureMessage))
        {
            return false;
        }

        segmentStart = shareEnd;
        failureMessage = string.Empty;
        return true;
    }

    private static bool TryValidateWindowsUncRootComponent (
        string value,
        string componentName,
        out string failureMessage)
    {
        if (value.Length == 0
            || string.Equals(value, ".", StringComparison.Ordinal)
            || string.Equals(value, "..", StringComparison.Ordinal))
        {
            failureMessage = $"A Windows UNC {componentName} name must be non-empty and non-relative.";
            return false;
        }

        if (!TryValidateWindowsSegmentCharacters(
                value,
                out failureMessage))
        {
            failureMessage = $"Windows UNC {componentName} name is invalid. {failureMessage}";
            return false;
        }

        failureMessage = string.Empty;
        return true;
    }

    private static bool TryValidateWindowsSegments (
        string value,
        int segmentStart,
        out string failureMessage)
    {
        string? lastNonEmptySegment = null;
        while (segmentStart < value.Length)
        {
            while (segmentStart < value.Length
                && IsDirectorySeparator(value[segmentStart]))
            {
                segmentStart++;
            }
            if (segmentStart >= value.Length)
            {
                break;
            }

            var segmentEnd = value.IndexOf(
                Path.DirectorySeparatorChar,
                segmentStart);
            if (segmentEnd < 0)
            {
                segmentEnd = value.Length;
            }

            var segment = value.Substring(
                segmentStart,
                segmentEnd - segmentStart);
            lastNonEmptySegment = segment;
            if (!TryValidateWindowsSegmentCharacters(
                    segment,
                    out failureMessage))
            {
                return false;
            }

            if (IsReservedWindowsDosDeviceName(segment))
            {
                failureMessage = $"Windows ordinary path segment '{segment}' is a reserved DOS device name.";
                return false;
            }

            segmentStart = segmentEnd + 1;
        }

        if (lastNonEmptySegment is not null
            && !IsExactWindowsNavigationSegment(lastNonEmptySegment)
            && lastNonEmptySegment.TrimEnd(' ', '.').Length == 0)
        {
            failureMessage = "Windows endpoint normalization must not remove the entire final path segment.";
            return false;
        }

        failureMessage = string.Empty;
        return true;
    }

    private static bool TryValidateWindowsParentStableSegments (
        string value,
        int segmentStart,
        out string failureMessage)
    {
        while (segmentStart < value.Length)
        {
            while (segmentStart < value.Length
                && IsDirectorySeparator(value[segmentStart]))
            {
                segmentStart++;
            }
            if (segmentStart >= value.Length)
            {
                break;
            }

            var segmentEnd = value.IndexOf(
                Path.DirectorySeparatorChar,
                segmentStart);
            if (segmentEnd < 0)
            {
                segmentEnd = value.Length;
            }

            var segment = value.Substring(
                segmentStart,
                segmentEnd - segmentStart);
            if (segmentEnd < value.Length
                && !IsExactWindowsNavigationSegment(segment)
                && segment.Length != segment.TrimEnd(' ', '.').Length)
            {
                failureMessage = "A normalized Windows path component followed by a directory separator must not end in a space or period because its endpoint identity would differ.";
                return false;
            }

            segmentStart = segmentEnd + 1;
        }

        failureMessage = string.Empty;
        return true;
    }

    private static bool TryValidateWindowsParentStableNormalizedPath (
        string value,
        out string failureMessage)
    {
        var segmentStart = Path.GetPathRoot(value)?.Length ?? 0;
        if (value.Length >= 2
            && IsDirectorySeparator(value[0])
            && IsDirectorySeparator(value[1]))
        {
            var serverEnd = value.IndexOf(
                Path.DirectorySeparatorChar,
                2);
            if (serverEnd >= 0)
            {
                // The server is a host identifier. The share and its descendants participate in
                // lexical parent derivation and therefore begin the parent-stability check.
                segmentStart = serverEnd + 1;
            }
        }

        return TryValidateWindowsParentStableSegments(
            value,
            segmentStart,
            out failureMessage);
    }

    private static bool TryValidateWindowsSegmentCharacters (
        string segment,
        out string failureMessage)
    {
        for (var index = 0; index < segment.Length; index++)
        {
            var character = segment[index];
            if (character < ' '
                || character is '"' or '*' or ':' or '<' or '>' or '?' or '|')
            {
                failureMessage = $"Path contains a character that is invalid in an ordinary Windows path segment: U+{(int)character:X4}.";
                return false;
            }
        }

        failureMessage = string.Empty;
        return true;
    }

    private static bool IsExactWindowsNavigationSegment (string segment)
    {
        return string.Equals(
                segment,
                ".",
                StringComparison.Ordinal)
            || string.Equals(
                segment,
                "..",
                StringComparison.Ordinal);
    }

    private static bool IsReservedWindowsDosDeviceName (string segment)
    {
        if (IsExactWindowsNavigationSegment(segment))
        {
            return false;
        }

        var normalizedSegment = segment.TrimEnd(' ', '.');
        var extensionSeparator = normalizedSegment.IndexOf('.');
        var baseName = (extensionSeparator < 0
            ? normalizedSegment
            : normalizedSegment.Substring(0, extensionSeparator))
            .TrimEnd(' ');
        if (baseName.Equals("CON", StringComparison.OrdinalIgnoreCase)
            || baseName.Equals("PRN", StringComparison.OrdinalIgnoreCase)
            || baseName.Equals("AUX", StringComparison.OrdinalIgnoreCase)
            || baseName.Equals("NUL", StringComparison.OrdinalIgnoreCase)
            || baseName.Equals("CONIN$", StringComparison.OrdinalIgnoreCase)
            || baseName.Equals("CONOUT$", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return baseName.Length == 4
            && (
                baseName.StartsWith("COM", StringComparison.OrdinalIgnoreCase)
                || baseName.StartsWith("LPT", StringComparison.OrdinalIgnoreCase)
            )
            && (
                baseName[3] is (>= '1' and <= '9')
                or '\u00B9'
                or '\u00B2'
                or '\u00B3'
            );
    }

    private static bool IsAsciiDriveLetter (char value)
    {
        return value is (>= 'A' and <= 'Z')
            or (>= 'a' and <= 'z');
    }

    public static bool IsPathFormatException (Exception exception)
    {
        return exception is ArgumentException
            or NotSupportedException
            or PathTooLongException;
    }

    [DllImport(
        "kernel32.dll",
        EntryPoint = "GetFullPathNameW",
        CharSet = CharSet.Unicode,
        ExactSpelling = true,
        SetLastError = true)]
    private static extern uint GetFullPathNameW (
        string fileName,
        uint bufferLength,
        [Out] StringBuilder buffer,
        IntPtr filePart);
}
