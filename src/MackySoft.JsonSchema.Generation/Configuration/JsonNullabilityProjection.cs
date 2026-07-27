using MackySoft.Text.Vocabularies;

namespace MackySoft.JsonSchema.Generation.Configuration;

/// <summary> Identifies the JSON Schema projection used for values that allow JSON <see langword="null" />. </summary>
[VocabularyDefinition]
public enum JsonNullabilityProjection
{
    /// <summary> Uses a JSON Schema type union or an equivalent union branch. </summary>
    [VocabularyText("typeUnion")]
    TypeUnion,
}
