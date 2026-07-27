using MackySoft.Text.Vocabularies;

namespace MackySoft.JsonSchema.Generation.ContractModel;

/// <summary> Identifies a JSON scalar value category. </summary>
[VocabularyDefinition]
public enum JsonContractScalarKind
{
    /// <summary> The JSON <see langword="null" /> value. </summary>
    [VocabularyText("null")]
    Null,

    /// <summary> A JSON Boolean value. </summary>
    [VocabularyText("boolean")]
    Boolean,

    /// <summary> A JSON number with no fractional part. </summary>
    [VocabularyText("integer")]
    Integer,

    /// <summary> A JSON number. </summary>
    [VocabularyText("number")]
    Number,

    /// <summary> A JSON string. </summary>
    [VocabularyText("string")]
    String,
}
