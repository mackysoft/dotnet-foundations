using System.Text.Json;
using System.Text.Json.Nodes;
using MackySoft.JsonSchema.Generation.Configuration;
using MackySoft.JsonSchema.Generation.ContractModel;
using MackySoft.Text.Vocabularies;

namespace MackySoft.JsonSchema.Generation.Internal.Projection.JsonSchema;

/// <summary>Writes JSON Schema semantics from normalized contract-model nodes.</summary>
internal static class JsonSchemaNodeWriter
{
    internal static JsonObject Create (
        JsonContractNode node,
        JsonContractGenerationSettings settings)
    {
        var schema = new JsonObject();
        Write(schema, node, settings);
        return schema;
    }

    internal static void Write (
        JsonObject schema,
        JsonContractNode node,
        JsonContractGenerationSettings settings)
    {
        WriteAnnotations(schema, node.Annotations);

        switch (node.Kind)
        {
            case JsonContractNodeKind.Arbitrary:
                break;

            case JsonContractNodeKind.Scalar:
                WriteScalarSchema(schema, node);
                break;

            case JsonContractNodeKind.Array:
                WriteArraySchema(schema, node, settings);
                break;

            case JsonContractNodeKind.Object:
                WriteObjectSchema(schema, node, settings);
                break;

            case JsonContractNodeKind.Dictionary:
                WriteDictionarySchema(schema, node, settings);
                break;

            case JsonContractNodeKind.Enum:
                WriteEnumSchema(schema, node);
                break;

            case JsonContractNodeKind.Const:
                WriteConstSchema(schema, node);
                break;

            case JsonContractNodeKind.Reference:
                WriteReferenceSchema(schema, node);
                break;

            case JsonContractNodeKind.OneOf:
                WriteOneOfSchema(schema, node, settings);
                break;

            default:
                throw new InvalidOperationException(
                    $"Contract node kind '{Vocabulary.GetText(node.Kind)}' cannot be projected to JSON Schema.");
        }

        JsonSchemaConstraintWriter.Write(schema, node.Constraints);
    }

    private static void WriteScalarSchema (
        JsonObject schema,
        JsonContractNode node)
    {
        JsonContractScalarKind scalarKind = RequireScalarKind(node);
        WriteType(schema, Vocabulary.GetText(scalarKind), node.IsNullable);
    }

    private static void WriteArraySchema (
        JsonObject schema,
        JsonContractNode node,
        JsonContractGenerationSettings settings)
    {
        WriteType(
            schema,
            Vocabulary.GetText(JsonContractNodeKind.Array),
            node.IsNullable);

        JsonContractNode items = node.Items
            ?? throw MissingNodeMember(node, nameof(node.Items));
        schema.Add("items", Create(items, settings));
    }

    private static void WriteObjectSchema (
        JsonObject schema,
        JsonContractNode node,
        JsonContractGenerationSettings settings)
    {
        WriteType(
            schema,
            Vocabulary.GetText(JsonContractNodeKind.Object),
            node.IsNullable);

        var properties = new JsonObject();
        foreach (JsonContractProperty property in node.Properties)
        {
            properties.Add(property.Name, Create(property.Value, settings));
        }

        schema.Add("properties", properties);
        WriteRequiredProperties(
            schema,
            node.Properties
                .Where(static property => property.IsRequired)
                .Select(static property => property.Name));

        if (node.AdditionalProperties is not null)
        {
            schema.Add(
                "additionalProperties",
                Create(node.AdditionalProperties, settings));
        }
        else
        {
            WriteObjectClosure(schema, settings.ObjectClosure);
        }

        if (node.Variants.Count != 0)
        {
            var variants = new JsonArray();
            foreach (JsonContractVariant variant in node.Variants)
            {
                variants.Add(CreatePropertyRequirementVariantSchema(node, variant));
            }

            if (node.IsNullable)
            {
                variants.Add(CreateNullSchema());
            }

            schema.Add("oneOf", variants);
        }
    }

    private static void WriteDictionarySchema (
        JsonObject schema,
        JsonContractNode node,
        JsonContractGenerationSettings settings)
    {
        WriteType(
            schema,
            Vocabulary.GetText(JsonContractNodeKind.Object),
            node.IsNullable);

        JsonContractNode additionalProperties = node.AdditionalProperties
            ?? throw MissingNodeMember(node, nameof(node.AdditionalProperties));
        schema.Add(
            "additionalProperties",
            Create(additionalProperties, settings));
    }

    private static void WriteEnumSchema (
        JsonObject schema,
        JsonContractNode node)
    {
        if (node.ScalarKind.HasValue)
        {
            WriteType(
                schema,
                Vocabulary.GetText(node.ScalarKind.Value),
                node.IsNullable);
        }

        var values = new JsonArray();
        bool containsNull = false;
        foreach (JsonElement value in node.AllowedValues)
        {
            containsNull |= value.ValueKind == JsonValueKind.Null;
            values.Add(ToJsonNode(value));
        }

        if (node.IsNullable && !containsNull)
        {
            values.Add(null);
        }

        schema.Add("enum", values);
    }

    private static void WriteConstSchema (
        JsonObject schema,
        JsonContractNode node)
    {
        JsonContractScalarKind? scalarKind = node.ScalarKind;
        JsonElement constant = node.Constant
            ?? throw MissingNodeMember(node, nameof(node.Constant));

        if (node.IsNullable && constant.ValueKind != JsonValueKind.Null)
        {
            var constantSchema = new JsonObject();
            if (scalarKind.HasValue)
            {
                WriteType(
                    constantSchema,
                    Vocabulary.GetText(scalarKind.Value),
                    isNullable: false);
            }

            constantSchema.Add("const", ToJsonNode(constant));
            schema.Add(
                "anyOf",
                new JsonArray
                {
                    constantSchema,
                    CreateNullSchema(),
                });
            return;
        }

        if (scalarKind.HasValue)
        {
            WriteType(
                schema,
                Vocabulary.GetText(scalarKind.Value),
                isNullable: false);
        }

        schema.Add("const", ToJsonNode(constant));
    }

    private static void WriteReferenceSchema (
        JsonObject schema,
        JsonContractNode node)
    {
        string referenceId = node.ReferenceId
            ?? throw MissingNodeMember(node, nameof(node.ReferenceId));
        string reference = "#/$defs/" + EncodeJsonPointerSegment(referenceId);

        if (node.IsNullable)
        {
            schema.Add(
                "anyOf",
                new JsonArray
                {
                    new JsonObject
                    {
                        ["$ref"] = reference,
                    },
                    CreateNullSchema(),
                });
            return;
        }

        schema.Add("$ref", reference);
    }

    private static void WriteOneOfSchema (
        JsonObject schema,
        JsonContractNode node,
        JsonContractGenerationSettings settings)
    {
        var variants = new JsonArray();
        foreach (JsonContractVariant variant in node.Variants)
        {
            variants.Add(
                CreatePolymorphicVariantSchema(
                    variant,
                    node.Discriminator,
                    settings));
        }

        if (node.IsNullable)
        {
            variants.Add(CreateNullSchema());
        }

        schema.Add("oneOf", variants);
    }

    private static JsonObject CreatePropertyRequirementVariantSchema (
        JsonContractNode containingObject,
        JsonContractVariant variant)
    {
        var schema = new JsonObject();
        WriteAnnotations(schema, variant.Annotations);
        WriteType(
            schema,
            Vocabulary.GetText(JsonContractNodeKind.Object),
            isNullable: false);

        IEnumerable<string> requiredProperties = variant.RequiredProperties;
        JsonContractDiscriminator? discriminator = containingObject.Discriminator;
        if (discriminator is not null && variant.DiscriminatorValue.HasValue)
        {
            schema.Add(
                "properties",
                CreateDiscriminatorProperties(
                    discriminator.PropertyName,
                    variant.DiscriminatorValue.Value));

            bool discriminatorIsRequired = containingObject.Properties.Any(
                property =>
                    property.IsRequired
                    && string.Equals(
                        property.Name,
                        discriminator.PropertyName,
                        StringComparison.Ordinal));
            if (discriminatorIsRequired)
            {
                requiredProperties = requiredProperties.Concat(
                    new[] { discriminator.PropertyName });
            }
        }

        WriteRequiredProperties(schema, requiredProperties);
        return schema;
    }

    private static JsonObject CreatePolymorphicVariantSchema (
        JsonContractVariant variant,
        JsonContractDiscriminator? discriminator,
        JsonContractGenerationSettings settings)
    {
        JsonObject schema = variant.Value is null
            ? new JsonObject()
            : Create(variant.Value, settings);

        WriteAnnotations(schema, variant.Annotations);
        AddRequiredProperties(schema, variant.RequiredProperties);

        if (discriminator is not null && variant.DiscriminatorValue.HasValue)
        {
            AddDiscriminatorConstraint(
                schema,
                discriminator.PropertyName,
                variant.DiscriminatorValue.Value,
                requireProperty: true);
        }

        return schema;
    }

    private static void AddDiscriminatorConstraint (
        JsonObject schema,
        string propertyName,
        JsonElement value,
        bool requireProperty)
    {
        JsonObject properties;
        if (schema["properties"] is JsonObject existingProperties)
        {
            properties = existingProperties;
        }
        else
        {
            properties = new JsonObject();
            schema["properties"] = properties;
        }

        var constantSchema = new JsonObject
        {
            ["const"] = ToJsonNode(value),
        };

        if (properties[propertyName] is JsonNode existingProperty)
        {
            properties[propertyName] = new JsonObject
            {
                ["allOf"] = new JsonArray
                {
                    existingProperty.DeepClone(),
                    constantSchema,
                },
            };
        }
        else
        {
            properties[propertyName] = constantSchema;
        }

        if (requireProperty)
        {
            AddRequiredProperties(schema, new[] { propertyName });
        }
    }

    private static JsonObject CreateDiscriminatorProperties (
        string propertyName,
        JsonElement value)
    {
        return new JsonObject
        {
            [propertyName] = new JsonObject
            {
                ["const"] = ToJsonNode(value),
            },
        };
    }

    private static void WriteObjectClosure (
        JsonObject schema,
        JsonObjectClosure objectClosure)
    {
        switch (objectClosure)
        {
            case JsonObjectClosure.AllowAdditionalProperties:
                schema.Add("additionalProperties", true);
                break;

            case JsonObjectClosure.DisallowAdditionalProperties:
                schema.Add("additionalProperties", false);
                break;

            case JsonObjectClosure.DisallowUnevaluatedProperties:
                schema.Add("unevaluatedProperties", false);
                break;

            default:
                throw new InvalidOperationException(
                    $"Object closure policy '{Vocabulary.GetText(objectClosure)}' cannot be projected to JSON Schema.");
        }
    }

    private static void WriteType (
        JsonObject schema,
        string type,
        bool isNullable)
    {
        if (!isNullable || string.Equals(
                type,
                Vocabulary.GetText(JsonContractScalarKind.Null),
                StringComparison.Ordinal))
        {
            schema.Add("type", type);
            return;
        }

        schema.Add(
            "type",
            new JsonArray
            {
                type,
                Vocabulary.GetText(JsonContractScalarKind.Null),
            });
    }

    private static void WriteAnnotations (
        JsonObject schema,
        JsonContractAnnotations annotations)
    {
        if (annotations.Title is not null)
        {
            schema["title"] = annotations.Title;
        }

        if (annotations.Description is not null)
        {
            schema["description"] = annotations.Description;
        }

        if (annotations.Examples.Count != 0)
        {
            var examples = new JsonArray();
            foreach (JsonElement example in annotations.Examples)
            {
                examples.Add(ToJsonNode(example));
            }

            schema["examples"] = examples;
        }
    }

    private static void WriteRequiredProperties (
        JsonObject schema,
        IEnumerable<string> propertyNames)
    {
        var required = new JsonArray();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (string propertyName in propertyNames)
        {
            if (seen.Add(propertyName))
            {
                required.Add(propertyName);
            }
        }

        if (required.Count != 0)
        {
            schema.Add("required", required);
        }
    }

    private static void AddRequiredProperties (
        JsonObject schema,
        IEnumerable<string> propertyNames)
    {
        JsonArray required;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        if (schema["required"] is JsonArray existingRequired)
        {
            required = existingRequired;
            foreach (JsonNode? item in required)
            {
                if (item is JsonValue value
                    && value.TryGetValue(out string? propertyName)
                    && propertyName is not null)
                {
                    seen.Add(propertyName);
                }
            }
        }
        else
        {
            required = new JsonArray();
        }

        foreach (string propertyName in propertyNames)
        {
            if (seen.Add(propertyName))
            {
                required.Add(propertyName);
            }
        }

        if (required.Count != 0 && schema["required"] is null)
        {
            schema["required"] = required;
        }
    }

    private static JsonObject CreateNullSchema ()
    {
        return new JsonObject
        {
            ["type"] = Vocabulary.GetText(JsonContractScalarKind.Null),
        };
    }

    private static JsonContractScalarKind RequireScalarKind (
        JsonContractNode node)
    {
        return node.ScalarKind
            ?? throw MissingNodeMember(node, nameof(node.ScalarKind));
    }

    private static InvalidOperationException MissingNodeMember (
        JsonContractNode node,
        string memberName)
    {
        return new InvalidOperationException(
            $"Contract node kind '{Vocabulary.GetText(node.Kind)}' requires '{memberName}'.");
    }

    private static string EncodeJsonPointerSegment (string value)
    {
        return value
            .Replace("~", "~0")
            .Replace("/", "~1");
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
