using System.Text.Json.Serialization.Metadata;
using MackySoft.JsonSchema.Generation.Projection;

namespace MackySoft.JsonSchema.Generation;

/// <summary> Supplies the authoritative serialization contract for one public JSON value. </summary>
public sealed class JsonContractGenerationRequest
{
    /// <summary> Initializes one contract generation request. </summary>
    /// <param name="contractId"> The product-assigned stable identifier for the public JSON contract. </param>
    /// <param name="typeInfo"> The effective <c>System.Text.Json</c> contract used by the product at runtime. </param>
    /// <param name="documentOptions"> Artifact-level JSON Schema and type metadata options. </param>
    /// <exception cref="ArgumentNullException"> A required argument is <see langword="null" />. </exception>
    public JsonContractGenerationRequest (
        string contractId,
        JsonTypeInfo typeInfo,
        JsonSchemaDocumentOptions documentOptions)
    {
        ContractId = contractId ?? throw new ArgumentNullException(nameof(contractId));
        TypeInfo = typeInfo ?? throw new ArgumentNullException(nameof(typeInfo));
        DocumentOptions = documentOptions ?? throw new ArgumentNullException(nameof(documentOptions));
    }

    /// <summary> Gets the product-assigned stable contract identifier. </summary>
    public string ContractId { get; }

    /// <summary> Gets the effective <c>System.Text.Json</c> contract used as the sole authoritative input. </summary>
    public JsonTypeInfo TypeInfo { get; }

    /// <summary> Gets artifact-level projection options. </summary>
    public JsonSchemaDocumentOptions DocumentOptions { get; }
}
