using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using MackySoft.JsonSchema.Generation.ContractModel;
using MackySoft.JsonSchema.Generation.Diagnostics;
using MackySoft.JsonSchema.Generation.Internal.ModelBuilding.Shapes;

namespace MackySoft.JsonSchema.Generation.Internal.ModelBuilding.TypeSystem;

/// <summary>
/// Classifies CLR scalar contracts whose wire representations are owned by
/// System.Text.Json and rejects serializer settings that would change them.
/// </summary>
internal sealed class BuiltInScalarContractResolver
{
    private const string BmpScalarPattern =
        "^[\\u0000-\\uD7FF\\uE000-\\uFFFF]$";

    private const string GuidPattern =
        "^[0-9A-Fa-f]{8}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{12}$";

    private static readonly Assembly SystemTextJsonAssembly =
        typeof(JsonSerializer).Assembly;

    private readonly string contractId;
    private readonly JsonSerializerOptions serializerOptions;

    internal BuiltInScalarContractResolver (
        string contractId,
        JsonSerializerOptions serializerOptions)
    {
        this.contractId = contractId
            ?? throw new ArgumentNullException(nameof(contractId));
        this.serializerOptions = serializerOptions
            ?? throw new ArgumentNullException(nameof(serializerOptions));
    }

    internal bool TryResolve (
        Type targetType,
        JsonTypeInfo typeInfo,
        JsonConverter? propertyConverter,
        JsonNumberHandling? propertyNumberHandling,
        string? jsonPropertyName,
        out ContractNodeShape? shape)
    {
        if (!TryClassify(
            targetType,
            out JsonContractScalarKind scalarKind,
            out string? format))
        {
            shape = null;
            return false;
        }

        EnsureDeterministicRepresentation(
            targetType,
            typeInfo,
            propertyConverter,
            propertyNumberHandling,
            jsonPropertyName,
            scalarKind);
        TryGetNumericBounds(
            targetType,
            out JsonElement? minimum,
            out JsonElement? maximum);
        int? fixedLength = targetType == typeof(char)
            ? 1
            : targetType == typeof(Guid)
                ? 36
                : null;
        string? pattern = targetType == typeof(char)
            ? BmpScalarPattern
            : targetType == typeof(Guid)
                ? GuidPattern
                : null;
        shape = new ContractNodeShape(
            JsonContractNodeKind.Scalar,
            scalarKind,
            format: format,
            minimum: minimum,
            maximum: maximum,
            minimumLength: fixedLength,
            maximumLength: fixedLength,
            pattern: pattern);
        return true;
    }

    internal ContractNodeShape ResolveNumericEnum (
        Type enumType,
        JsonTypeInfo typeInfo,
        JsonConverter? propertyConverter,
        JsonNumberHandling? propertyNumberHandling,
        string? jsonPropertyName)
    {
        EnsureDeterministicRepresentation(
            enumType,
            typeInfo,
            propertyConverter,
            propertyNumberHandling,
            jsonPropertyName,
            JsonContractScalarKind.Integer);
        VocabularyContractReader.EnsureNumericRepresentation(
            contractId,
            enumType,
            serializerOptions,
            propertyConverter,
            jsonPropertyName);

        Type underlyingType = Enum.GetUnderlyingType(enumType);
        if (!TryGetNumericBounds(
                underlyingType,
                out JsonElement? minimum,
                out JsonElement? maximum))
        {
            throw new InvalidOperationException(
                $"Enum '{enumType.FullName}' has an unsupported underlying type.");
        }

        return new ContractNodeShape(
            JsonContractNodeKind.Scalar,
            JsonContractScalarKind.Integer,
            minimum: minimum,
            maximum: maximum);
    }

    internal static bool IsSystemTextJsonConverter (JsonConverter converter)
    {
        return converter.GetType().Assembly == SystemTextJsonAssembly;
    }

    internal static bool IsSupportedScalarType (Type type)
    {
        return TryClassify(
            type,
            out _,
            out _);
    }

    internal static bool RequiresExplicitTypeMapping (Type type)
    {
        return type == typeof(DateTime)
            || type == typeof(DateTimeOffset)
            || type == typeof(TimeSpan)
            || type == typeof(Uri)
            || type == typeof(Version)
            || type == typeof(byte[])
            || string.Equals(
                type.FullName,
                "System.DateOnly",
                StringComparison.Ordinal)
            || string.Equals(
                type.FullName,
                "System.TimeOnly",
                StringComparison.Ordinal);
    }

    private static bool TryClassify (
        Type type,
        out JsonContractScalarKind scalarKind,
        out string? format)
    {
        format = null;
        if (type == typeof(string) || type == typeof(char))
        {
            scalarKind = JsonContractScalarKind.String;
            return true;
        }

        if (type == typeof(bool))
        {
            scalarKind = JsonContractScalarKind.Boolean;
            return true;
        }

        if (type == typeof(byte)
            || type == typeof(sbyte)
            || type == typeof(short)
            || type == typeof(ushort)
            || type == typeof(int)
            || type == typeof(uint)
            || type == typeof(long)
            || type == typeof(ulong))
        {
            scalarKind = JsonContractScalarKind.Integer;
            return true;
        }

        if (type == typeof(float)
            || type == typeof(double)
            || type == typeof(decimal))
        {
            scalarKind = JsonContractScalarKind.Number;
            return true;
        }

        if (type == typeof(Guid))
        {
            scalarKind = JsonContractScalarKind.String;
            format = "uuid";
            return true;
        }

        scalarKind = default;
        return false;
    }

    private static bool TryGetNumericBounds (
        Type type,
        out JsonElement? minimum,
        out JsonElement? maximum)
    {
        if (type == typeof(byte))
        {
            minimum = JsonSerializer.SerializeToElement(byte.MinValue);
            maximum = JsonSerializer.SerializeToElement(byte.MaxValue);
            return true;
        }

        if (type == typeof(sbyte))
        {
            minimum = JsonSerializer.SerializeToElement(sbyte.MinValue);
            maximum = JsonSerializer.SerializeToElement(sbyte.MaxValue);
            return true;
        }

        if (type == typeof(short))
        {
            minimum = JsonSerializer.SerializeToElement(short.MinValue);
            maximum = JsonSerializer.SerializeToElement(short.MaxValue);
            return true;
        }

        if (type == typeof(ushort))
        {
            minimum = JsonSerializer.SerializeToElement(ushort.MinValue);
            maximum = JsonSerializer.SerializeToElement(ushort.MaxValue);
            return true;
        }

        if (type == typeof(int))
        {
            minimum = JsonSerializer.SerializeToElement(int.MinValue);
            maximum = JsonSerializer.SerializeToElement(int.MaxValue);
            return true;
        }

        if (type == typeof(uint))
        {
            minimum = JsonSerializer.SerializeToElement(uint.MinValue);
            maximum = JsonSerializer.SerializeToElement(uint.MaxValue);
            return true;
        }

        if (type == typeof(long))
        {
            minimum = JsonSerializer.SerializeToElement(long.MinValue);
            maximum = JsonSerializer.SerializeToElement(long.MaxValue);
            return true;
        }

        if (type == typeof(ulong))
        {
            minimum = JsonSerializer.SerializeToElement(ulong.MinValue);
            maximum = JsonSerializer.SerializeToElement(ulong.MaxValue);
            return true;
        }

        if (type == typeof(decimal))
        {
            minimum = JsonSerializer.SerializeToElement(decimal.MinValue);
            maximum = JsonSerializer.SerializeToElement(decimal.MaxValue);
            return true;
        }

        if (type == typeof(float))
        {
            minimum = JsonSerializer.SerializeToElement(float.MinValue);
            maximum = JsonSerializer.SerializeToElement(float.MaxValue);
            return true;
        }

        if (type == typeof(double))
        {
            minimum = JsonSerializer.SerializeToElement(double.MinValue);
            maximum = JsonSerializer.SerializeToElement(double.MaxValue);
            return true;
        }

        minimum = null;
        maximum = null;
        return false;
    }

    private void EnsureDeterministicRepresentation (
        Type targetType,
        JsonTypeInfo typeInfo,
        JsonConverter? propertyConverter,
        JsonNumberHandling? propertyNumberHandling,
        string? jsonPropertyName,
        JsonContractScalarKind scalarKind)
    {
        if (!IsSystemTextJsonConverter(typeInfo.Converter)
            || (propertyConverter is not null
                && !IsSystemTextJsonConverter(propertyConverter)))
        {
            throw UnsupportedConverter(
                targetType,
                jsonPropertyName,
                "A built-in CLR scalar uses a converter whose JSON representation is not declared.");
        }

        JsonNumberHandling effectiveNumberHandling =
            propertyNumberHandling
            ?? typeInfo.NumberHandling
            ?? serializerOptions.NumberHandling;
        bool nonStrictNumberHandling =
            scalarKind is JsonContractScalarKind.Integer
                or JsonContractScalarKind.Number
            && effectiveNumberHandling != JsonNumberHandling.Strict;
        if (nonStrictNumberHandling)
        {
            throw UnsupportedConverter(
                targetType,
                jsonPropertyName,
                "Non-strict JSON number handling requires an explicit type mapper.");
        }
    }

    private JsonContractGenerationException UnsupportedConverter (
        Type targetType,
        string? jsonPropertyName,
        string message)
    {
        return new JsonContractGenerationException(
            JsonContractGenerationFailureKind.UnsupportedConverter,
            message,
            contractId,
            targetType,
            jsonPropertyName);
    }
}
