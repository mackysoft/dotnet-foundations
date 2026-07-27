using System.Text.Json;
using MackySoft.JsonSchema.Generation.ContractModel;
using MackySoft.Text.Vocabularies;

namespace MackySoft.JsonSchema.Generation.Internal.Determinism.Semantics;

/// <summary>
/// Writes the source-independent semantic representation shared by deterministic model projections.
/// </summary>
internal static class ContractModelSemanticWriter
{
    public static void WriteModel (
        Utf8JsonWriter writer,
        JsonContractModel model)
    {
        if (writer is null)
        {
            throw new ArgumentNullException(nameof(writer));
        }

        if (model is null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        writer.WriteStartObject();
        writer.WriteString("contractId", model.ContractId);

        writer.WritePropertyName("root");
        WriteNode(writer, model.Root, encodeJsonValueSemantics: true);

        writer.WritePropertyName("definitions");
        writer.WriteStartArray();
        foreach (JsonContractDefinition definition in model.Definitions)
        {
            writer.WriteStartObject();
            writer.WriteString("id", definition.Id);
            writer.WritePropertyName("value");
            WriteNode(writer, definition.Value, encodeJsonValueSemantics: true);
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
            JsonSemanticValueCanonicalizer.WriteValue(
                writer,
                contribution.Value);
            writer.WriteString("sourceId", contribution.SourceId);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    public static void WriteNode (
        Utf8JsonWriter writer,
        JsonContractNode node)
    {
        WriteNode(writer, node, encodeJsonValueSemantics: false);
    }

    private static void WriteNode (
        Utf8JsonWriter writer,
        JsonContractNode node,
        bool encodeJsonValueSemantics)
    {
        if (writer is null)
        {
            throw new ArgumentNullException(nameof(writer));
        }

        if (node is null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        writer.WriteStartObject();
        writer.WriteString("kind", Vocabulary.GetText(node.Kind));
        writer.WriteBoolean("isNullable", node.IsNullable);

        if (node.ScalarKind.HasValue)
        {
            writer.WriteString("scalarKind", Vocabulary.GetText(node.ScalarKind.Value));
        }
        else
        {
            writer.WriteNull("scalarKind");
        }

        writer.WritePropertyName("annotations");
        WriteAnnotations(
            writer,
            node.Annotations,
            encodeJsonValueSemantics);

        writer.WritePropertyName("constraints");
        WriteConstraints(
            writer,
            node.Constraints,
            encodeJsonValueSemantics);

        writer.WritePropertyName("constant");
        WriteNullableJsonElement(
            writer,
            node.Constant,
            encodeJsonValueSemantics);

        writer.WritePropertyName("allowedValues");
        WriteJsonElements(
            writer,
            node.AllowedValues,
            encodeJsonValueSemantics);

        if (node.ReferenceId is null)
        {
            writer.WriteNull("referenceId");
        }
        else
        {
            writer.WriteString("referenceId", node.ReferenceId);
        }

        writer.WritePropertyName("items");
        WriteNullableNode(
            writer,
            node.Items,
            encodeJsonValueSemantics);

        writer.WritePropertyName("additionalProperties");
        WriteNullableNode(
            writer,
            node.AdditionalProperties,
            encodeJsonValueSemantics);

        writer.WritePropertyName("properties");
        writer.WriteStartArray();
        foreach (JsonContractProperty property in node.Properties)
        {
            writer.WriteStartObject();
            writer.WriteString("name", property.Name);
            writer.WriteBoolean("isRequired", property.IsRequired);
            writer.WritePropertyName("value");
            WriteNode(
                writer,
                property.Value,
                encodeJsonValueSemantics);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();

        writer.WritePropertyName("variants");
        writer.WriteStartArray();
        foreach (JsonContractVariant variant in node.Variants)
        {
            writer.WriteStartObject();
            writer.WriteString("name", variant.Name);
            writer.WritePropertyName("value");
            WriteNullableNode(
                writer,
                variant.Value,
                encodeJsonValueSemantics);

            writer.WritePropertyName("requiredProperties");
            writer.WriteStartArray();
            foreach (string requiredProperty in variant.RequiredProperties)
            {
                writer.WriteStringValue(requiredProperty);
            }

            writer.WriteEndArray();

            writer.WritePropertyName("discriminatorValue");
            WriteNullableJsonElement(
                writer,
                variant.DiscriminatorValue,
                encodeJsonValueSemantics);

            writer.WritePropertyName("annotations");
            WriteAnnotations(
                writer,
                variant.Annotations,
                encodeJsonValueSemantics);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();

        writer.WritePropertyName("discriminator");
        if (node.Discriminator is null)
        {
            writer.WriteNullValue();
        }
        else
        {
            writer.WriteStartObject();
            writer.WriteString("propertyName", node.Discriminator.PropertyName);
            writer.WriteEndObject();
        }

        writer.WriteEndObject();
    }

    private static void WriteAnnotations (
        Utf8JsonWriter writer,
        JsonContractAnnotations annotations,
        bool encodeJsonValueSemantics)
    {
        writer.WriteStartObject();

        if (annotations.Title is null)
        {
            writer.WriteNull("title");
        }
        else
        {
            writer.WriteString("title", annotations.Title);
        }

        if (annotations.Description is null)
        {
            writer.WriteNull("description");
        }
        else
        {
            writer.WriteString("description", annotations.Description);
        }

        writer.WritePropertyName("examples");
        WriteJsonElements(
            writer,
            annotations.Examples,
            encodeJsonValueSemantics);
        writer.WriteEndObject();
    }

    private static void WriteConstraints (
        Utf8JsonWriter writer,
        JsonContractConstraints constraints,
        bool encodeJsonValueSemantics)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("minimum");
        WriteNullableJsonElement(
            writer,
            constraints.Minimum,
            encodeJsonValueSemantics);
        writer.WritePropertyName("exclusiveMinimum");
        WriteNullableJsonElement(
            writer,
            constraints.ExclusiveMinimum,
            encodeJsonValueSemantics);
        writer.WritePropertyName("maximum");
        WriteNullableJsonElement(
            writer,
            constraints.Maximum,
            encodeJsonValueSemantics);
        writer.WritePropertyName("exclusiveMaximum");
        WriteNullableJsonElement(
            writer,
            constraints.ExclusiveMaximum,
            encodeJsonValueSemantics);
        WriteNullableInt32(writer, "minimumLength", constraints.MinimumLength);
        WriteNullableInt32(writer, "maximumLength", constraints.MaximumLength);
        WriteNullableInt32(writer, "minimumItems", constraints.MinimumItems);
        WriteNullableInt32(writer, "maximumItems", constraints.MaximumItems);
        WriteNullableInt32(writer, "minimumProperties", constraints.MinimumProperties);
        WriteNullableInt32(writer, "maximumProperties", constraints.MaximumProperties);

        if (constraints.Pattern is null)
        {
            writer.WriteNull("pattern");
        }
        else
        {
            writer.WriteString("pattern", constraints.Pattern);
        }

        if (constraints.Format is null)
        {
            writer.WriteNull("format");
        }
        else
        {
            writer.WriteString("format", constraints.Format);
        }

        writer.WriteEndObject();
    }

    private static void WriteNullableNode (
        Utf8JsonWriter writer,
        JsonContractNode? node,
        bool encodeJsonValueSemantics)
    {
        if (node is null)
        {
            writer.WriteNullValue();
        }
        else
        {
            WriteNode(writer, node, encodeJsonValueSemantics);
        }
    }

    private static void WriteNullableJsonElement (
        Utf8JsonWriter writer,
        JsonElement? value,
        bool encodeJsonValueSemantics)
    {
        if (value.HasValue)
        {
            WriteJsonElement(
                writer,
                value.Value,
                encodeJsonValueSemantics);
        }
        else
        {
            writer.WriteNullValue();
        }
    }

    private static void WriteJsonElements (
        Utf8JsonWriter writer,
        IReadOnlyList<JsonElement> values,
        bool encodeJsonValueSemantics)
    {
        writer.WriteStartArray();
        foreach (JsonElement value in values)
        {
            WriteJsonElement(
                writer,
                value,
                encodeJsonValueSemantics);
        }

        writer.WriteEndArray();
    }

    private static void WriteJsonElement (
        Utf8JsonWriter writer,
        JsonElement value,
        bool encodeJsonValueSemantics)
    {
        if (encodeJsonValueSemantics)
        {
            JsonSemanticValueCanonicalizer.WriteValue(writer, value);
        }
        else
        {
            value.WriteTo(writer);
        }
    }

    private static void WriteNullableInt32 (
        Utf8JsonWriter writer,
        string propertyName,
        int? value)
    {
        if (value.HasValue)
        {
            writer.WriteNumber(propertyName, value.Value);
        }
        else
        {
            writer.WriteNull(propertyName);
        }
    }
}
