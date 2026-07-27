using MackySoft.Text.Vocabularies;

namespace MackySoft.JsonSchema.Generation.ContractModel;

/// <summary> Identifies the structural role of a node in a JSON contract model. </summary>
[VocabularyDefinition]
public enum JsonContractNodeKind
{
    /// <summary> A JSON value whose shape is not constrained. </summary>
    [VocabularyText("arbitrary")]
    Arbitrary,

    /// <summary> A scalar JSON value. </summary>
    [VocabularyText("scalar")]
    Scalar,

    /// <summary> An array with a shared item contract. </summary>
    [VocabularyText("array")]
    Array,

    /// <summary> An object with declared properties. </summary>
    [VocabularyText("object")]
    Object,

    /// <summary> An object whose property values share a contract. </summary>
    [VocabularyText("dictionary")]
    Dictionary,

    /// <summary> A value selected from a finite set. </summary>
    [VocabularyText("enum")]
    Enum,

    /// <summary> A value equal to one constant. </summary>
    [VocabularyText("const")]
    Const,

    /// <summary> A reference to a reusable contract definition. </summary>
    [VocabularyText("reference")]
    Reference,

    /// <summary> A value matching exactly one declared variant. </summary>
    [VocabularyText("oneOf")]
    OneOf,
}
