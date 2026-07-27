using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using MackySoft.JsonSchema.Generation.Diagnostics;

namespace MackySoft.JsonSchema.Generation.Internal.ModelBuilding.SerializerMetadata;

/// <summary>
/// Resolves the ordered member set and object-acceptance behavior that
/// <see cref="JsonTypeInfo"/> and its owning serializer options expose.
/// </summary>
internal sealed class SerializedObjectContractResolver
{
    private readonly string contractId;
    private readonly JsonSerializerOptions serializerOptions;
    private readonly bool allowsAdditionalProperties;

    internal SerializedObjectContractResolver (
        string contractId,
        JsonSerializerOptions serializerOptions,
        bool allowsAdditionalProperties)
    {
        this.contractId = contractId
            ?? throw new ArgumentNullException(nameof(contractId));
        this.serializerOptions = serializerOptions
            ?? throw new ArgumentNullException(nameof(serializerOptions));
        this.allowsAdditionalProperties = allowsAdditionalProperties;
    }

    internal void ValidateGlobalContract (Type contractType)
    {
        if (serializerOptions.PropertyNameCaseInsensitive)
        {
            throw UnsupportedTypeInfo(
                contractType,
                jsonPropertyName: null,
                "Case-insensitive JSON property matching cannot be represented by a case-sensitive JSON Schema object contract.");
        }
    }

    internal IReadOnlyList<SerializedObjectProperty> ResolveProperties (
        Type declaringType,
        JsonTypeInfo typeInfo)
    {
        var result = new List<SerializedObjectProperty>();
        var propertyNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (JsonPropertyInfo propertyInfo in typeInfo.Properties)
        {
            if (propertyInfo.Get is null && propertyInfo.Set is null)
            {
                continue;
            }

            MemberInfo member = SerializedMemberResolver.Resolve(
                contractId,
                declaringType,
                propertyInfo,
                typeInfo.Options);
            if (!IsSerialized(propertyInfo, member))
            {
                continue;
            }

            if (!propertyNames.Add(propertyInfo.Name))
            {
                throw UnsupportedTypeInfo(
                    declaringType,
                    propertyInfo.Name,
                    "JsonTypeInfo contains duplicate serialized property names.");
            }

            ValidateRequiredWriteContract(propertyInfo, member);
            result.Add(new SerializedObjectProperty(propertyInfo, member));
        }

        return result.AsReadOnly();
    }

    internal void ValidateObjectClosure (
        Type targetType,
        JsonTypeInfo typeInfo,
        bool hasExtensionData)
    {
        if (hasExtensionData)
        {
            return;
        }

        JsonUnmappedMemberHandling effectiveHandling =
            typeInfo.UnmappedMemberHandling
            ?? serializerOptions.UnmappedMemberHandling;
        bool serializerAllowsAdditionalProperties =
            effectiveHandling != JsonUnmappedMemberHandling.Disallow;
        if (serializerAllowsAdditionalProperties == allowsAdditionalProperties)
        {
            return;
        }

        string message = allowsAdditionalProperties
            ? "The generated object contract allows undeclared properties, but System.Text.Json rejects unmapped members."
            : "The generated object contract rejects undeclared properties, but System.Text.Json skips unmapped members.";
        throw UnsupportedTypeInfo(
            targetType,
            jsonPropertyName: null,
            message);
    }

    private bool IsSerialized (
        JsonPropertyInfo propertyInfo,
        MemberInfo member)
    {
        bool ignoresReadOnlyMember = member switch
        {
            PropertyInfo =>
                serializerOptions.IgnoreReadOnlyProperties
                && propertyInfo.Set is null,
            FieldInfo field =>
                serializerOptions.IgnoreReadOnlyFields
                && field.IsInitOnly,
            _ => false,
        };
        if (!ignoresReadOnlyMember
            || IsCollectionLike(propertyInfo.PropertyType)
            || IsExplicitlyIncluded(member)
            || propertyInfo.ShouldSerialize is not null)
        {
            return true;
        }

        return false;
    }

    private void ValidateRequiredWriteContract (
        JsonPropertyInfo propertyInfo,
        MemberInfo member)
    {
        if (!propertyInfo.IsRequired)
        {
            return;
        }

        JsonIgnoreAttribute? ignore = member.GetCustomAttribute<
            JsonIgnoreAttribute>(inherit: true);
        bool canContainNull = !propertyInfo.PropertyType.IsValueType
            || Nullable.GetUnderlyingType(propertyInfo.PropertyType) is not null;
#pragma warning disable SYSLIB0020
        bool legacyNullIgnore =
            serializerOptions.IgnoreNullValues && canContainNull;
#pragma warning restore SYSLIB0020
        bool attributeCanOmit = ignore?.Condition
            is JsonIgnoreCondition.Always
                or JsonIgnoreCondition.WhenWritingDefault
            || (ignore?.Condition == JsonIgnoreCondition.WhenWritingNull
                && canContainNull);
        bool globalCanOmit =
            ignore?.Condition != JsonIgnoreCondition.Never
            && (serializerOptions.DefaultIgnoreCondition
                    == JsonIgnoreCondition.WhenWritingDefault
                || (serializerOptions.DefaultIgnoreCondition
                        == JsonIgnoreCondition.WhenWritingNull
                    && canContainNull)
                || legacyNullIgnore);
        bool canBeOmitted = attributeCanOmit
            || globalCanOmit
            || propertyInfo.ShouldSerialize is not null;
        if (!canBeOmitted)
        {
            return;
        }

        throw UnsupportedTypeInfo(
            Nullable.GetUnderlyingType(propertyInfo.PropertyType)
                ?? propertyInfo.PropertyType,
            propertyInfo.Name,
            "A required JSON property can be omitted by the configured write-ignore contract.");
    }

    private static bool IsCollectionLike (Type type)
    {
        return type != typeof(string)
            && typeof(IEnumerable).IsAssignableFrom(type);
    }

    private static bool IsExplicitlyIncluded (MemberInfo member)
    {
        return member.GetCustomAttribute<JsonIgnoreAttribute>(inherit: true)
            ?.Condition == JsonIgnoreCondition.Never;
    }

    private JsonContractGenerationException UnsupportedTypeInfo (
        Type targetType,
        string? jsonPropertyName,
        string message)
    {
        return new JsonContractGenerationException(
            JsonContractGenerationFailureKind.UnsupportedTypeInfo,
            message,
            contractId,
            targetType,
            jsonPropertyName);
    }
}
