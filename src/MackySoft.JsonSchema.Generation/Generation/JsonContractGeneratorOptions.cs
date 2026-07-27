using MackySoft.JsonSchema.Generation.Configuration;
using MackySoft.JsonSchema.Generation.Diagnostics;
using MackySoft.JsonSchema.Generation.Extensibility;
using MackySoft.JsonSchema.Generation.Internal.Determinism;

namespace MackySoft.JsonSchema.Generation;

/// <summary> Defines deterministic settings and explicit extension registrations for a generator. </summary>
public sealed class JsonContractGeneratorOptions
{
    /// <summary> Initializes generator options and validates extension identities. </summary>
    /// <param name="settings"> JSON-value semantics shared by every generated contract. </param>
    /// <param name="metadataProviders"> Optional metadata providers. </param>
    /// <param name="typeMappers"> Optional converter and value-object mappings. </param>
    /// <param name="modelContributors"> Optional product metadata contributors. </param>
    /// <param name="documentPostProcessors"> Optional delivery-only schema annotation providers. </param>
    /// <exception cref="ArgumentNullException"> <paramref name="settings" /> is <see langword="null" />. </exception>
    /// <exception cref="JsonContractGenerationException">
    /// An extension identity is invalid or duplicated within one extension category.
    /// </exception>
    public JsonContractGeneratorOptions (
        JsonContractGenerationSettings settings,
        IEnumerable<IJsonContractMetadataProvider>? metadataProviders = null,
        IEnumerable<IJsonContractTypeMapper>? typeMappers = null,
        IEnumerable<IJsonContractModelContributor>? modelContributors = null,
        IEnumerable<IJsonSchemaDocumentPostProcessor>? documentPostProcessors = null)
    {
        Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        MetadataProviders = NormalizeExtensions(
            metadataProviders,
            nameof(metadataProviders),
            "metadataProvider");
        TypeMappers = NormalizeExtensions(
            typeMappers,
            nameof(typeMappers),
            "typeMapper");
        ModelContributors = NormalizeExtensions(
            modelContributors,
            nameof(modelContributors),
            "modelContributor");
        DocumentPostProcessors = NormalizeExtensions(
            documentPostProcessors,
            nameof(documentPostProcessors),
            "documentPostProcessor");
    }

    /// <summary> Gets JSON-value generation semantics. </summary>
    public JsonContractGenerationSettings Settings { get; }

    /// <summary> Gets metadata providers ordered by stable ID. </summary>
    public IReadOnlyList<IJsonContractMetadataProvider> MetadataProviders { get; }

    /// <summary> Gets type mappers ordered by stable ID. </summary>
    public IReadOnlyList<IJsonContractTypeMapper> TypeMappers { get; }

    /// <summary> Gets model contributors ordered by stable ID. </summary>
    public IReadOnlyList<IJsonContractModelContributor> ModelContributors { get; }

    /// <summary> Gets document post-processors ordered by stable ID. </summary>
    public IReadOnlyList<IJsonSchemaDocumentPostProcessor> DocumentPostProcessors { get; }

    private static IReadOnlyList<TExtension> NormalizeExtensions<TExtension> (
        IEnumerable<TExtension>? extensions,
        string parameterName,
        string extensionKind)
        where TExtension : class, IJsonContractExtension
    {
        TExtension[] values = extensions?.ToArray() ?? Array.Empty<TExtension>();
        for (int index = 0; index < values.Length; index++)
        {
            TExtension? extension = values[index];
            if (extension == null)
            {
                throw new ArgumentException(
                    "An extension collection cannot contain null.",
                    parameterName);
            }

            ValidateExtensionIdentity(extension, extensionKind);
        }

        Array.Sort(
            values,
            static (left, right) =>
                UnicodeCodePointComparer.Instance.Compare(left.StableId, right.StableId));

        for (int index = 1; index < values.Length; index++)
        {
            if (string.Equals(
                values[index - 1].StableId,
                values[index].StableId,
                StringComparison.Ordinal))
            {
                throw new JsonContractGenerationException(
                    JsonContractGenerationFailureKind.DuplicateExtensionId,
                    $"Extension kind '{extensionKind}' contains duplicate stable ID '{values[index].StableId}'.",
                    sourceIds: new[] { values[index].StableId });
            }
        }

        return Array.AsReadOnly(values);
    }

    private static void ValidateExtensionIdentity (
        IJsonContractExtension extension,
        string extensionKind)
    {
        if (!IsStableIdentityText(extension.StableId, maximumLength: 256)
            || !IsStableIdentityText(extension.ContractVersion, maximumLength: 64))
        {
            throw new JsonContractGenerationException(
                JsonContractGenerationFailureKind.InvalidExtensionIdentity,
                $"Extension kind '{extensionKind}' must declare a non-empty stable ID and contract version without outer whitespace.",
                sourceIds: string.IsNullOrEmpty(extension.StableId)
                    ? Array.Empty<string>()
                    : new[] { extension.StableId });
        }
    }

    private static bool IsStableIdentityText (
        string? value,
        int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value)
            || value.Length > maximumLength
            || char.IsWhiteSpace(value[0])
            || char.IsWhiteSpace(value[value.Length - 1]))
        {
            return false;
        }

        try
        {
            _ = UnicodeCodePointComparer.Instance.Compare(value, value);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
