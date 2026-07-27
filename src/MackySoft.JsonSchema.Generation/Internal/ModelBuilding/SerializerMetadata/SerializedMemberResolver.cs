using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using MackySoft.JsonSchema.Generation.Diagnostics;
using MackySoft.JsonSchema.Generation.Internal.Determinism;

namespace MackySoft.JsonSchema.Generation.Internal.ModelBuilding.SerializerMetadata;

/// <summary>
/// Associates source-generated property metadata with one CLR declaration solely
/// so annotations and nullable metadata can be read. The serialized name and
/// structure remain authoritative from <see cref="JsonPropertyInfo"/>.
/// </summary>
internal static class SerializedMemberResolver
{
    internal static MemberInfo Resolve (
        string contractId,
        Type declaringType,
        JsonPropertyInfo propertyInfo,
        JsonSerializerOptions serializerOptions)
    {
        if (propertyInfo.AttributeProvider is MemberInfo declaredMember)
        {
            return declaredMember;
        }

        MemberInfo[] candidates = GetCandidateMembers(declaringType)
            .Where(
                member => GetMemberType(member) == propertyInfo.PropertyType)
            .Where(
                member => string.Equals(
                    GetCandidateJsonName(member, serializerOptions),
                    propertyInfo.Name,
                    StringComparison.Ordinal))
            .OrderBy(
                static member => member.Name,
                UnicodeCodePointComparer.Instance)
            .ThenBy(static member => member.MetadataToken)
            .ToArray();
        if (candidates.Length == 1)
        {
            return candidates[0];
        }

        string reason = candidates.Length == 0
            ? "does not map to a CLR property or field"
            : "maps to more than one CLR property or field";
        throw new JsonContractGenerationException(
            JsonContractGenerationFailureKind.UnsupportedTypeInfo,
            $"Source-generated JSON property '{propertyInfo.Name}' {reason}; member annotations and nullability cannot be resolved without guessing.",
            contractId,
            propertyInfo.PropertyType,
            propertyInfo.Name);
    }

    private static IEnumerable<MemberInfo> GetCandidateMembers (
        Type declaringType)
    {
        const BindingFlags Flags = BindingFlags.Instance
            | BindingFlags.Public
            | BindingFlags.NonPublic;

        foreach (PropertyInfo property in declaringType.GetProperties(Flags))
        {
            if (property.GetIndexParameters().Length == 0)
            {
                yield return property;
            }
        }

        foreach (FieldInfo field in declaringType.GetFields(Flags))
        {
            yield return field;
        }
    }

    private static Type? GetMemberType (MemberInfo member)
    {
        return member switch
        {
            PropertyInfo property => property.PropertyType,
            FieldInfo field => field.FieldType,
            _ => null,
        };
    }

    private static string GetCandidateJsonName (
        MemberInfo member,
        JsonSerializerOptions serializerOptions)
    {
        JsonPropertyNameAttribute? explicitName =
            member.GetCustomAttribute<JsonPropertyNameAttribute>(
                inherit: true);
        return explicitName?.Name
            ?? serializerOptions.PropertyNamingPolicy?.ConvertName(member.Name)
            ?? member.Name;
    }
}
