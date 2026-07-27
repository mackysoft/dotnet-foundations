using MackySoft.JsonSchema.Generation.ContractModel;
using MackySoft.JsonSchema.Generation.Diagnostics;
using MackySoft.JsonSchema.Generation.Internal.Determinism;
using MackySoft.JsonSchema.Generation.Internal.Determinism.Digests;
using MackySoft.JsonSchema.Generation.Internal.ModelBuilding;
using MackySoft.JsonSchema.Generation.Internal.ModelBuilding.Contributions;
using MackySoft.JsonSchema.Generation.Internal.Projection.JsonSchema;
using MackySoft.JsonSchema.Generation.Internal.Projection.TypeMetadata;

namespace MackySoft.JsonSchema.Generation;

/// <summary> Builds immutable JSON Contract Models and deterministic projections from authoritative serializer contracts. </summary>
public sealed class JsonContractGenerator
{
    private readonly JsonContractGeneratorOptions options;

    /// <summary> Initializes a generator with fixed semantic settings and extension registrations. </summary>
    /// <param name="options"> Settings and extensions shared by every generation request. </param>
    /// <exception cref="ArgumentNullException"> <paramref name="options" /> is <see langword="null" />. </exception>
    public JsonContractGenerator (JsonContractGeneratorOptions options)
    {
        this.options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary> Generates one contract model, JSON Schema projection, and type metadata projection. </summary>
    /// <param name="request"> The authoritative input for one public JSON contract. </param>
    /// <returns> The immutable model and caller-owned projection accessors. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="request" /> is <see langword="null" />. </exception>
    /// <exception cref="JsonContractGenerationException"> The request cannot be interpreted without violating the generation contract. </exception>
    public JsonContractGenerationResult Generate (JsonContractGenerationRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        return GenerateSet(new[] { request })[0];
    }

    /// <summary>
    /// Generates a deterministic contract set and rejects every repeated contract ID, including repeated equivalent requests.
    /// </summary>
    /// <param name="requests"> The complete finite request set. </param>
    /// <returns> Results ordered by contract ID in Unicode code-point order. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="requests" /> is <see langword="null" />. </exception>
    /// <exception cref="ArgumentException"> The request set contains <see langword="null" />. </exception>
    /// <exception cref="JsonContractGenerationException"> A contract ID is invalid or duplicated, or a request cannot be generated. </exception>
    public IReadOnlyList<JsonContractGenerationResult> GenerateSet (
        IEnumerable<JsonContractGenerationRequest> requests)
    {
        if (requests == null)
        {
            throw new ArgumentNullException(nameof(requests));
        }

        JsonContractGenerationRequest[] orderedRequests = requests.ToArray();
        if (orderedRequests.Any(static request => request == null))
        {
            throw new ArgumentException(
                "A contract generation request set cannot contain null.",
                nameof(requests));
        }

        foreach (JsonContractGenerationRequest request in orderedRequests)
        {
            ValidateContractId(request.ContractId, request.TypeInfo.Type);
        }

        Array.Sort(
            orderedRequests,
            static (left, right) =>
                UnicodeCodePointComparer.Instance.Compare(left.ContractId, right.ContractId));

        for (int index = 1; index < orderedRequests.Length; index++)
        {
            if (string.Equals(
                orderedRequests[index - 1].ContractId,
                orderedRequests[index].ContractId,
                StringComparison.Ordinal))
            {
                JsonContractGenerationRequest request = orderedRequests[index];
                throw new JsonContractGenerationException(
                    JsonContractGenerationFailureKind.DuplicateContractId,
                    $"Generation set contains contract ID '{request.ContractId}' more than once.",
                    request.ContractId,
                    request.TypeInfo.Type);
            }
        }

        JsonContractGenerationResult[] results =
            new JsonContractGenerationResult[orderedRequests.Length];
        for (int index = 0; index < orderedRequests.Length; index++)
        {
            results[index] = GenerateValidated(orderedRequests[index]);
        }

        return Array.AsReadOnly(results);
    }

    private JsonContractGenerationResult GenerateValidated (
        JsonContractGenerationRequest request)
    {
        ContractModelBuilder builder = new(
            request.ContractId,
            request.TypeInfo,
            options.Settings,
            options.MetadataExtensions,
            options.TypeMappers);

        ContractModelStructure structure = builder.Build();
        IReadOnlyList<JsonContractModelContribution> contributions =
            ModelContributionResolver.Resolve(
                request.ContractId,
                structure.Root,
                structure.Definitions,
                options.ModelContributors);

        JsonContractModel modelWithoutDigest = new(
            request.ContractId,
            string.Empty,
            structure.Root,
            structure.Definitions,
            contributions);
        string digest = ContractDigestCalculator.Calculate(
            modelWithoutDigest,
            options.Settings,
            options.MetadataExtensions,
            options.TypeMappers,
            options.ModelContributors);
        JsonContractModel model = new(
            request.ContractId,
            digest,
            structure.Root,
            structure.Definitions,
            contributions);

        byte[] schema = JsonSchemaEmitter.Emit(
            model,
            options.Settings,
            request.DocumentOptions,
            options.DocumentPostProcessors);
        byte[] typeMetadata = TypeMetadataEmitter.Emit(
            model,
            request.DocumentOptions);

        return new JsonContractGenerationResult(model, schema, typeMetadata);
    }

    private static void ValidateContractId (
        string contractId,
        Type contractType)
    {
        bool isValid = contractId.Length is >= 1 and <= 256
            && IsAsciiAlphaNumeric(contractId[0]);
        for (int index = 1; isValid && index < contractId.Length; index++)
        {
            char character = contractId[index];
            isValid = IsAsciiAlphaNumeric(character)
                || character is '.' or '_' or ':' or '/' or '@' or '-';
        }

        if (!isValid)
        {
            throw new JsonContractGenerationException(
                JsonContractGenerationFailureKind.InvalidContractId,
                "Contract ID must contain 1 through 256 characters and match [A-Za-z0-9][A-Za-z0-9._:/@-]*.",
                contractId,
                contractType);
        }
    }

    private static bool IsAsciiAlphaNumeric (char value)
    {
        return (value >= 'A' && value <= 'Z')
            || (value >= 'a' && value <= 'z')
            || (value >= '0' && value <= '9');
    }
}
