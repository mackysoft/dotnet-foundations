namespace MackySoft.FileSystem;

/// <summary> Defines the target and required policies for one complete single-file publication. </summary>
public sealed class AtomicFilePublication
{
    /// <summary> Creates a publication contract for a non-root target below a lexical boundary. </summary>
    /// <param name="targetPath"> The lexical boundary and file target. </param>
    /// <param name="symbolicLinkHandling">
    /// The required behavior for the boundary entry and existing links or reparse points below it.
    /// </param>
    /// <param name="existingTargetHandling"> The required behavior when the resolved target is a regular file. </param>
    /// <param name="missingParentHandling"> The required behavior when the resolved parent is missing. </param>
    /// <exception cref="ArgumentNullException"> <paramref name="targetPath" /> is <see langword="null" />. </exception>
    /// <exception cref="ArgumentException"> <paramref name="targetPath" /> identifies its boundary root. </exception>
    /// <exception cref="ArgumentOutOfRangeException"> A supplied policy value is not defined. </exception>
    public AtomicFilePublication (
        ContainedPath targetPath,
        SymbolicLinkHandling symbolicLinkHandling,
        ExistingTargetHandling existingTargetHandling,
        MissingParentHandling missingParentHandling)
    {
        TargetPath = targetPath ?? throw new ArgumentNullException(nameof(targetPath));
        if (targetPath.RelativePath.IsRoot)
        {
            throw new ArgumentException("A publication target must be below its boundary root.", nameof(targetPath));
        }

        ValidatePolicy(symbolicLinkHandling, nameof(symbolicLinkHandling));
        ValidatePolicy(existingTargetHandling, nameof(existingTargetHandling));
        ValidatePolicy(missingParentHandling, nameof(missingParentHandling));
        SymbolicLinkHandling = symbolicLinkHandling;
        ExistingTargetHandling = existingTargetHandling;
        MissingParentHandling = missingParentHandling;
    }

    /// <summary> Gets the lexical boundary and file target. </summary>
    public ContainedPath TargetPath { get; }

    /// <summary> Gets the required behavior for the boundary entry and existing links or reparse points below it. </summary>
    public SymbolicLinkHandling SymbolicLinkHandling { get; }

    /// <summary> Gets the required behavior for an existing regular-file target. </summary>
    public ExistingTargetHandling ExistingTargetHandling { get; }

    /// <summary> Gets the required behavior for missing parent directories. </summary>
    public MissingParentHandling MissingParentHandling { get; }

    private static void ValidatePolicy<T> (
        T policy,
        string parameterName)
        where T : struct, Enum
    {
        if (!Enum.IsDefined(typeof(T), policy))
        {
            throw new ArgumentOutOfRangeException(parameterName, policy, "Policy must be defined.");
        }
    }
}
