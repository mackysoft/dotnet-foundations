using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using MackySoft.JsonSchema.Generation.Internal.Metadata.Contracts;
using MackySoft.JsonSchema.Generation.Metadata;

namespace MackySoft.JsonSchema.Generation.Internal.Metadata.Declarations;

internal sealed class MetadataDeclarationSink
    : IJsonContractMetadataDeclarationSink
{
    private readonly MetadataResolutionTarget target;
    private readonly string sourceId;
    private readonly MetadataDeclarationSet declarations;

    internal MetadataDeclarationSink (
        MetadataResolutionTarget target,
        string sourceId,
        MetadataDeclarationSet declarations)
    {
        this.target = target;
        this.sourceId = sourceId
            ?? throw new ArgumentNullException(nameof(sourceId));
        this.declarations = declarations
            ?? throw new ArgumentNullException(nameof(declarations));
    }

    public void AddTitle (string value)
    {
        declarations.AddTitle(sourceId, value);
    }

    public void AddDescription (string value)
    {
        declarations.AddDescription(sourceId, value);
    }

    public void AddExample (JsonElement value)
    {
        declarations.AddExample(sourceId, value);
    }

    public void SetConstant (JsonElement value)
    {
        declarations.AddConstant(sourceId, value);
    }

    public void AddMinimum (JsonElement value)
    {
        declarations.AddMinimum(sourceId, value);
    }

    public void AddExclusiveMinimum (JsonElement value)
    {
        declarations.AddExclusiveMinimum(sourceId, value);
    }

    public void AddMaximum (JsonElement value)
    {
        declarations.AddMaximum(sourceId, value);
    }

    public void AddExclusiveMaximum (JsonElement value)
    {
        declarations.AddExclusiveMaximum(sourceId, value);
    }

    public void AddMinimumLength (int value)
    {
        declarations.AddMinimumLength(sourceId, value);
    }

    public void AddMaximumLength (int value)
    {
        declarations.AddMaximumLength(sourceId, value);
    }

    public void AddMinimumItemCount (int value)
    {
        declarations.AddMinimumItemCount(sourceId, value);
    }

    public void AddMaximumItemCount (int value)
    {
        declarations.AddMaximumItemCount(sourceId, value);
    }

    public void AddMinimumPropertyCount (int value)
    {
        declarations.AddMinimumPropertyCount(sourceId, value);
    }

    public void AddMaximumPropertyCount (int value)
    {
        declarations.AddMaximumPropertyCount(sourceId, value);
    }

    public void AddPattern (string value)
    {
        declarations.AddPattern(sourceId, value);
    }

    public void EnsureTypedValueSerializationIsAuthoritative (
        JsonPropertyInfo? propertyInfo,
        string declarationName)
    {
        if (propertyInfo is null
            || (propertyInfo.CustomConverter is null
                && !propertyInfo.NumberHandling.HasValue))
        {
            return;
        }

        throw MetadataFailure.Invalid(
            target,
            new[] { sourceId },
            $"Typed {declarationName} metadata cannot be serialized for "
            + $"property '{propertyInfo.Name}' because its property-level "
            + "converter or number handling cannot be reproduced from the "
            + "value type's JsonTypeInfo.");
    }
}
