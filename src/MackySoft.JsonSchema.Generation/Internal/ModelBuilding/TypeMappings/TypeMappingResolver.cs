using System.Text.Json.Serialization.Metadata;
using MackySoft.JsonSchema.Generation.Diagnostics;
using MackySoft.JsonSchema.Generation.Extensibility;

namespace MackySoft.JsonSchema.Generation.Internal.ModelBuilding.TypeMappings;

/// <summary>
/// Selects exactly one explicit type mapper and classifies mapper failures.
/// </summary>
internal sealed class TypeMappingResolver
{
    private readonly string contractId;
    private readonly IReadOnlyList<IJsonContractTypeMapper> typeMappers;

    internal TypeMappingResolver (
        string contractId,
        IReadOnlyList<IJsonContractTypeMapper> typeMappers)
    {
        this.contractId = contractId
            ?? throw new ArgumentNullException(nameof(contractId));
        this.typeMappers = typeMappers
            ?? throw new ArgumentNullException(nameof(typeMappers));
    }

    internal ResolvedTypeMapping? Resolve (
        JsonTypeInfo typeInfo,
        JsonTypeInfo declaringTypeInfo,
        JsonPropertyInfo? propertyInfo,
        string? diagnosticPropertyName)
    {
        Type targetType = typeInfo.Type;
        var context = new JsonContractTypeMapperContext(
            typeInfo,
            declaringTypeInfo,
            propertyInfo);
        var matches = new List<IJsonContractTypeMapper>();
        foreach (IJsonContractTypeMapper mapper in typeMappers)
        {
            bool canMap;
            try
            {
                canMap = mapper.CanMap(context);
            }
            catch (Exception exception)
            {
                throw new JsonContractGenerationException(
                    JsonContractGenerationFailureKind.UnsupportedConverter,
                    $"Type mapper '{mapper.StableId}' failed while inspecting '{targetType.FullName}'.",
                    contractId,
                    targetType,
                    diagnosticPropertyName,
                    sourceIds: new[] { mapper.StableId },
                    innerException: exception);
            }

            if (canMap)
            {
                matches.Add(mapper);
            }
        }

        if (matches.Count > 1)
        {
            throw new JsonContractGenerationException(
                JsonContractGenerationFailureKind.MultipleTypeMappers,
                $"More than one type mapper recognizes '{targetType.FullName}'.",
                contractId,
                targetType,
                diagnosticPropertyName,
                sourceIds: matches.Select(static mapper => mapper.StableId));
        }

        if (matches.Count == 0)
        {
            return null;
        }

        IJsonContractTypeMapper selected = matches[0];
        try
        {
            JsonContractTypeMapping mapping = selected.Map(context)
                ?? throw new InvalidOperationException(
                    "A matching type mapper returned null.");
            return new ResolvedTypeMapping(selected, mapping, context);
        }
        catch (JsonContractGenerationException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new JsonContractGenerationException(
                JsonContractGenerationFailureKind.UnsupportedConverter,
                $"Type mapper '{selected.StableId}' could not map '{targetType.FullName}'.",
                contractId,
                targetType,
                diagnosticPropertyName,
                sourceIds: new[] { selected.StableId },
                innerException: exception);
        }
    }
}
