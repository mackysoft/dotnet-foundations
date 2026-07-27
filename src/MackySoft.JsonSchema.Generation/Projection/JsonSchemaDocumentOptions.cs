using MackySoft.Text.Vocabularies;

namespace MackySoft.JsonSchema.Generation.Projection;

/// <summary> Defines artifact-level values for JSON Schema and type metadata projections. </summary>
public sealed class JsonSchemaDocumentOptions
{
    /// <summary> Initializes document projection options. </summary>
    /// <param name="kind"> Whether to include document-level declarations or emit a schema resource root without them. </param>
    /// <param name="id"> The optional JSON Schema <c>$id</c> for a complete document. </param>
    /// <param name="logicalName"> The optional product-owned logical name reported by type metadata. </param>
    /// <exception cref="ArgumentException">
    /// <paramref name="id" /> or <paramref name="logicalName" /> is empty or contains only whitespace,
    /// <paramref name="id" /> is not a well-formed URI reference, contains a non-empty fragment,
    /// or is provided for a schema fragment.
    /// </exception>
    public JsonSchemaDocumentOptions (
        JsonSchemaDocumentKind kind,
        string? id,
        string? logicalName)
    {
        if (!Vocabulary.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "The document kind is not declared.");
        }

        if (id != null && string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("A schema document identifier cannot be empty or whitespace.", nameof(id));
        }

        if (id != null && !IsValidSchemaIdentifier(id))
        {
            throw new ArgumentException(
                "A schema document identifier must be a well-formed URI reference without a non-empty fragment.",
                nameof(id));
        }

        if (logicalName != null && string.IsNullOrWhiteSpace(logicalName))
        {
            throw new ArgumentException("A schema logical name cannot be empty or whitespace.", nameof(logicalName));
        }

        if (kind == JsonSchemaDocumentKind.Fragment && id != null)
        {
            throw new ArgumentException("A schema fragment cannot declare a document identifier.", nameof(id));
        }

        Kind = kind;
        Id = id;
        LogicalName = logicalName;
    }

    /// <summary> Gets whether the projection is a complete document or a fragment. </summary>
    public JsonSchemaDocumentKind Kind { get; }

    /// <summary> Gets the optional JSON Schema document identifier. </summary>
    public string? Id { get; }

    /// <summary> Gets the optional product-owned logical name included in type metadata. </summary>
    public string? LogicalName { get; }

    private static bool IsValidSchemaIdentifier (string id)
    {
        if (!Uri.IsWellFormedUriString(id, UriKind.RelativeOrAbsolute)
            || !Uri.TryCreate(id, UriKind.RelativeOrAbsolute, out _))
        {
            return false;
        }

        int fragmentMarker = id.IndexOf('#');
        return fragmentMarker < 0 || fragmentMarker == id.Length - 1;
    }
}
