using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace MackySoft.JsonSchema.Generation.Metadata;

internal interface IJsonContractMetadataDeclarationSink
{
    void AddTitle (string value);

    void AddDescription (string value);

    void AddExample (JsonElement value);

    void SetConstant (JsonElement value);

    void AddMinimum (JsonElement value);

    void AddExclusiveMinimum (JsonElement value);

    void AddMaximum (JsonElement value);

    void AddExclusiveMaximum (JsonElement value);

    void AddMinimumLength (int value);

    void AddMaximumLength (int value);

    void AddMinimumItemCount (int value);

    void AddMaximumItemCount (int value);

    void AddMinimumPropertyCount (int value);

    void AddMaximumPropertyCount (int value);

    void AddPattern (string value);

    void EnsureTypedValueSerializationIsAuthoritative (
        JsonPropertyInfo? propertyInfo,
        string declarationName);
}
