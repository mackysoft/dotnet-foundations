using System.Buffers;
using System.Text.Json;
using System.Text.Json.Nodes;
using MackySoft.JsonSchema.Generation.Configuration;
using MackySoft.JsonSchema.Generation.ContractModel;
using MackySoft.JsonSchema.Generation.Extensibility;
using MackySoft.JsonSchema.Generation.Internal.Projection.JsonSchema.VendorExtensions;
using MackySoft.JsonSchema.Generation.Projection;

namespace MackySoft.JsonSchema.Generation.Internal.Projection.JsonSchema;

/// <summary>Orchestrates deterministic JSON Schema Draft 2020-12 document emission.</summary>
internal static class JsonSchemaEmitter
{
    public static byte[] Emit (
        JsonContractModel model,
        JsonContractGenerationSettings settings,
        JsonSchemaDocumentOptions documentOptions,
        IReadOnlyList<IJsonSchemaDocumentPostProcessor> processors)
    {
        if (model is null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        if (settings is null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        if (documentOptions is null)
        {
            throw new ArgumentNullException(nameof(documentOptions));
        }

        if (processors is null)
        {
            throw new ArgumentNullException(nameof(processors));
        }

        JsonObject document = CreateBaseDocument(model, settings, documentOptions);
        byte[] baseDocumentUtf8 = Serialize(document);
        if (processors.Count == 0)
        {
            return baseDocumentUtf8;
        }

        JsonSchemaVendorExtensionApplicator.Apply(
            document,
            baseDocumentUtf8,
            model,
            processors);
        return Serialize(document);
    }

    private static JsonObject CreateBaseDocument (
        JsonContractModel model,
        JsonContractGenerationSettings settings,
        JsonSchemaDocumentOptions options)
    {
        var document = new JsonObject();
        if (options.Kind == JsonSchemaDocumentKind.Complete)
        {
            document.Add("$schema", settings.Dialect);
            if (options.Id is not null)
            {
                document.Add("$id", options.Id);
            }
        }

        document.Add("x-contract-id", model.ContractId);
        document.Add("x-contract-digest", model.ContractDigest);
        JsonSchemaNodeWriter.Write(document, model.Root, settings);

        if (model.Definitions.Count != 0)
        {
            var definitions = new JsonObject();
            foreach (JsonContractDefinition definition in model.Definitions)
            {
                definitions.Add(
                    definition.Id,
                    JsonSchemaNodeWriter.Create(definition.Value, settings));
            }

            document.Add("$defs", definitions);
        }

        return document;
    }

    private static byte[] Serialize (JsonNode document)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer);
        document.WriteTo(writer);
        writer.Flush();
        return buffer.WrittenSpan.ToArray();
    }
}
