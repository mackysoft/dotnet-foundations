using System.Text.Json;
using MackySoft.JsonSchema.Generation.ContractModel;
using MackySoft.JsonSchema.Generation.Internal.Determinism;

namespace MackySoft.JsonSchema.Generation.Extensibility;

/// <summary> Provides a completed contract model and its immutable base JSON Schema projection. </summary>
public sealed class JsonSchemaDocumentContext
{
    internal JsonSchemaDocumentContext (
        JsonContractModel model,
        JsonElement baseDocument)
    {
        Model = model ?? throw new ArgumentNullException(nameof(model));
        BaseDocument = JsonElementUtility.Clone(baseDocument);
    }

    /// <summary> Gets the model from which the schema was projected. </summary>
    public JsonContractModel Model { get; }

    /// <summary> Gets an independently owned snapshot of the schema before vendor extensions are applied. </summary>
    public JsonElement BaseDocument { get; }
}
