using System.Reflection;
using MackySoft.JsonSchema.Generation.Diagnostics;
using MackySoft.JsonSchema.Generation.Extensibility;
using MackySoft.JsonSchema.Generation.Internal.Determinism;
using MackySoft.JsonSchema.Generation.Internal.Metadata.Contracts;
using MackySoft.JsonSchema.Generation.Metadata;

namespace MackySoft.JsonSchema.Generation.Internal.Metadata.Declarations;

internal sealed class ProviderMetadataDeclarationCollector
{
    private readonly IReadOnlyList<IJsonContractMetadataProvider> providers;

    internal ProviderMetadataDeclarationCollector (
        IReadOnlyList<IJsonContractMetadataProvider> metadataProviders)
    {
        if (metadataProviders is null)
        {
            throw new ArgumentNullException(nameof(metadataProviders));
        }

        IJsonContractMetadataProvider[] copy = metadataProviders.ToArray();
        if (copy.Any(static provider => provider is null))
        {
            throw new ArgumentException(
                "The metadata provider collection must not contain null values.",
                nameof(metadataProviders));
        }

        Array.Sort(
            copy,
            static (left, right) => UnicodeCodePointComparer.Instance.Compare(
                left.StableId,
                right.StableId));
        this.providers = Array.AsReadOnly(copy);
    }

    internal void Collect (
        MetadataResolutionTarget target,
        MemberInfo? member,
        MetadataDeclarationSet declarations)
    {
        var context = new JsonContractMetadataContext(
            target.TargetType,
            member,
            target.JsonPropertyName);

        foreach (IJsonContractMetadataProvider provider in providers)
        {
            CollectSnapshot(target, provider, context, declarations);
        }
    }

    private static void CollectSnapshot (
        MetadataResolutionTarget target,
        IJsonContractMetadataProvider provider,
        JsonContractMetadataContext context,
        MetadataDeclarationSet declarations)
    {
        string? sourceId = provider.StableId;
        if (sourceId is null)
        {
            throw MetadataFailure.Invalid(
                target,
                metadataKind: null,
                sourceIds: Array.Empty<string>(),
                "A metadata provider returned a null stable identifier.");
        }

        IReadOnlyList<JsonContractMetadata>? snapshot;
        try
        {
            snapshot = provider.GetMetadata(context);
        }
        catch (JsonContractGenerationException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw MetadataFailure.Invalid(
                target,
                metadataKind: null,
                new[] { sourceId },
                $"Metadata provider '{sourceId}' failed to produce a snapshot.",
                exception);
        }

        if (snapshot is null)
        {
            throw MetadataFailure.Invalid(
                target,
                metadataKind: null,
                new[] { sourceId },
                $"Metadata provider '{sourceId}' returned a null snapshot.");
        }

        JsonContractMetadata[] copy;
        try
        {
            copy = snapshot.ToArray();
        }
        catch (Exception exception)
        {
            throw MetadataFailure.Invalid(
                target,
                metadataKind: null,
                new[] { sourceId },
                $"Metadata provider '{sourceId}' returned an unreadable snapshot.",
                exception);
        }

        if (copy.Any(static metadata => metadata is null))
        {
            throw MetadataFailure.Invalid(
                target,
                metadataKind: null,
                new[] { sourceId },
                $"Metadata provider '{sourceId}' returned a snapshot containing null.");
        }

        foreach (JsonContractMetadata metadata in copy)
        {
            CollectDeclaration(
                target,
                sourceId,
                metadata,
                declarations);
        }
    }

    private static void CollectDeclaration (
        MetadataResolutionTarget target,
        string sourceId,
        JsonContractMetadata metadata,
        MetadataDeclarationSet declarations)
    {
        switch (metadata.Kind)
        {
            case JsonContractMetadataKind.OneOfBranch:
                JsonContractBranchMetadata branch = metadata.BranchValue
                    ?? throw MetadataFailure.Invalid(
                        target,
                        JsonContractMetadataKind.OneOfBranch,
                        new[] { sourceId },
                        "A provider oneOf branch declaration has no branch payload.");
                declarations.Add(
                    new ResolvedContractMetadata.OneOfBranchProvenance(
                        sourceId,
                        OneOfBranchDeclarationNormalizer.Normalize(
                            target,
                            branch,
                            sourceId)));
                break;

            case JsonContractMetadataKind.Discriminator:
                string propertyName = metadata.StringValue
                    ?? throw MetadataFailure.Invalid(
                        target,
                        JsonContractMetadataKind.Discriminator,
                        new[] { sourceId },
                        "A provider discriminator declaration has no property name.");
                MetadataTextContract.Validate(
                    propertyName,
                    target,
                    JsonContractMetadataKind.Discriminator,
                    sourceId,
                    "The discriminator property name");
                declarations.Add(
                    new ResolvedContractMetadata.DiscriminatorProvenance(
                        sourceId,
                        propertyName));
                break;

            default:
                declarations.Add(
                    new ResolvedContractMetadata.MetadataProvenance(
                        sourceId,
                        metadata));
                break;
        }
    }

}
