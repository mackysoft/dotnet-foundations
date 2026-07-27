using System.Reflection;
using MackySoft.JsonSchema.Generation.Annotations;
using MackySoft.JsonSchema.Generation.Internal.Metadata.Contracts;

namespace MackySoft.JsonSchema.Generation.Internal.Metadata.Declarations;

internal static class AttributeMetadataDeclarationCollector
{
    internal static void Collect (
        MetadataResolutionTarget target,
        MemberInfo? member,
        MetadataDeclarationSet declarations)
    {
        MemberInfo source = member ?? target.TargetType;
        CollectTitles(target, source, declarations);
        CollectDescriptions(target, source, declarations);
        CollectLengths(target, source, declarations);
        CollectPatterns(target, source, declarations);
        CollectItemCounts(target, source, declarations);
        CollectPropertyCounts(target, source, declarations);
    }

    private static void CollectTitles (
        MetadataResolutionTarget target,
        MemberInfo source,
        MetadataDeclarationSet declarations)
    {
        foreach (TitleAttribute attribute
            in GetAttributes<TitleAttribute>(target, source))
        {
            declarations.AddTitle(
                typeof(TitleAttribute).FullName!,
                attribute.Title);
        }
    }

    private static void CollectDescriptions (
        MetadataResolutionTarget target,
        MemberInfo source,
        MetadataDeclarationSet declarations)
    {
        foreach (DescriptionAttribute attribute
            in GetAttributes<DescriptionAttribute>(target, source))
        {
            declarations.AddDescription(
                typeof(DescriptionAttribute).FullName!,
                attribute.Description);
        }
    }

    private static void CollectLengths (
        MetadataResolutionTarget target,
        MemberInfo source,
        MetadataDeclarationSet declarations)
    {
        foreach (LengthAttribute attribute
            in GetAttributes<LengthAttribute>(target, source))
        {
            string sourceId = typeof(LengthAttribute).FullName!;
            declarations.AddMinimumLength(sourceId, attribute.Minimum);
            declarations.AddMaximumLength(sourceId, attribute.Maximum);
        }
    }

    private static void CollectPatterns (
        MetadataResolutionTarget target,
        MemberInfo source,
        MetadataDeclarationSet declarations)
    {
        foreach (PatternAttribute attribute
            in GetAttributes<PatternAttribute>(target, source))
        {
            declarations.AddPattern(
                typeof(PatternAttribute).FullName!,
                attribute.Pattern);
        }
    }

    private static void CollectItemCounts (
        MetadataResolutionTarget target,
        MemberInfo source,
        MetadataDeclarationSet declarations)
    {
        foreach (ItemCountAttribute attribute
            in GetAttributes<ItemCountAttribute>(target, source))
        {
            string sourceId = typeof(ItemCountAttribute).FullName!;
            declarations.AddMinimumItemCount(sourceId, attribute.Minimum);
            declarations.AddMaximumItemCount(sourceId, attribute.Maximum);
        }
    }

    private static void CollectPropertyCounts (
        MetadataResolutionTarget target,
        MemberInfo source,
        MetadataDeclarationSet declarations)
    {
        foreach (PropertyCountAttribute attribute
            in GetAttributes<PropertyCountAttribute>(target, source))
        {
            string sourceId = typeof(PropertyCountAttribute).FullName!;
            declarations.AddMinimumPropertyCount(
                sourceId,
                attribute.Minimum);
            declarations.AddMaximumPropertyCount(
                sourceId,
                attribute.Maximum);
        }
    }

    private static IReadOnlyList<TAttribute> GetAttributes<TAttribute> (
        MetadataResolutionTarget target,
        MemberInfo source)
        where TAttribute : Attribute
    {
        try
        {
            return Attribute
                .GetCustomAttributes(
                    source,
                    typeof(TAttribute),
                    inherit: true)
                .Cast<TAttribute>()
                .ToArray();
        }
        catch (Exception exception) when (
            IsAttributeMaterializationFailure(exception))
        {
            throw MetadataFailure.Invalid(
                target,
                new[] { typeof(TAttribute).FullName! },
                $"Contract attribute '{typeof(TAttribute).FullName}' could not be materialized.",
                exception);
        }
    }

    private static bool IsAttributeMaterializationFailure (Exception exception)
    {
        return exception is ArgumentException
            or CustomAttributeFormatException
            or TargetInvocationException
            or TypeLoadException;
    }
}
