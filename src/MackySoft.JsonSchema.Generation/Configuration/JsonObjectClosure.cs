using MackySoft.Text.Vocabularies;

namespace MackySoft.JsonSchema.Generation.Configuration;

/// <summary> Defines how generated object schemas treat properties that are not declared by the contract. </summary>
[VocabularyDefinition]
public enum JsonObjectClosure
{
    /// <summary> Permits properties that are not declared by the contract. </summary>
    [VocabularyText("allowAdditionalProperties")]
    AllowAdditionalProperties,

    /// <summary> Rejects properties that are not declared by the contract by using <c>additionalProperties</c>. </summary>
    [VocabularyText("disallowAdditionalProperties")]
    DisallowAdditionalProperties,

    /// <summary> Rejects unevaluated properties by using <c>unevaluatedProperties</c>. </summary>
    [VocabularyText("disallowUnevaluatedProperties")]
    DisallowUnevaluatedProperties,
}
