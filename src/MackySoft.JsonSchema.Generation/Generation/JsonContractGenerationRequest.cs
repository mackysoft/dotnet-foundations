using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using MackySoft.JsonSchema.Generation.Projection;

namespace MackySoft.JsonSchema.Generation;

/// <summary> Supplies the authoritative serialization contract for one public JSON value. </summary>
public sealed class JsonContractGenerationRequest
{
    private readonly JsonSerializerOptions serializerOptions;

    /// <summary> Initializes one contract generation request. </summary>
    /// <param name="contractId"> The product-assigned stable identifier for the public JSON contract. </param>
    /// <param name="contractType"> The DTO type that represents the public JSON value. </param>
    /// <param name="serializerOptions">
    /// The serializer settings used by the product at runtime. A private
    /// snapshot is captured by this constructor; later mutations are not
    /// observed.
    /// </param>
    /// <param name="typeInfoResolver">
    /// The authoritative resolver, including a source-generated context when
    /// applicable, used at runtime. It replaces any resolver carried by
    /// <paramref name="serializerOptions" /> in the captured snapshot.
    /// </param>
    /// <param name="documentOptions"> Artifact-level JSON Schema and type metadata options. </param>
    /// <exception cref="ArgumentNullException"> A required argument is <see langword="null" />. </exception>
    public JsonContractGenerationRequest (
        string contractId,
        Type contractType,
        JsonSerializerOptions serializerOptions,
        IJsonTypeInfoResolver typeInfoResolver,
        JsonSchemaDocumentOptions documentOptions)
    {
        ContractId = contractId ?? throw new ArgumentNullException(nameof(contractId));
        ContractType = contractType ?? throw new ArgumentNullException(nameof(contractType));
        if (serializerOptions == null)
        {
            throw new ArgumentNullException(nameof(serializerOptions));
        }
        TypeInfoResolver = typeInfoResolver ?? throw new ArgumentNullException(nameof(typeInfoResolver));
        DocumentOptions = documentOptions ?? throw new ArgumentNullException(nameof(documentOptions));

        this.serializerOptions = new JsonSerializerOptions(serializerOptions)
        {
            TypeInfoResolver = typeInfoResolver,
        };
    }

    /// <summary> Gets the product-assigned stable contract identifier. </summary>
    public string ContractId { get; }

    /// <summary> Gets the DTO type that represents the public JSON value. </summary>
    public Type ContractType { get; }

    /// <summary> Gets the resolver used to obtain authoritative <c>System.Text.Json</c> type information. </summary>
    public IJsonTypeInfoResolver TypeInfoResolver { get; }

    /// <summary> Gets artifact-level projection options. </summary>
    public JsonSchemaDocumentOptions DocumentOptions { get; }

    internal JsonSerializerOptions CreateSerializerOptions ()
    {
        return new JsonSerializerOptions(serializerOptions)
        {
            TypeInfoResolver = TypeInfoResolver,
        };
    }
}
