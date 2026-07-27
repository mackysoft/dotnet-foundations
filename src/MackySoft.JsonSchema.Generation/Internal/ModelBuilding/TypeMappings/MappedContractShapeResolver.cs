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

            case JsonContractTypeMappingKind.Enum:
                return ResolveEnum(
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

    private ContractNodeShape ResolveEnum (
        Type targetType,
        ResolvedTypeMapping resolvedMapping,
        string? jsonPropertyName)
    {
        JsonContractTypeMapping mapping = resolvedMapping.Mapping;
        if (!mapping.ScalarKind.HasValue
            || mapping.AllowedValues.Count == 0
            || mapping.AllowedValues.Any(
                value => !MatchesScalarKind(
                    value,
                    mapping.ScalarKind.Value)))
        {
            throw InvalidTypeMapping(
                targetType,
                jsonPropertyName,
                resolvedMapping.Mapper,
                "An enum type mapping must declare one scalar kind and one or more matching values.");
        }

        if (targetType.IsEnum
            && mapping.ScalarKind.Value == JsonContractScalarKind.String)
        {
            EnsureCanonicalVocabularyValues(
                targetType,
                resolvedMapping,
                jsonPropertyName);
        }

        JsonElement[] orderedValues = mapping.AllowedValues.ToArray();
        Array.Sort(orderedValues, JsonElementUtility.CompareCanonical);
        return new ContractNodeShape(
            JsonContractNodeKind.Enum,
            mapping.ScalarKind.Value,
            allowedValues: orderedValues
                .Distinct(JsonElementCanonicalEqualityComparer.Instance)
                .ToArray());
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
                && !VocabularyContractReader.IsVocabulary(targetType)
                && surrogate.Kind == JsonContractNodeKind.Scalar
                && surrogate.ScalarKind == JsonContractScalarKind.String)
            {
                throw InvalidTypeMapping(
                    targetType,
                    jsonPropertyName,
                    resolvedMapping.Mapper,
                    "A closed enum-to-string contract must be declared through MackySoft.Text.Vocabularies.");
            }

            return ShapeFromNode(surrogate);
        }
        finally
        {
            activeMappedTypes.Remove(targetType);
        }
    }

    private void EnsureCanonicalVocabularyValues (
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
                "A closed enum-to-string contract must be declared through MackySoft.Text.Vocabularies.");
        }

        IReadOnlyList<JsonElement> canonicalValues =
            JsonContractTypeMapping
                .TextVocabulary(targetType)
                .AllowedValues;
        if (!CanonicalSetsEqual(
            canonicalValues,
            resolvedMapping.Mapping.AllowedValues))
        {
            throw InvalidTypeMapping(
                targetType,
                jsonPropertyName,
                resolvedMapping.Mapper,
                "The mapped enum values differ from the canonical MackySoft.Text.Vocabularies texts.");
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

    private static bool MatchesScalarKind (
        JsonElement value,
        JsonContractScalarKind scalarKind)
    {
        return scalarKind switch
        {
            JsonContractScalarKind.Null =>
                value.ValueKind == JsonValueKind.Null,
            JsonContractScalarKind.Boolean =>
                value.ValueKind is JsonValueKind.True or JsonValueKind.False,
            JsonContractScalarKind.Integer =>
                value.ValueKind == JsonValueKind.Number
                && value.GetRawText().IndexOf('.') < 0
                && value.GetRawText().IndexOf('e') < 0
                && value.GetRawText().IndexOf('E') < 0,
            JsonContractScalarKind.Number =>
                value.ValueKind == JsonValueKind.Number,
            JsonContractScalarKind.String =>
                value.ValueKind == JsonValueKind.String,
            _ => false,
        };
    }

    private static bool CanonicalSetsEqual (
        IReadOnlyList<JsonElement> left,
        IReadOnlyList<JsonElement> right)
    {
        JsonElement[] leftValues = left.ToArray();
        JsonElement[] rightValues = right.ToArray();
        Array.Sort(leftValues, JsonElementUtility.CompareCanonical);
        Array.Sort(rightValues, JsonElementUtility.CompareCanonical);
        if (leftValues.Length != rightValues.Length)
        {
            return false;
        }

        for (int index = 0; index < leftValues.Length; index++)
        {
            if (JsonElementUtility.CompareCanonical(
                leftValues[index],
                rightValues[index]) != 0)
            {
                return false;
            }
        }

        return true;
    }

    private JsonContractGenerationException InvalidTypeMapping (
        Type targetType,
        string? jsonPropertyName,
        IJsonContractTypeMapper mapper,
        string message)
    {
        return new JsonContractGenerationException(
            JsonContractGenerationFailureKind.UnsupportedConverter,
            message,
            contractId,
            targetType,
            jsonPropertyName,
            sourceIds: new[] { mapper.StableId });
    }
}
