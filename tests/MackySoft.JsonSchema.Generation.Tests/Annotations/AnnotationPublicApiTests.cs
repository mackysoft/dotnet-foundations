namespace MackySoft.JsonSchema.Generation.Tests.Annotations;

public sealed class AnnotationPublicApiTests
{
    [Fact]
    [Trait("Size", "Small")]
    public void ExportedAttributeTypes_ExposeOnlyUnprefixedAnnotationNames ()
    {
        string[] attributeTypeNames = typeof(JsonContractGenerator)
            .Assembly
            .GetExportedTypes()
            .Where(type => typeof(Attribute).IsAssignableFrom(type))
            .Select(type => type.FullName!)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            new[]
            {
                "MackySoft.JsonSchema.Generation.Annotations.AllowNullAttribute",
                "MackySoft.JsonSchema.Generation.Annotations.AnyValueAttribute",
                "MackySoft.JsonSchema.Generation.Annotations.ConstAttribute",
                "MackySoft.JsonSchema.Generation.Annotations.DescriptionAttribute",
                "MackySoft.JsonSchema.Generation.Annotations.DiscriminatorAttribute",
                "MackySoft.JsonSchema.Generation.Annotations.EnumAttribute",
                "MackySoft.JsonSchema.Generation.Annotations.ExampleAttribute",
                "MackySoft.JsonSchema.Generation.Annotations.ItemCountAttribute",
                "MackySoft.JsonSchema.Generation.Annotations.LengthAttribute",
                "MackySoft.JsonSchema.Generation.Annotations.OneOfBranchAttribute",
                "MackySoft.JsonSchema.Generation.Annotations.PatternAttribute",
                "MackySoft.JsonSchema.Generation.Annotations.PropertyCountAttribute",
                "MackySoft.JsonSchema.Generation.Annotations.RangeAttribute",
                "MackySoft.JsonSchema.Generation.Annotations.RequiredAttribute",
                "MackySoft.JsonSchema.Generation.Annotations.TitleAttribute",
            },
            attributeTypeNames);
    }
}
