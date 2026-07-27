using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using MackySoft.JsonSchema.Generation.ContractModel;
using MackySoft.JsonSchema.Generation.Internal.Common;
using MackySoft.Text.Vocabularies;

namespace MackySoft.JsonSchema.Generation.Extensibility;

/// <summary> Declares the complete product-independent representation selected by a type mapper. </summary>
public sealed class JsonContractTypeMapping
{
    private static readonly MethodInfo GetVocabularyTextsMethod =
        typeof(Vocabulary)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(
                static method =>
                    method.Name == nameof(Vocabulary.GetTexts)
                    && method.IsGenericMethodDefinition
                    && method.GetParameters().Length == 0);

    private JsonContractTypeMapping (
        JsonContractTypeMappingKind kind,
        JsonContractScalarKind? scalarKind,
        IEnumerable<JsonElement> allowedValues,
        Type? contractType)
    {
        Kind = kind;
        ScalarKind = scalarKind;
        AllowedValues = JsonContractCollections.CloneJsonElements(
            allowedValues,
            nameof(allowedValues));
        SurrogateType = contractType;
    }

    /// <summary> Gets the selected representation category. </summary>
    public JsonContractTypeMappingKind Kind { get; }

    /// <summary> Gets the scalar category for scalar and enum mappings. </summary>
    public JsonContractScalarKind? ScalarKind { get; }

    /// <summary> Gets the finite values for an enum mapping in declared order. </summary>
    public IReadOnlyList<JsonElement> AllowedValues { get; }

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
            Array.Empty<JsonElement>(),
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
            Array.Empty<JsonElement>(),
            null);
    }

    /// <summary> Creates a mapping to a finite set of JSON values sharing one scalar category. </summary>
    /// <param name="scalarKind"> The JSON scalar category shared by every allowed value. </param>
    /// <param name="allowedValues"> One or more independently copied JSON values. </param>
    /// <exception cref="ArgumentNullException"> <paramref name="allowedValues" /> is <see langword="null" />. </exception>
    /// <exception cref="ArgumentException"> <paramref name="allowedValues" /> is empty. </exception>
    public static JsonContractTypeMapping Enum (
        JsonContractScalarKind scalarKind,
        params JsonElement[] allowedValues)
    {
        if (allowedValues is null)
        {
            throw new ArgumentNullException(nameof(allowedValues));
        }

        if (allowedValues.Length == 0)
        {
            throw new ArgumentException(
                "At least one allowed JSON value must be supplied.",
                nameof(allowedValues));
        }

        return new JsonContractTypeMapping(
            JsonContractTypeMappingKind.Enum,
            scalarKind,
            allowedValues,
            null);
    }

    /// <summary>
    /// Creates a finite string mapping from the canonical texts declared by a
    /// <c>MackySoft.Text.Vocabularies</c> enum.
    /// </summary>
    /// <param name="vocabularyType"> The runtime enum type that owns the finite text vocabulary. </param>
    /// <returns> A finite string mapping ordered by vocabulary declaration. </returns>
    /// <remarks>
    /// The calling mapper remains responsible for recognizing the custom
    /// converter that reads and writes these canonical texts.
    /// </remarks>
    /// <exception cref="ArgumentNullException"> <paramref name="vocabularyType" /> is <see langword="null" />. </exception>
    /// <exception cref="ArgumentException"> <paramref name="vocabularyType" /> is not a declared text vocabulary. </exception>
    /// <exception cref="InvalidOperationException"> The vocabulary declaration is invalid. </exception>
    public static JsonContractTypeMapping TextVocabulary (
        Type vocabularyType)
    {
        if (vocabularyType is null)
        {
            throw new ArgumentNullException(nameof(vocabularyType));
        }

        if (!Vocabulary.IsVocabulary(vocabularyType))
        {
            throw new ArgumentException(
                $"Type '{vocabularyType.FullName}' does not declare a text vocabulary.",
                nameof(vocabularyType));
        }

        try
        {
            object? textsObject = GetVocabularyTextsMethod
                .MakeGenericMethod(vocabularyType)
                .Invoke(null, null);
            if (textsObject is not IEnumerable<string> texts)
            {
                throw new InvalidOperationException(
                    "Vocabulary enumeration did not return canonical texts.");
            }

            return new JsonContractTypeMapping(
                JsonContractTypeMappingKind.Enum,
                JsonContractScalarKind.String,
                texts.Select(
                    static text =>
                        JsonSerializer.SerializeToElement(text)),
                null);
        }
        catch (TargetInvocationException exception)
            when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
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
            Array.Empty<JsonElement>(),
            contractType);
    }
}
