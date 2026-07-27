using MackySoft.Text.Vocabularies;

namespace MackySoft.JsonSchema.Generation.Configuration;

/// <summary> Identifies how reusable and recursive contract nodes are represented by JSON Schema. </summary>
[VocabularyDefinition]
public enum JsonReferenceProjection
{
    /// <summary> Uses document-local <c>$defs</c> entries and JSON References. </summary>
    [VocabularyText("localDefinitions")]
    LocalDefinitions,
}
