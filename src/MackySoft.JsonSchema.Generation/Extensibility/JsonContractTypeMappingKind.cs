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

    /// <summary> A finite set of JSON values sharing a scalar category. </summary>
    [VocabularyText("enum")]
    Enum,

    /// <summary>
    /// The normalized serializer structure, annotations, and constraints of a
    /// surrogate CLR type.
    /// </summary>
    [VocabularyText("contractType")]
    ContractType,
}
