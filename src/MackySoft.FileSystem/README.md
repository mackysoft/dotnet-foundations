# MackySoft.FileSystem

`MackySoft.FileSystem` provides guarded values for physical filesystem path text. It validates raw text once at an input boundary, then carries structural path facts through typed contracts.

Install an exact package version:

```bash
dotnet add package MackySoft.FileSystem --version <version>
```

## Guarded values

| Type | Construction guarantees |
| --- | --- |
| `AbsolutePath` | Non-empty, accepted by the package's ordinary-path syntax for the current platform, fully qualified, and normalized for separators and trailing separators. Input casing is preserved; equality and hashing use current-platform case identity. |
| `RootRelativePath` | Non-empty, non-rooted, unable to traverse above an unspecified boundary, and stored with `/` separators. `.` identifies the boundary itself. |
| `ContainedPath` | A matching `AbsolutePath` boundary, absolute target, and `RootRelativePath` whose target is lexically equal to or below that boundary. |

Use `Parse` when invalid input is exceptional and `TryParse` or `TryResolve` when an adapter must map `PathValidationFailureKind` to its own diagnostics:

```csharp
var root = AbsolutePath.Parse(rootText);

if (!ContainedPath.TryResolve(root, inputText, out var path, out var failure))
{
    return ReportInvalidInput(failure.Kind, failure.Message);
}

OpenForRead(path.Target.Value);
```

There are no implicit conversions to or from `string`. Convert input text through a factory and access `Value` explicitly at a display, serialization, or `System.IO` boundary.

Raw text is classified and normalized once inside the selected factory. Operations that combine an
`AbsolutePath` with a `RootRelativePath`, or derive a relative value from an established containment
relationship, use those existing guarantees and do not route the result back through a raw-text parser.

`PathValidationFailureKind` is a CLR classification for branching in typed code. Its enum member names and
`PathValidationFailure.ToString()` output are diagnostics, not stable external text tokens. A transport adapter
that exposes text must define and own its vocabulary separately.

## Running-platform lexical identity

Absolute paths are not converted into a portable common-path string. One internal policy applies the lexical rules of the operating system running the process to separator normalization, root recognition, fully-qualified checks, equality, hashing, and containment:

- On Windows, `/` and `\` are directory separators, normalized absolute output uses `\`, drive roots and structurally complete UNC roots are preserved, drive-relative and current-drive-relative input is rejected where an absolute or ordinary relative path is required, and path identity is case-insensitive without rewriting the retained casing. A UNC root must contain non-empty, non-navigation server and share components that use the supported ordinary path characters. Host reachability, DNS or NetBIOS naming, and remote-provider share-creation policy are not validated. Ordinary path segments outside the UNC root reject control characters, `*`, `?`, `"`, `<`, `>`, `|`, and colon outside an ASCII-letter drive designator. `GetFullPathNameW` evaluates relative components and endpoint trimming first; before this package removes a trailing separator, it rejects any component that still ends in a space or period while a separator follows it. This includes an ordinary trailing component and a UNC share before a descendant or trailing separator. A separator-free final component can normalize to a stable endpoint, and a component removed by navigation is absent from the guarded result. Consequently, every non-root `AbsolutePath` has an identity-preserving immediate parent and `TryGetParent` returns `false` only for a filesystem root. Alternate-data-stream syntax, device-namespace paths, and reserved DOS device names (`CON`, `PRN`, `AUX`, `NUL`, `CONIN$`, `CONOUT$`, `COM1`–`COM9`, `COM¹`/`COM²`/`COM³`, `LPT1`–`LPT9`, and `LPT¹`/`LPT²`/`LPT³`, including extensions) are outside this package's ordinary-path contract. Normalization does not verify existence or convert short and long names; short-name-looking segments such as `PROGRA~1` therefore retain their text and casing.
- On Unix, `/` is the directory separator and filesystem root, `\`, whitespace, and control characters other than NUL remain ordinary filename characters, and path identity is case-sensitive.

This ordinary-name boundary follows Microsoft's [Win32 naming guidance](https://learn.microsoft.com/en-us/windows/win32/fileio/naming-a-file); the normalization order and separator-free endpoint distinction follow the [.NET path-normalization trim rules](https://learn.microsoft.com/en-us/dotnet/standard/io/file-path-formats#trim-characters).

`RootRelativePath` stores recognized directory separators as `/`; it does not reinterpret a character that the running operating system treats as part of a filename. On Windows, the same post-normalization check applies after relative components and endpoint trimming are evaluated. An input whose final ordinary segment disappears entirely, including `. `, `.. `, or `...`, is invalid; only exact `.` and `..` are navigation segments. A component removed by navigation is accepted only when it does not remain in the normalized value. A retained component ending in a space or period is rejected when a separator follows it, including a trailing separator; a separator-free final component can normalize to a stable endpoint. The resulting root-relative value therefore resolves under a boundary with the same lexical identity and cannot bypass the parent-closure invariant when combined with an `AbsolutePath`.

These are operating-system lexical rules, not observations of a mounted volume. The values do not detect whether an individual volume or directory uses different case-sensitivity behavior.

## Physical state is not retained

These values do not access the filesystem and do not guarantee:

- path existence;
- file, directory, symbolic-link, or special-node kind;
- permissions or accessibility;
- inode identity;
- identity after symbolic-link resolution;
- containment after symbolic-link resolution.

Observe any required physical state immediately before the operation that depends on it. When a race must be prevented, rely on an opened handle or the result of the operation itself.

The package intentionally contains no read or write operations, atomic publication, temporary-node cleanup, locking, access-control handling, storage layout, or general-purpose filesystem interface.

Use the `MackySoft.FileSystem.Physical` companion package, released at the same version, when an application needs product-independent entry observation, link-policy resolution, link-resolved containment snapshots, or complete single-file publication through a same-directory move or replacement.
