using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
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
    private readonly JsonSerializerOptions serializerOptions;
    private readonly IReadOnlyList<IJsonContractTypeMapper> typeMappers;

    internal TypeMappingResolver (
        string contractId,
        JsonSerializerOptions serializerOptions,
        IReadOnlyList<IJsonContractTypeMapper> typeMappers)
    {
        this.contractId = contractId
            ?? throw new ArgumentNullException(nameof(contractId));
        this.serializerOptions = serializerOptions
            ?? throw new ArgumentNullException(nameof(serializerOptions));
        this.typeMappers = typeMappers
            ?? throw new ArgumentNullException(nameof(typeMappers));
    }

    internal ResolvedTypeMapping? Resolve (
        Type targetType,
        JsonTypeInfo typeInfo,
        MemberInfo? member,
        string? jsonPropertyName,
        JsonConverter? propertyConverter)
    {
        var context = new JsonContractTypeMapperContext(
            targetType,
            typeInfo,
            serializerOptions,
            member,
            jsonPropertyName,
            propertyConverter);
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
                    jsonPropertyName,
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
                jsonPropertyName,
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
            return new ResolvedTypeMapping(selected, mapping);
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
                jsonPropertyName,
                sourceIds: new[] { selected.StableId },
                innerException: exception);
        }
    }
}
