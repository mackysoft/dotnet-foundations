using MackySoft.JsonSchema.Generation.ContractModel;
using MackySoft.Text.Vocabularies;

namespace MackySoft.JsonSchema.Generation.Extensibility;

/// <summary> Declares the complete product-independent representation selected by a type mapper. </summary>
public sealed class JsonContractTypeMapping
{
    private JsonContractTypeMapping (
        JsonContractTypeMappingKind kind,
        JsonContractScalarKind? scalarKind,
        Type? contractType)
    {
        Kind = kind;
        ScalarKind = scalarKind;
        SurrogateType = contractType;
    }

    /// <summary> Gets the selected representation category. </summary>
    public JsonContractTypeMappingKind Kind { get; }

    /// <summary> Gets the scalar category for a scalar mapping. </summary>
    public JsonContractScalarKind? ScalarKind { get; }

    /// <summary>
    /// Gets the CLR type whose normalized JSON contract supplies a
    /// contract-type mapping, or <see langword="null" /> for another mapping
    /// category.
    /// </summary>
    public Type? SurrogateType { get; }

    /// <summary> Creates a mapping that accepts any JSON value. </summary>
    public static JsonContractTypeMapping Arbitrary ()
    {
        return new JsonContractTypeMapping(
            JsonContractTypeMappingKind.Arbitrary,
            null,
            null);
    }

    /// <summary> Creates a mapping to one declared JSON scalar category. </summary>
    /// <param name="scalarKind"> The declared JSON scalar category. </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="scalarKind" /> is not a declared scalar category.
    /// </exception>
    public static JsonContractTypeMapping Scalar (JsonContractScalarKind scalarKind)
    {
        if (!Vocabulary.IsDefined(scalarKind))
        {
            throw new ArgumentOutOfRangeException(
                nameof(scalarKind),
                scalarKind,
                "The scalar kind is not declared.");
        }

        return new JsonContractTypeMapping(
            JsonContractTypeMappingKind.Scalar,
            scalarKind,
            null);
    }

    /// <summary>
    /// Creates a marker mapping whose finite strings are derived from the
    /// mapped target's <c>MackySoft.Text.Vocabularies</c> declaration.
    /// </summary>
    /// <returns>
    /// A text-vocabulary marker without a repeated type or allowed-value
    /// declaration.
    /// </returns>
    /// <remarks>
    /// The generator obtains the target type and effective converter from the
    /// mapper context, then validates every typed vocabulary entry against its
    /// canonical text.
    /// </remarks>
    public static JsonContractTypeMapping TextVocabulary ()
    {
        return new JsonContractTypeMapping(
            JsonContractTypeMappingKind.TextVocabulary,
            scalarKind: null,
            contractType: null);
    }

    /// <summary>
    /// Creates a mapping whose JSON representation is the normalized contract
    /// of a surrogate CLR type.
    /// </summary>
    /// <param name="contractType">
    /// The CLR type from which serializer structure, annotations, and value
    /// constraints are discovered.
    /// </param>
    /// <remarks>
    /// <para>
    /// The surrogate contract is the baseline representation. Metadata on the
    /// mapped source can add examples, repeat the same title or description,
    /// and narrow value constraints. Generation fails when it declares a
    /// different title or description or widens a surrogate constraint.
    /// </para>
    /// <para>
    /// Null acceptance and source identity remain those of the mapped CLR
    /// source. The calling mapper remains responsible for recognizing a
    /// converter whose wire representation matches the surrogate contract.
    /// </para>
    /// <para>
    /// An enum cannot delegate to a string-valued surrogate. Its finite
    /// strings must be derived from a <see cref="TextVocabulary" /> mapping
    /// on that enum.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"> <paramref name="contractType" /> is <see langword="null" />. </exception>
    public static JsonContractTypeMapping ContractType (Type contractType)
    {
        if (contractType is null)
        {
            throw new ArgumentNullException(nameof(contractType));
        }

        return new JsonContractTypeMapping(
            JsonContractTypeMappingKind.ContractType,
            null,
            contractType);
    }
}
