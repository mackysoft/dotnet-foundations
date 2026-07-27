using MackySoft.Text.Vocabularies;

namespace MackySoft.JsonSchema.Generation.Extensibility;

/// <summary> Identifies a representation declared by a contract type mapper. </summary>
[VocabularyDefinition]
public enum JsonContractTypeMappingKind
{
    /// <summary> An unconstrained JSON value. </summary>
    [VocabularyText("arbitrary")]
    Arbitrary,

    /// <summary> One JSON scalar category. </summary>
    [VocabularyText("scalar")]
    Scalar,

    /// <summary>
    /// Canonical strings derived from the mapped target's declared text
    /// vocabulary and effective converter.
    /// </summary>
    [VocabularyText("textVocabulary")]
    TextVocabulary,

    /// <summary>
    /// The normalized serializer structure, annotations, and constraints of a
    /// surrogate CLR type.
    /// </summary>
    [VocabularyText("contractType")]
    ContractType,
}
