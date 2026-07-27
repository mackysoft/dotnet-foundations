using MackySoft.Text.Vocabularies;

namespace MackySoft.JsonSchema.Generation.Projection;

/// <summary> Identifies whether a projection includes document-level JSON Schema declarations. </summary>
[VocabularyDefinition]
public enum JsonSchemaDocumentKind
{
    /// <summary> Emits a complete document with the Draft 2020-12 dialect declaration. </summary>
    [VocabularyText("complete")]
    Complete,

    /// <summary>
    /// Emits a schema resource root without a dialect declaration or document identifier.
    /// </summary>
    /// <remarks>
    /// Local <c>#/$defs/...</c> references resolve against the emitted fragment.
    /// Direct insertion below a different schema resource root is not supported
    /// unless the consumer establishes an explicit resource boundary.
    /// </remarks>
    [VocabularyText("fragment")]
    Fragment,
}
