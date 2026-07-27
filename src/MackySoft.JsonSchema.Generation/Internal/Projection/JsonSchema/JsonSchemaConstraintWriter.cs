using System.Text.Json;
using System.Text.Json.Nodes;
using MackySoft.JsonSchema.Generation.ContractModel;

namespace MackySoft.JsonSchema.Generation.Internal.Projection.JsonSchema;

/// <summary>Writes normalized validation constraints as JSON Schema keywords.</summary>
internal static class JsonSchemaConstraintWriter
{
    internal static void Write (
        JsonObject schema,
        JsonContractConstraints constraints)
    {
        AddJsonElement(schema, "minimum", constraints.Minimum);
        AddJsonElement(schema, "exclusiveMinimum", constraints.ExclusiveMinimum);
        AddJsonElement(schema, "maximum", constraints.Maximum);
        AddJsonElement(schema, "exclusiveMaximum", constraints.ExclusiveMaximum);
        AddInt32(schema, "minLength", constraints.MinimumLength);
        AddInt32(schema, "maxLength", constraints.MaximumLength);
        AddInt32(schema, "minItems", constraints.MinimumItems);
        AddInt32(schema, "maxItems", constraints.MaximumItems);
        AddInt32(schema, "minProperties", constraints.MinimumProperties);
        AddInt32(schema, "maxProperties", constraints.MaximumProperties);

        if (constraints.Pattern is not null)
        {
            schema.Add("pattern", constraints.Pattern);
        }

        if (constraints.Format is not null)
        {
            schema.Add("format", constraints.Format);
        }
    }

    private static void AddJsonElement (
        JsonObject schema,
        string propertyName,
        JsonElement? value)
    {
        if (value.HasValue)
        {
            schema.Add(propertyName, ToJsonNode(value.Value));
        }
    }

    private static void AddInt32 (
        JsonObject schema,
        string propertyName,
        int? value)
    {
        if (value.HasValue)
        {
            schema.Add(propertyName, value.Value);
        }
    }

    private static JsonNode? ToJsonNode (JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Undefined)
        {
            throw new InvalidOperationException(
                "An undefined JSON value cannot be projected to a JSON Schema document.");
        }

        return value.ValueKind == JsonValueKind.Null
            ? null
            : JsonNode.Parse(value.GetRawText());
    }
}
