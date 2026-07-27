using System.Buffers;
using System.Security.Cryptography;
using System.Text.Json;
using MackySoft.Json.Canonicalization;
using MackySoft.JsonSchema.Generation.Configuration;
using MackySoft.JsonSchema.Generation.ContractModel;
using MackySoft.JsonSchema.Generation.Diagnostics;
using MackySoft.JsonSchema.Generation.Extensibility;
using MackySoft.JsonSchema.Generation.Internal.Determinism.Semantics;
using MackySoft.Text.Vocabularies;

namespace MackySoft.JsonSchema.Generation.Internal.Determinism.Digests;

/// <summary> Calculates the contract identity from its closed semantic projection. </summary>
internal static class ContractDigestCalculator
{
    public static string Calculate (
        JsonContractModel model,
        JsonContractGenerationSettings settings,
        IReadOnlyList<IJsonContractMetadataProvider> metadataProviders,
        IReadOnlyList<IJsonContractTypeMapper> typeMappers,
        IReadOnlyList<IJsonContractModelContributor> modelContributors)
    {
        if (model is null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        if (settings is null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        if (metadataProviders is null)
        {
            throw new ArgumentNullException(nameof(metadataProviders));
        }

        if (typeMappers is null)
        {
            throw new ArgumentNullException(nameof(typeMappers));
        }

        if (modelContributors is null)
        {
            throw new ArgumentNullException(nameof(modelContributors));
        }

        try
        {
            byte[] semanticProjection = WriteSemanticProjection(
                model,
                settings,
                metadataProviders,
                typeMappers,
                modelContributors);

            byte[] canonicalProjection =
                Rfc8785JsonCanonicalizer.Canonicalize(semanticProjection);

            using SHA256 sha256 = SHA256.Create();
            byte[] digest = sha256.ComputeHash(canonicalProjection);
            return BitConverter
                .ToString(digest)
                .Replace("-", string.Empty)
                .ToLowerInvariant();
        }
        catch (Exception exception) when (
            exception is JsonException
            or JsonCanonicalizationException
            or CryptographicException
            or ArgumentException
            or InvalidOperationException
            or NotSupportedException)
        {
            throw new JsonContractGenerationException(
                JsonContractGenerationFailureKind.DigestGenerationFailed,
                $"The semantic projection for contract '{model.ContractId}' could not be canonicalized and hashed.",
                contractId: model.ContractId,
                innerException: exception);
        }
    }

    private static byte[] WriteSemanticProjection (
        JsonContractModel model,
        JsonContractGenerationSettings settings,
        IReadOnlyList<IJsonContractMetadataProvider> metadataProviders,
        IReadOnlyList<IJsonContractTypeMapper> typeMappers,
        IReadOnlyList<IJsonContractModelContributor> modelContributors)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer);

        writer.WriteStartObject();
        writer.WritePropertyName("model");
        ContractModelSemanticWriter.WriteModel(writer, model);

        writer.WritePropertyName("settings");
        writer.WriteStartObject();
        writer.WriteString("dialect", settings.Dialect);
        writer.WriteString("objectClosure", Vocabulary.GetText(settings.ObjectClosure));
        writer.WriteString(
            "nullabilityProjection",
            Vocabulary.GetText(settings.NullabilityProjection));
        writer.WriteString(
            "referenceProjection",
            Vocabulary.GetText(settings.ReferenceProjection));
        WriteExtensionIdentities(writer, "metadataProviders", metadataProviders);
        WriteExtensionIdentities(writer, "typeMappers", typeMappers);
        WriteExtensionIdentities(writer, "modelContributors", modelContributors);
        writer.WriteEndObject();
        writer.WriteEndObject();
        writer.Flush();

        return buffer.WrittenSpan.ToArray();
    }

    private static void WriteExtensionIdentities<TExtension> (
        Utf8JsonWriter writer,
        string propertyName,
        IReadOnlyList<TExtension> extensions)
        where TExtension : IJsonContractExtension
    {
        TExtension[] orderedExtensions = extensions.ToArray();
        Array.Sort(
            orderedExtensions,
            static (left, right) =>
            {
                int idComparison = UnicodeCodePointComparer.Instance.Compare(
                    left.StableId,
                    right.StableId);
                return idComparison != 0
                    ? idComparison
                    : UnicodeCodePointComparer.Instance.Compare(
                        left.ContractVersion,
                        right.ContractVersion);
            });

        writer.WritePropertyName(propertyName);
        writer.WriteStartArray();
        foreach (TExtension extension in orderedExtensions)
        {
            writer.WriteStartObject();
            writer.WriteString("stableId", extension.StableId);
            writer.WriteString("contractVersion", extension.ContractVersion);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }
}
