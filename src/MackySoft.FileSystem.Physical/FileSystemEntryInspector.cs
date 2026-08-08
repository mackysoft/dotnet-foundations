using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace MackySoft.FileSystem;

/// <summary> Observes filesystem entry state without following a link at the inspected path. </summary>
public static class FileSystemEntryInspector
{
    private const ushort FileTypeMask = 0xF000;
    private const ushort DirectoryFileType = 0x4000;
    private const ushort RegularFileType = 0x8000;
    private const ushort SymbolicLinkFileType = 0xA000;

    private const int AtCurrentWorkingDirectory = -100;
    private const int AtSymlinkNoFollow = 0x100;
    private const uint StatxType = 0x0001;
    private const int StatBufferSize = 512;
    private const int LinuxStatxModeOffset = 28;
    private const int DarwinStatModeOffset = 4;

    private const int NoSuchEntryError = 2;
    private const int OperationNotPermittedError = 1;
    private const int AccessDeniedError = 13;
    private const int NotDirectoryError = 20;
    private const int FunctionNotImplementedError = 38;
    private const int LinuxLinkCycleError = 40;
    private const int DarwinLinkCycleError = 62;

    /// <summary> Attempts to observe the entry at an absolute path without following the final path segment. </summary>
    /// <param name="path"> The guarded absolute path to inspect. </param>
    /// <param name="observation"> The current entry state when this method returns <see langword="true" />. </param>
    /// <param name="failure">
    /// <see cref="FileSystemOperationFailureKind.None" /> on success; otherwise the failed operating-system observation.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when the path was observed, including when it is missing; otherwise <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="path" /> is <see langword="null" />. </exception>
    public static bool TryInspect (
        AbsolutePath path,
        [NotNullWhen(true)] out FileSystemEntryObservation? observation,
        out FileSystemOperationFailure failure)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (OperatingSystem.IsWindows())
        {
            return TryInspectWindows(path, out observation, out failure);
        }

        if (OperatingSystem.IsLinux())
        {
            return TryInspectUnix(path, useStatx: true, out observation, out failure);
        }

        if (OperatingSystem.IsMacOS())
        {
            return TryInspectUnix(path, useStatx: false, out observation, out failure);
        }

        observation = null;
        failure = CreateFailure(
            FileSystemOperationFailureKind.PlatformNotSupported,
            path,
            "The running platform does not provide a supported no-follow entry inspection operation.");
        return false;
    }

    private static bool TryInspectWindows (
        AbsolutePath path,
        [NotNullWhen(true)] out FileSystemEntryObservation? observation,
        out FileSystemOperationFailure failure)
    {
        try
        {
            var attributes = File.GetAttributes(path.Value);
            var state = ClassifyWindowsEntry(path, attributes);
            observation = new FileSystemEntryObservation(path, state);
            failure = default;
            return true;
        }
        catch (FileNotFoundException)
        {
            return Missing(path, out observation, out failure);
        }
        catch (DirectoryNotFoundException)
        {
            return Missing(path, out observation, out failure);
        }
        catch (UnauthorizedAccessException exception)
        {
            observation = null;
            failure = CreateFailure(FileSystemOperationFailureKind.AccessDenied, path, exception.Message);
            return false;
        }
        catch (PlatformNotSupportedException exception)
        {
            observation = null;
            failure = CreateFailure(FileSystemOperationFailureKind.PlatformNotSupported, path, exception.Message);
            return false;
        }
        catch (IOException exception)
        {
            observation = null;
            failure = CreateFailure(FileSystemOperationFailureKind.IoFailure, path, exception.Message);
            return false;
        }
    }

    private static FileSystemEntryState ClassifyWindowsEntry (
        AbsolutePath path,
        FileAttributes attributes)
    {
        if ((attributes & FileAttributes.ReparsePoint) != 0)
        {
            FileSystemInfo entry = (attributes & FileAttributes.Directory) != 0
                ? new DirectoryInfo(path.Value)
                : new FileInfo(path.Value);
            return entry.LinkTarget is null
                ? FileSystemEntryState.ReparsePoint
                : FileSystemEntryState.SymbolicLink;
        }

        if ((attributes & FileAttributes.Directory) != 0)
        {
            return FileSystemEntryState.Directory;
        }

        return (attributes & FileAttributes.Device) != 0
            ? FileSystemEntryState.Other
            : FileSystemEntryState.RegularFile;
    }

    private static bool TryInspectUnix (
        AbsolutePath path,
        bool useStatx,
        [NotNullWhen(true)] out FileSystemEntryObservation? observation,
        out FileSystemOperationFailure failure)
    {
        try
        {
            var buffer = new byte[StatBufferSize];
            var result = useStatx
                ? Statx(AtCurrentWorkingDirectory, path.Value, AtSymlinkNoFollow, StatxType, buffer)
                : LStat(path.Value, buffer);
            if (result != 0)
            {
                return FromNativeError(path, Marshal.GetLastPInvokeError(), useStatx, out observation, out failure);
            }

            var mode = BitConverter.ToUInt16(
                buffer,
                useStatx ? LinuxStatxModeOffset : DarwinStatModeOffset);
            var state = (mode & FileTypeMask) switch
            {
                RegularFileType => FileSystemEntryState.RegularFile,
                DirectoryFileType => FileSystemEntryState.Directory,
                SymbolicLinkFileType => FileSystemEntryState.SymbolicLink,
                _ => FileSystemEntryState.Other,
            };
            observation = new FileSystemEntryObservation(path, state);
            failure = default;
            return true;
        }
        catch (Exception exception) when (exception is DllNotFoundException or EntryPointNotFoundException or MarshalDirectiveException)
        {
            observation = null;
            failure = CreateFailure(FileSystemOperationFailureKind.PlatformNotSupported, path, exception.Message);
            return false;
        }
    }

    private static bool FromNativeError (
        AbsolutePath path,
        int error,
        bool useStatx,
        [NotNullWhen(true)] out FileSystemEntryObservation? observation,
        out FileSystemOperationFailure failure)
    {
        if (error is NoSuchEntryError or NotDirectoryError)
        {
            return Missing(path, out observation, out failure);
        }

        var kind = error switch
        {
            OperationNotPermittedError or AccessDeniedError => FileSystemOperationFailureKind.AccessDenied,
            LinuxLinkCycleError or DarwinLinkCycleError => FileSystemOperationFailureKind.LinkCycle,
            _ when useStatx && error == FunctionNotImplementedError => FileSystemOperationFailureKind.PlatformNotSupported,
            _ => FileSystemOperationFailureKind.IoFailure,
        };
        observation = null;
        failure = CreateFailure(kind, path, $"No-follow entry inspection failed with native error {error}.");
        return false;
    }

    private static bool Missing (
        AbsolutePath path,
        [NotNullWhen(true)] out FileSystemEntryObservation? observation,
        out FileSystemOperationFailure failure)
    {
        observation = new FileSystemEntryObservation(path, FileSystemEntryState.Missing);
        failure = default;
        return true;
    }

    private static FileSystemOperationFailure CreateFailure (
        FileSystemOperationFailureKind kind,
        AbsolutePath path,
        string message)
    {
        return FileSystemOperationFailure.Create(kind, path, message);
    }

    [DllImport("libc", SetLastError = true, EntryPoint = "statx")]
    private static extern int Statx (
        int directoryFileDescriptor,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string path,
        int flags,
        uint mask,
        byte[] buffer);

    [DllImport("libc", SetLastError = true, EntryPoint = "lstat")]
    private static extern int LStat (
        [MarshalAs(UnmanagedType.LPUTF8Str)] string path,
        byte[] buffer);
}
