using System.Text.Json;
using System.Text.Json.Serialization;
using MackySoft.JsonSchema.Generation.Internal.Determinism;

namespace MackySoft.JsonSchema.Generation.Internal.ModelBuilding.SerializerMetadata;

/// <summary>
/// Verifies that a declared finite JSON value is accepted and emitted unchanged
/// by the authoritative serializer contract.
/// </summary>
internal sealed class SerializerFiniteValueValidator
{
    private readonly JsonSerializerOptions serializerOptions;

    internal SerializerFiniteValueValidator (
        JsonSerializerOptions serializerOptions)
    {
        this.serializerOptions = serializerOptions
            ?? throw new ArgumentNullException(nameof(serializerOptions));
    }

    internal static bool Supports (Type targetType)
    {
        return targetType.IsEnum
            || targetType == typeof(byte)
            || targetType == typeof(sbyte)
            || targetType == typeof(short)
            || targetType == typeof(ushort)
            || targetType == typeof(int)
            || targetType == typeof(uint)
            || targetType == typeof(long)
            || targetType == typeof(ulong)
            || targetType == typeof(decimal)
            || targetType == typeof(char)
            || targetType == typeof(Guid)
            || targetType == typeof(DateTime)
            || targetType == typeof(DateTimeOffset)
            || targetType == typeof(TimeSpan)
            || targetType == typeof(Uri)
            || targetType == typeof(Version)
            || targetType == typeof(byte[])
            || string.Equals(
                targetType.FullName,
                "System.DateOnly",
                StringComparison.Ordinal)
            || string.Equals(
                targetType.FullName,
                "System.TimeOnly",
                StringComparison.Ordinal);
    }

    internal bool IsRoundTripStable (
        Type targetType,
        JsonConverter? propertyConverter,
        JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Null)
        {
            return true;
        }

        JsonSerializerOptions effectiveOptions =
            CreateEffectiveOptions(propertyConverter);
        object? deserialized = null;
        try
        {
            deserialized = JsonSerializer.Deserialize(
                value.GetRawText(),
                targetType,
                effectiveOptions);
            if (deserialized is null)
            {
                return false;
            }

            string serialized = JsonSerializer.Serialize(
                deserialized,
                targetType,
                effectiveOptions);
            JsonElement roundTripped = JsonElementUtility.ParseStrict(
                serialized);
            return JsonElementUtility.CompareCanonical(
                value,
                roundTripped) == 0;
        }
        catch (Exception exception)
            when (exception is JsonException
                or NotSupportedException
                or InvalidOperationException
                or FormatException)
        {
            return false;
        }
        finally
        {
            (deserialized as IDisposable)?.Dispose();
        }
    }

    private JsonSerializerOptions CreateEffectiveOptions (
        JsonConverter? propertyConverter)
    {
        if (propertyConverter is null)
        {
            return serializerOptions;
        }

        var effectiveOptions = new JsonSerializerOptions(serializerOptions);
        effectiveOptions.Converters.Insert(0, propertyConverter);
        return effectiveOptions;
    }
}
