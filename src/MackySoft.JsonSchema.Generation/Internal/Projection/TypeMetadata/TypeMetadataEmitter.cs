using System.Buffers;
using System.Text.Json;
using MackySoft.JsonSchema.Generation.ContractModel;
using MackySoft.JsonSchema.Generation.Internal.Determinism.Semantics;
using MackySoft.JsonSchema.Generation.Projection;

namespace MackySoft.JsonSchema.Generation.Internal.Projection.TypeMetadata;

/// <summary> Projects describe-oriented metadata directly from the normalized contract model. </summary>
internal static class TypeMetadataEmitter
{
    public static byte[] Emit (
        JsonContractModel model,
        JsonSchemaDocumentOptions options)
    {
        if (model is null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer);

        writer.WriteStartObject();
        writer.WriteString("contractId", model.ContractId);
        writer.WriteString("contractDigest", model.ContractDigest);

        if (options.LogicalName is null)
        {
            writer.WriteNull("schemaName");
        }
        else
        {
            writer.WriteString("schemaName", options.LogicalName);
        }

        writer.WritePropertyName("root");
        ContractModelSemanticWriter.WriteNode(writer, model.Root);

        writer.WritePropertyName("definitions");
        writer.WriteStartArray();
        foreach (JsonContractDefinition definition in model.Definitions)
        {
            writer.WriteStartObject();
            writer.WriteString("id", definition.Id);
            writer.WritePropertyName("value");
            ContractModelSemanticWriter.WriteNode(writer, definition.Value);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();

        writer.WritePropertyName("contributions");
        writer.WriteStartArray();
        foreach (JsonContractModelContribution contribution in model.Contributions)
        {
            writer.WriteStartObject();
            writer.WriteString("targetPointer", contribution.TargetPointer);
            writer.WriteString("name", contribution.Name);
            writer.WritePropertyName("value");
            contribution.Value.WriteTo(writer);
            writer.WriteString("sourceId", contribution.SourceId);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
        writer.Flush();

        return buffer.WrittenSpan.ToArray();
    }
}
