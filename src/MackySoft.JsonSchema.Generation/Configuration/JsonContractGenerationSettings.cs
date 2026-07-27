using MackySoft.Text.Vocabularies;

namespace MackySoft.JsonSchema.Generation.Configuration;

/// <summary> Defines the JSON-value semantics used by contract model projections. </summary>
public sealed class JsonContractGenerationSettings
{
    /// <summary> Gets the JSON Schema dialect emitted by this package version. </summary>
    public const string Draft202012Dialect = "https://json-schema.org/draft/2020-12/schema";

    /// <summary> Initializes settings for one contract generation session. </summary>
    /// <param name="objectClosure"> The policy applied to generated object schemas. </param>
    public JsonContractGenerationSettings (JsonObjectClosure objectClosure)
    {
        if (!Vocabulary.IsDefined(objectClosure))
        {
            throw new ArgumentOutOfRangeException(nameof(objectClosure), objectClosure, "The object closure policy is not declared.");
        }

        ObjectClosure = objectClosure;
    }

    /// <summary> Gets settings that reject undeclared object properties with <c>additionalProperties: false</c>. </summary>
    public static JsonContractGenerationSettings ClosedObjects { get; } =
        new(JsonObjectClosure.DisallowAdditionalProperties);

    /// <summary> Gets the JSON Schema dialect. </summary>
    public string Dialect => Draft202012Dialect;

    /// <summary> Gets the object closure policy. </summary>
    public JsonObjectClosure ObjectClosure { get; }

    /// <summary> Gets the nullability projection used by this package version. </summary>
    public JsonNullabilityProjection NullabilityProjection => JsonNullabilityProjection.TypeUnion;

    /// <summary> Gets the reference projection used by this package version. </summary>
    public JsonReferenceProjection ReferenceProjection => JsonReferenceProjection.LocalDefinitions;
}
