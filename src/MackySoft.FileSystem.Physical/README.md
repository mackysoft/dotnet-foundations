# MackySoft.FileSystem.Physical

`MackySoft.FileSystem.Physical` observes current filesystem state, resolves links under a typed lexical boundary, and publishes complete files through a same-directory move or replacement operation.

Install an exact package version:

```bash
dotnet add package MackySoft.FileSystem.Physical --version <version>
```

The package is released at the same exact version as `MackySoft.FileSystem` and depends on that version for `AbsolutePath`, `RootRelativePath`, and `ContainedPath`. It does not accept raw path strings at its public boundaries.

## Entry observation

`FileSystemEntryInspector.TryInspect` observes one `AbsolutePath` without following the final path segment. A missing entry is a successful `FileSystemEntryState.Missing` observation. Access denial, unsupported platform behavior, and input/output failures are returned as `FileSystemOperationFailure` values.

The entry states distinguish regular files, directories, symbolic links or Windows junctions, other Windows reparse points, other node kinds, and missing entries. The Unix implementation uses no-follow native metadata so that devices, sockets, and named pipes are not classified as regular files.

## Link-resolved containment

`PhysicalPathResolver.TryResolve` accepts a lexical `ContainedPath` and requires explicit `SymbolicLinkHandling` and `MissingPathHandling` policies. Ancestors above the supplied boundary are resolved only to establish the link-resolved boundary path. The selected link policy applies to the boundary itself and paths below it. `Follow` resolves symbolic links and Windows junctions; another Windows reparse-point kind produces `UnexpectedEntryKind`.

When links are followed, both the boundary and target are resolved before containment is checked. Containment uses the current operating system's lexical path identity rules supplied by `MackySoft.FileSystem`; it does not inspect case-sensitivity overrides of an individual mounted volume. A link that resolves the target outside that resolved lexical boundary produces `FileSystemOperationFailureKind.OutsideBoundary`. `AllowMissingTail` resolves every existing prefix and retains the remaining lexical tail.

## Atomic single-file publication

`AtomicFilePublisher.PublishAsync` writes a borrowed readable stream to a unique sibling file and publishes the completed file by a same-directory move or replacement. `AtomicFilePublication` requires explicit policies for links, an existing target, and missing parent directories. At each observed snapshot, existing directories, links, reparse points, devices, sockets, and named pipes are rejected as regular-file targets.

The borrowed stream remains open, and exceptions raised while reading it propagate without being classified as target-filesystem failures. Publication does not guarantee preservation of target metadata or durability of directory metadata. A temporary file is removed on a failed operation when cleanup succeeds. Parent directories created by `MissingParentHandling.Create` remain after a later failure or cancellation.

The operating-system move or replacement gives atomic visibility only when the underlying filesystem provider guarantees that behavior for the temporary sibling and target. The package does not detect or strengthen provider-specific atomicity guarantees.

## Lifetime and scope

Physical observations and resolutions are snapshots; they do not reserve a path or prevent later filesystem changes. Publication re-resolves its target after writing the temporary file and reports a detected change in the resolved path value or required entry state, but path-based operating-system APIs cannot reserve every segment against hostile concurrent replacement.

The package does not define product storage layouts, required product files, domain diagnostics, multi-file transactions, locking, access-control policy, transports, watchers, or a general-purpose filesystem abstraction.
