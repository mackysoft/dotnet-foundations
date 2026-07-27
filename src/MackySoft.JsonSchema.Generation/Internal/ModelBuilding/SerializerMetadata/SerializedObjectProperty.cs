using System.Reflection;
using System.Text.Json.Serialization.Metadata;

namespace MackySoft.JsonSchema.Generation.Internal.ModelBuilding.SerializerMetadata;

/// <summary>
/// Couples one authoritative JSON property with the CLR declaration used only
/// for annotations and nullable metadata.
/// </summary>
internal sealed class SerializedObjectProperty
{
    internal SerializedObjectProperty (
        JsonPropertyInfo propertyInfo,
        MemberInfo member)
    {
        PropertyInfo = propertyInfo
            ?? throw new ArgumentNullException(nameof(propertyInfo));
        Member = member
            ?? throw new ArgumentNullException(nameof(member));
    }

    internal JsonPropertyInfo PropertyInfo { get; }

    internal MemberInfo Member { get; }
}
