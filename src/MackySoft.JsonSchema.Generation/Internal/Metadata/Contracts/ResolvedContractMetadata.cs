using System.Text.Json;
using MackySoft.JsonSchema.Generation.ContractModel;
using MackySoft.JsonSchema.Generation.Internal.Common;
using MackySoft.JsonSchema.Generation.Metadata;

namespace MackySoft.JsonSchema.Generation.Internal.Metadata.Contracts;

internal sealed class ResolvedContractMetadata
{
    private readonly IReadOnlyList<MetadataProvenance> metadataDeclarations;
    private readonly IReadOnlyList<OneOfBranchProvenance> oneOfBranchDeclarations;
    private readonly IReadOnlyList<DiscriminatorProvenance> discriminatorDeclarations;

    internal ResolvedContractMetadata (
        JsonContractAnnotations annotations,
        JsonContractConstraints constraints,
        bool? isRequired,
        bool? allowsNull,
        bool isArbitrary,
        JsonElement? constant,
        IEnumerable<JsonElement> allowedValues,
        string? discriminatorPropertyName,
        IEnumerable<ResolvedOneOfBranch> oneOfBranches,
        IEnumerable<MetadataProvenance> metadataDeclarations,
        IEnumerable<OneOfBranchProvenance> oneOfBranchDeclarations,
        IEnumerable<DiscriminatorProvenance> discriminatorDeclarations)
    {
        Annotations = annotations ?? throw new ArgumentNullException(nameof(annotations));
        Constraints = constraints ?? throw new ArgumentNullException(nameof(constraints));
        IsRequired = isRequired;
        AllowsNull = allowsNull;
        IsArbitrary = isArbitrary;
        Constant = JsonContractCollections.CloneNullableJsonElement(constant);
        AllowedValues = JsonContractCollections.CloneJsonElements(
            allowedValues,
            nameof(allowedValues));
        DiscriminatorPropertyName = discriminatorPropertyName;
        OneOfBranches = JsonContractCollections.Copy(
            oneOfBranches,
            nameof(oneOfBranches));
        this.metadataDeclarations = JsonContractCollections.Copy(
            metadataDeclarations,
            nameof(metadataDeclarations));
        this.oneOfBranchDeclarations = JsonContractCollections.Copy(
            oneOfBranchDeclarations,
            nameof(oneOfBranchDeclarations));
        this.discriminatorDeclarations = JsonContractCollections.Copy(
            discriminatorDeclarations,
            nameof(discriminatorDeclarations));
    }

    public JsonContractAnnotations Annotations { get; }

    public JsonContractConstraints Constraints { get; }

    public bool? IsRequired { get; }

    public bool? AllowsNull { get; }

    public bool IsArbitrary { get; }

    public JsonElement? Constant { get; }

    public IReadOnlyList<JsonElement> AllowedValues { get; }

    public string? DiscriminatorPropertyName { get; }

    public IReadOnlyList<ResolvedOneOfBranch> OneOfBranches { get; }

    internal IReadOnlyList<MetadataProvenance> MetadataDeclarations =>
        metadataDeclarations;

    internal IReadOnlyList<OneOfBranchProvenance> OneOfBranchDeclarations =>
        oneOfBranchDeclarations;

    internal IReadOnlyList<DiscriminatorProvenance> DiscriminatorDeclarations =>
        discriminatorDeclarations;

    internal sealed class MetadataProvenance
    {
        internal MetadataProvenance (
            string sourceId,
            JsonContractMetadata metadata)
        {
            SourceId = sourceId ?? throw new ArgumentNullException(nameof(sourceId));
            Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        }

        public string SourceId { get; }

        public JsonContractMetadata Metadata { get; }
    }

    internal sealed class OneOfBranchProvenance
    {
        internal OneOfBranchProvenance (
            string sourceId,
            ResolvedOneOfBranch branch)
        {
            SourceId = sourceId ?? throw new ArgumentNullException(nameof(sourceId));
            Branch = branch ?? throw new ArgumentNullException(nameof(branch));
        }

        public string SourceId { get; }

        public ResolvedOneOfBranch Branch { get; }
    }

    internal sealed class DiscriminatorProvenance
    {
        internal DiscriminatorProvenance (
            string sourceId,
            string propertyName)
        {
            SourceId = sourceId ?? throw new ArgumentNullException(nameof(sourceId));
            PropertyName = propertyName
                ?? throw new ArgumentNullException(nameof(propertyName));
        }

        public string SourceId { get; }

        public string PropertyName { get; }
    }
}
