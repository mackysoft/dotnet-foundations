using MackySoft.Text.Vocabularies;

namespace MackySoft.JsonSchema.Generation.Metadata;

/// <summary> Identifies one product-independent contract metadata declaration. </summary>
[VocabularyDefinition]
public enum JsonContractMetadataKind
{
    /// <summary> A human-readable title. </summary>
    [VocabularyText("title")]
    Title,

    /// <summary> Explanatory text. </summary>
    [VocabularyText("description")]
    Description,

    /// <summary> One JSON example. </summary>
    [VocabularyText("example")]
    Example,

    /// <summary> Required property presence. </summary>
    [VocabularyText("required")]
    Required,

    /// <summary> Explicit JSON null acceptance. </summary>
    [VocabularyText("allowNull")]
    AllowNull,

    /// <summary> One required constant value. </summary>
    [VocabularyText("const")]
    Const,

    /// <summary> One value in a finite allowed set. </summary>
    [VocabularyText("enumValue")]
    EnumValue,

    /// <summary> An inclusive numeric lower bound. </summary>
    [VocabularyText("minimum")]
    Minimum,

    /// <summary> An exclusive numeric lower bound. </summary>
    [VocabularyText("exclusiveMinimum")]
    ExclusiveMinimum,

    /// <summary> An inclusive numeric upper bound. </summary>
    [VocabularyText("maximum")]
    Maximum,

    /// <summary> An exclusive numeric upper bound. </summary>
    [VocabularyText("exclusiveMaximum")]
    ExclusiveMaximum,

    /// <summary> A minimum string length. </summary>
    [VocabularyText("minimumLength")]
    MinimumLength,

    /// <summary> A maximum string length. </summary>
    [VocabularyText("maximumLength")]
    MaximumLength,

    /// <summary> A minimum array item count. </summary>
    [VocabularyText("minimumItems")]
    MinimumItems,

    /// <summary> A maximum array item count. </summary>
    [VocabularyText("maximumItems")]
    MaximumItems,

    /// <summary> A minimum object property count. </summary>
    [VocabularyText("minimumProperties")]
    MinimumProperties,

    /// <summary> A maximum object property count. </summary>
    [VocabularyText("maximumProperties")]
    MaximumProperties,

    /// <summary> A JSON Schema regular-expression pattern. </summary>
    [VocabularyText("pattern")]
    Pattern,

    /// <summary> A semantic string format annotation. </summary>
    [VocabularyText("format")]
    Format,

    /// <summary> An explicit declaration that any JSON value is accepted. </summary>
    [VocabularyText("arbitrary")]
    Arbitrary,

    /// <summary> One exclusive contract branch and its selection conditions. </summary>
    [VocabularyText("oneOfBranch")]
    OneOfBranch,

    /// <summary> The JSON property that selects a tagged-union branch. </summary>
    [VocabularyText("discriminator")]
    Discriminator,
}
