using System.Reflection;
using System.Text.Json;
using MackySoft.JsonSchema.Generation.Annotations;
using MackySoft.JsonSchema.Generation.Internal.Metadata.Contracts;
using MackySoft.JsonSchema.Generation.Internal.Metadata.Normalization;
using MackySoft.JsonSchema.Generation.Metadata;
using MackySoft.Text.Vocabularies;

namespace MackySoft.JsonSchema.Generation.Internal.Metadata.Declarations;

internal static class AttributeMetadataDeclarationCollector
{
    internal static void Collect (
        MetadataResolutionTarget target,
        MemberInfo? member,
        MetadataDeclarationSet declarations)
    {
        Attribute[] attributes;
        try
        {
            attributes = member is null
                ? Attribute.GetCustomAttributes(target.TargetType, inherit: true)
                : Attribute.GetCustomAttributes(member, inherit: true);
        }
        catch (Exception exception) when (IsAttributeMaterializationFailure(exception))
        {
            throw MetadataFailure.Invalid(
                target,
                metadataKind: null,
                sourceIds: Array.Empty<string>(),
                "Contract attributes could not be materialized.",
                exception);
        }

        Array.Sort(attributes, ContractAttributeComparer.Instance);
        foreach (Attribute attribute in attributes)
        {
            string? sourceId = attribute.GetType().FullName;
            if (sourceId is null)
            {
                throw MetadataFailure.Invalid(
                    target,
                    metadataKind: null,
                    sourceIds: Array.Empty<string>(),
                    "A contract attribute does not have a deterministic full type name.");
            }

            CollectAttribute(target, attribute, sourceId, declarations);
        }
    }

    private static void CollectAttribute (
        MetadataResolutionTarget target,
        Attribute attribute,
        string sourceId,
        MetadataDeclarationSet declarations)
    {
        switch (attribute)
        {
            case TitleAttribute title:
                declarations.Add(sourceId, JsonContractMetadata.Title(title.Title));
                break;

            case DescriptionAttribute description:
                declarations.Add(
                    sourceId,
                    JsonContractMetadata.Description(description.Description));
                break;

            case ExampleAttribute example:
                declarations.Add(
                    sourceId,
                    JsonContractMetadata.Example(
                        ParseAttributeJson(
                            example.Json,
                            target,
                            JsonContractMetadataKind.Example,
                            sourceId)));
                break;

            case RequiredAttribute:
                declarations.Add(sourceId, JsonContractMetadata.Required());
                break;

            case AllowNullAttribute:
                declarations.Add(sourceId, JsonContractMetadata.AllowNull());
                break;

            case ConstAttribute constant:
                declarations.Add(
                    sourceId,
                    JsonContractMetadata.Const(
                        ParseAttributeJson(
                            constant.Json,
                            target,
                            JsonContractMetadataKind.Const,
                            sourceId)));
                break;

            case EnumAttribute finiteSet:
                foreach (string jsonValue in finiteSet.JsonValues)
                {
                    declarations.Add(
                        sourceId,
                        JsonContractMetadata.EnumValue(
                            ParseAttributeJson(
                                jsonValue,
                                target,
                                JsonContractMetadataKind.EnumValue,
                                sourceId)));
                }
                break;

            case RangeAttribute range:
                CollectRangeAttribute(
                    target,
                    range,
                    sourceId,
                    declarations);
                break;

            case LengthAttribute length:
                declarations.Add(
                    sourceId,
                    JsonContractMetadata.MinimumLength(length.Minimum));
                declarations.Add(
                    sourceId,
                    JsonContractMetadata.MaximumLength(length.Maximum));
                break;

            case PatternAttribute pattern:
                declarations.Add(
                    sourceId,
                    JsonContractMetadata.Pattern(pattern.Pattern));
                break;

            case ItemCountAttribute itemCount:
                declarations.Add(
                    sourceId,
                    JsonContractMetadata.MinimumItems(itemCount.Minimum));
                declarations.Add(
                    sourceId,
                    JsonContractMetadata.MaximumItems(itemCount.Maximum));
                break;

            case PropertyCountAttribute propertyCount:
                declarations.Add(
                    sourceId,
                    JsonContractMetadata.MinimumProperties(propertyCount.Minimum));
                declarations.Add(
                    sourceId,
                    JsonContractMetadata.MaximumProperties(propertyCount.Maximum));
                break;

            case AnyValueAttribute:
                declarations.Add(sourceId, JsonContractMetadata.Arbitrary());
                break;

            case OneOfBranchAttribute branch:
                declarations.Add(
                    new ResolvedContractMetadata.OneOfBranchProvenance(
                        sourceId,
                        OneOfBranchDeclarationNormalizer.Normalize(
                            target,
                            branch,
                            sourceId)));
                break;

            case DiscriminatorAttribute discriminator:
                MetadataResolutionTarget typeTarget = ForTypeMetadata(target);
                MetadataTextContract.Validate(
                    discriminator.PropertyName,
                    typeTarget,
                    JsonContractMetadataKind.Discriminator,
                    sourceId,
                    "The discriminator property name");
                declarations.Add(
                    new ResolvedContractMetadata.DiscriminatorProvenance(
                        sourceId,
                        discriminator.PropertyName));
                break;
        }
    }

    private static void CollectRangeAttribute (
        MetadataResolutionTarget target,
        RangeAttribute range,
        string sourceId,
        MetadataDeclarationSet declarations)
    {
        if (range.MinimumJson is not null)
        {
            JsonContractMetadataKind kind = range.ExclusiveMinimum
                ? JsonContractMetadataKind.ExclusiveMinimum
                : JsonContractMetadataKind.Minimum;
            JsonElement value = ParseAttributeJson(
                range.MinimumJson,
                target,
                kind,
                sourceId);
            declarations.Add(
                new ResolvedContractMetadata.MetadataProvenance(
                    sourceId,
                    range.ExclusiveMinimum
                        ? JsonContractMetadata.ExclusiveMinimum(value)
                        : JsonContractMetadata.Minimum(value)));
        }

        if (range.MaximumJson is not null)
        {
            JsonContractMetadataKind kind = range.ExclusiveMaximum
                ? JsonContractMetadataKind.ExclusiveMaximum
                : JsonContractMetadataKind.Maximum;
            JsonElement value = ParseAttributeJson(
                range.MaximumJson,
                target,
                kind,
                sourceId);
            declarations.Add(
                new ResolvedContractMetadata.MetadataProvenance(
                    sourceId,
                    range.ExclusiveMaximum
                        ? JsonContractMetadata.ExclusiveMaximum(value)
                        : JsonContractMetadata.Maximum(value)));
        }
    }

    private static JsonElement ParseAttributeJson (
        string json,
        MetadataResolutionTarget target,
        JsonContractMetadataKind metadataKind,
        string sourceId)
    {
        try
        {
            return MetadataJsonNormalizer.ParseStrict(json);
        }
        catch (Exception exception) when (
            exception is JsonException or ArgumentException)
        {
            throw MetadataFailure.Invalid(
                target,
                metadataKind,
                new[] { sourceId },
                $"The {Vocabulary.GetText(metadataKind)} attribute value is not strict JSON.",
                exception);
        }
    }

    private static MetadataResolutionTarget ForTypeMetadata (
        MetadataResolutionTarget target)
    {
        return new MetadataResolutionTarget(
            target.ContractId,
            target.TargetType,
            jsonPropertyName: null,
            isMember: false);
    }

    private static bool IsAttributeMaterializationFailure (Exception exception)
    {
        return exception is ArgumentException
            or CustomAttributeFormatException
            or TargetInvocationException
            or TypeLoadException;
    }
}
