using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using MackySoft.JsonSchema.Generation.Diagnostics;
using MackySoft.JsonSchema.Generation.Internal.ModelBuilding.TypeSystem;

namespace MackySoft.JsonSchema.Generation.Internal.ModelBuilding.TypeMappings;

/// <summary>
/// Preserves serializer authority by limiting type mappings to converter
/// contracts that the provider cannot interpret itself.
/// </summary>
internal static class TypeMappingAuthorityValidator
{
    internal static void Validate (
        string contractId,
        Type targetType,
        JsonTypeInfo typeInfo,
        JsonConverter? propertyConverter,
        string? jsonPropertyName,
        ResolvedTypeMapping? resolvedMapping)
    {
        if (resolvedMapping is null)
        {
            return;
        }

        JsonConverter effectiveConverter =
            propertyConverter ?? typeInfo.Converter;
        if (!BuiltInScalarContractResolver.IsSystemTextJsonConverter(
            effectiveConverter))
        {
            return;
        }

        bool hasBuiltInInterpretation =
            targetType.IsEnum
            || BuiltInScalarContractResolver.IsSupportedScalarType(targetType)
            || IsArbitraryJsonType(targetType)
            || (typeInfo.Kind != JsonTypeInfoKind.None
                && !BuiltInScalarContractResolver.RequiresExplicitTypeMapping(
                    targetType));
        if (!hasBuiltInInterpretation)
        {
            return;
        }

        throw new JsonContractGenerationException(
            JsonContractGenerationFailureKind.UnsupportedConverter,
            "A type mapper cannot replace an authoritative built-in System.Text.Json representation. Type mappers may interpret explicitly recognized custom converter contracts.",
            contractId,
            targetType,
            jsonPropertyName,
            sourceIds: new[] { resolvedMapping.Mapper.StableId });
    }

    private static bool IsArbitraryJsonType (Type type)
    {
        return type == typeof(object)
            || type == typeof(JsonElement)
            || type == typeof(JsonDocument)
            || typeof(JsonNode).IsAssignableFrom(type);
    }
}
