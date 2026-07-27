using System.Reflection;
using System.Text.Json.Serialization.Metadata;
using MackySoft.JsonSchema.Generation.Internal.Metadata.Contracts;
using MackySoft.JsonSchema.Generation.Metadata;

namespace MackySoft.JsonSchema.Generation.Internal.Metadata.Declarations;

internal sealed class MetadataExtensionCollectionRequest
{
    internal MetadataExtensionCollectionRequest (
        MetadataResolutionTarget target,
        JsonTypeInfo valueTypeInfo,
        JsonTypeInfo declaringTypeInfo,
        JsonPropertyInfo? propertyInfo,
        MemberInfo attributeSource)
    {
        Target = target;
        ValueTypeInfo = valueTypeInfo
            ?? throw new ArgumentNullException(nameof(valueTypeInfo));
        DeclaringTypeInfo = declaringTypeInfo
            ?? throw new ArgumentNullException(nameof(declaringTypeInfo));
        PropertyInfo = propertyInfo;
        AttributeSource = attributeSource
            ?? throw new ArgumentNullException(nameof(attributeSource));
    }

    internal MetadataResolutionTarget Target { get; }

    internal JsonTypeInfo ValueTypeInfo { get; }

    internal JsonTypeInfo DeclaringTypeInfo { get; }

    internal JsonPropertyInfo? PropertyInfo { get; }

    internal MemberInfo AttributeSource { get; }

    internal JsonContractMetadataContext<TValue> CreateContext<TValue> ()
    {
        if (ValueTypeInfo is not JsonTypeInfo<TValue> typedTypeInfo)
        {
            throw MetadataFailure.Invalid(
                Target,
                sourceIds: Array.Empty<string>(),
                $"System.Text.Json type information for '{typeof(TValue).FullName}' is not strongly typed.");
        }

        return new JsonContractMetadataContext<TValue>(
            typedTypeInfo,
            DeclaringTypeInfo,
            PropertyInfo);
    }
}
