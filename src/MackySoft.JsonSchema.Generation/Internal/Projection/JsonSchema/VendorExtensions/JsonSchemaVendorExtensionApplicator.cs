using System.Text.Json;
using System.Text.Json.Nodes;
using MackySoft.JsonSchema.Generation.ContractModel;
using MackySoft.JsonSchema.Generation.Extensibility;
using MackySoft.JsonSchema.Generation.Internal.Determinism;

namespace MackySoft.JsonSchema.Generation.Internal.Projection.JsonSchema.VendorExtensions;

/// <summary>Invokes document post-processors against the immutable base schema and applies their declarations.</summary>
internal static class JsonSchemaVendorExtensionApplicator
{
    private const string ProcessorIdentityAnnotationName =
        "x-document-post-processors";

    public static void Apply (
        JsonObject document,
        byte[] baseDocumentUtf8,
        JsonContractModel model,
        IReadOnlyList<IJsonSchemaDocumentPostProcessor> processors)
    {
        using JsonDocument baseDocument = JsonDocument.Parse(baseDocumentUtf8);
        var context = new JsonSchemaDocumentContext(model, baseDocument.RootElement);

        IJsonSchemaDocumentPostProcessor[] orderedProcessors = processors.ToArray();
        Array.Sort(
            orderedProcessors,
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

        AddProcessorIdentityAnnotation(document, orderedProcessors);
        var declarations = new VendorExtensionDeclarationSet(
            document,
            model.ContractId);

        foreach (IJsonSchemaDocumentPostProcessor processor in orderedProcessors)
        {
            JsonSchemaVendorExtension?[]? processorExtensions;
            try
            {
                processorExtensions = processor
                    .GetVendorExtensions(context)
                    ?.ToArray();
            }
            catch (Exception exception)
            {
                throw VendorExtensionFailure.Invalid(
                    model.ContractId,
                    processor.StableId,
                    $"Document post-processor '{processor.StableId}' failed while declaring vendor extensions.",
                    exception);
            }

            if (processorExtensions is null)
            {
                throw VendorExtensionFailure.Invalid(
                    model.ContractId,
                    processor.StableId,
                    $"Document post-processor '{processor.StableId}' returned a null vendor-extension list.");
            }

            foreach (JsonSchemaVendorExtension? extension in processorExtensions)
            {
                declarations.Add(processor.StableId, extension);
            }
        }

        declarations.ApplyTo(document);
    }

    private static void AddProcessorIdentityAnnotation (
        JsonObject document,
        IReadOnlyList<IJsonSchemaDocumentPostProcessor> processors)
    {
        var identities = new JsonArray();
        foreach (IJsonSchemaDocumentPostProcessor processor in processors)
        {
            identities.Add(
                new JsonObject
                {
                    ["stableId"] = processor.StableId,
                    ["contractVersion"] = processor.ContractVersion,
                });
        }

        document.Add(ProcessorIdentityAnnotationName, identities);
    }
}
