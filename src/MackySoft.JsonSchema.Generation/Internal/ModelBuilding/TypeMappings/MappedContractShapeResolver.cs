using System.Text.Json;
using MackySoft.JsonSchema.Generation.ContractModel;
using MackySoft.JsonSchema.Generation.Diagnostics;
using MackySoft.JsonSchema.Generation.Extensibility;
using MackySoft.JsonSchema.Generation.Internal.Determinism;
using MackySoft.JsonSchema.Generation.Internal.ModelBuilding.Shapes;
using MackySoft.JsonSchema.Generation.Internal.ModelBuilding.TypeSystem;

namespace MackySoft.JsonSchema.Generation.Internal.ModelBuilding.TypeMappings;

/// <summary>
/// Validates explicit mapping declarations and translates them into contract
/// shapes without owning CLR graph traversal.
/// </summary>
internal sealed class MappedContractShapeResolver
{
    private readonly string contractId;
    private readonly HashSet<Type> activeMappedTypes = new();

    internal MappedContractShapeResolver (string contractId)
    {
        this.contractId = contractId
            ?? throw new ArgumentNullException(nameof(contractId));
    }

    internal ContractNodeShape Resolve (
        Type targetType,
        ResolvedTypeMapping resolvedMapping,
        string? jsonPropertyName,
        Func<Type, string?, JsonContractNode> buildSurrogate)
    {
        JsonContractTypeMapping mapping = resolvedMapping.Mapping;
        switch (mapping.Kind)
        {
            case JsonContractTypeMappingKind.Arbitrary:
                return new ContractNodeShape(JsonContractNodeKind.Arbitrary);

            case JsonContractTypeMappingKind.Scalar:
                return ResolveScalar(
                    targetType,
                    resolvedMapping,
                    jsonPropertyName);

            case JsonContractTypeMappingKind.TextVocabulary:
                return ResolveTextVocabulary(
                    targetType,
                    resolvedMapping,
                    jsonPropertyName);

            case JsonContractTypeMappingKind.ContractType:
                return ResolveContractType(
                    targetType,
                    resolvedMapping,
                    jsonPropertyName,
                    buildSurrogate);

            default:
                throw InvalidTypeMapping(
                    targetType,
                    jsonPropertyName,
                    resolvedMapping.Mapper,
                    $"Type mapping kind '{mapping.Kind}' is not declared.");
        }
    }

    private ContractNodeShape ResolveScalar (
        Type targetType,
        ResolvedTypeMapping resolvedMapping,
        string? jsonPropertyName)
    {
        JsonContractScalarKind scalarKind =
            resolvedMapping.Mapping.ScalarKind
            ?? throw InvalidTypeMapping(
                targetType,
                jsonPropertyName,
                resolvedMapping.Mapper,
                "A scalar type mapping must declare a scalar kind.");
        if (targetType.IsEnum
            && scalarKind == JsonContractScalarKind.String)
        {
            throw InvalidTypeMapping(
                targetType,
                jsonPropertyName,
                resolvedMapping.Mapper,
                "A closed enum-to-string contract must use a finite MackySoft.Text.Vocabularies declaration.");
        }

        return new ContractNodeShape(
            JsonContractNodeKind.Scalar,
            scalarKind);
    }

    private ContractNodeShape ResolveTextVocabulary (
        Type targetType,
        ResolvedTypeMapping resolvedMapping,
        string? jsonPropertyName)
    {
        EnsureTextVocabularyTarget(
            targetType,
            resolvedMapping,
            jsonPropertyName);
        IReadOnlyList<string> canonicalTexts =
            ReadTextVocabularyCanonicalTexts(
                targetType,
                resolvedMapping,
                jsonPropertyName);

        return CreateTextVocabularyShape(canonicalTexts);
    }

    private void EnsureTextVocabularyTarget (
        Type targetType,
        ResolvedTypeMapping resolvedMapping,
        string? jsonPropertyName)
    {
        if (!VocabularyContractReader.IsVocabulary(targetType))
        {
            throw InvalidTypeMapping(
                targetType,
                jsonPropertyName,
                resolvedMapping.Mapper,
                "A text-vocabulary mapping requires the mapped target type to declare MackySoft.Text.Vocabularies.");
        }
    }

    private IReadOnlyList<string> ReadTextVocabularyCanonicalTexts (
        Type targetType,
        ResolvedTypeMapping resolvedMapping,
        string? jsonPropertyName)
    {
        try
        {
            return TextVocabularyMappingResolver.ReadCanonicalTexts(
                resolvedMapping.Context);
        }
        catch (Exception exception)
        {
            throw InvalidTypeMapping(
                targetType,
                jsonPropertyName,
                resolvedMapping.Mapper,
                "The effective converter does not implement the target type's exact canonical text vocabulary.",
                exception);
        }
    }

    private static ContractNodeShape CreateTextVocabularyShape (
        IReadOnlyList<string> canonicalTexts)
    {
        JsonElement[] orderedValues = canonicalTexts
            .Select(
                static text =>
                    JsonSerializer.SerializeToElement(text))
            .ToArray();
        Array.Sort(orderedValues, JsonElementUtility.CompareCanonical);
        if (orderedValues.Length == 1)
        {
            return new ContractNodeShape(
                JsonContractNodeKind.Const,
                JsonContractScalarKind.String,
                constant: orderedValues[0]);
        }

        return new ContractNodeShape(
            JsonContractNodeKind.Enum,
            JsonContractScalarKind.String,
            allowedValues: orderedValues);
    }

    private ContractNodeShape ResolveContractType (
        Type targetType,
        ResolvedTypeMapping resolvedMapping,
        string? jsonPropertyName,
        Func<Type, string?, JsonContractNode> buildSurrogate)
    {
        Type surrogateType = resolvedMapping.Mapping.SurrogateType
            ?? throw InvalidTypeMapping(
                targetType,
                jsonPropertyName,
                resolvedMapping.Mapper,
                "A contract-type mapping must declare a surrogate CLR type.");
        if (surrogateType == targetType
            || !activeMappedTypes.Add(targetType))
        {
            throw InvalidTypeMapping(
                targetType,
                jsonPropertyName,
                resolvedMapping.Mapper,
                "A contract-type mapping contains a direct or indirect surrogate cycle.");
        }

        try
        {
            JsonContractNode surrogate = buildSurrogate(
                surrogateType,
                jsonPropertyName);
            if (targetType.IsEnum
                && surrogate.ScalarKind == JsonContractScalarKind.String)
            {
                throw InvalidTypeMapping(
                    targetType,
                    jsonPropertyName,
                    resolvedMapping.Mapper,
                    "A closed enum-to-string contract must use a TextVocabulary mapping derived from the mapped enum's MackySoft.Text.Vocabularies declaration.");
            }

            return ShapeFromNode(surrogate);
        }
        finally
        {
            activeMappedTypes.Remove(targetType);
        }
    }

    private static ContractNodeShape ShapeFromNode (JsonContractNode node)
    {
        return new ContractNodeShape(
            node.Kind,
            scalarKind: node.ScalarKind,
            constant: node.Constant,
            allowedValues: node.AllowedValues,
            referenceId: node.ReferenceId,
            items: node.Items,
            additionalProperties: node.AdditionalProperties,
            properties: node.Properties,
            variants: node.Variants,
            discriminator: node.Discriminator,
            annotations: node.Annotations,
            format: node.Constraints.Format,
            minimum: node.Constraints.Minimum,
            exclusiveMinimum: node.Constraints.ExclusiveMinimum,
            maximum: node.Constraints.Maximum,
            exclusiveMaximum: node.Constraints.ExclusiveMaximum,
            minimumLength: node.Constraints.MinimumLength,
            maximumLength: node.Constraints.MaximumLength,
            minimumItems: node.Constraints.MinimumItems,
            maximumItems: node.Constraints.MaximumItems,
            minimumProperties: node.Constraints.MinimumProperties,
            maximumProperties: node.Constraints.MaximumProperties,
            pattern: node.Constraints.Pattern);
    }

    private JsonContractGenerationException InvalidTypeMapping (
        Type targetType,
        string? jsonPropertyName,
        IJsonContractTypeMapper mapper,
        string message,
        Exception? innerException = null)
    {
        return new JsonContractGenerationException(
            JsonContractGenerationFailureKind.UnsupportedConverter,
            message,
            contractId,
            targetType,
            jsonPropertyName,
            sourceIds: new[] { mapper.StableId },
            innerException: innerException);
    }
}
