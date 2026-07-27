using System.Text.Json;
using MackySoft.JsonSchema.Generation.ContractModel;
using MackySoft.JsonSchema.Generation.Internal.Common;

namespace MackySoft.JsonSchema.Generation.Internal.Metadata.Contracts;

internal sealed class ResolvedContractMetadata
{
    private readonly MetadataDeclarationSnapshot declarations;

    internal ResolvedContractMetadata (
        JsonContractAnnotations annotations,
        JsonContractConstraints constraints,
        JsonElement? constant,
        MetadataDeclarationSnapshot declarations)
    {
        Annotations = annotations ?? throw new ArgumentNullException(nameof(annotations));
        Constraints = constraints ?? throw new ArgumentNullException(nameof(constraints));
        Constant = JsonContractCollections.CloneNullableJsonElement(constant);
        this.declarations = declarations
            ?? throw new ArgumentNullException(nameof(declarations));
    }

    public JsonContractAnnotations Annotations { get; }

    public JsonContractConstraints Constraints { get; }

    public JsonElement? Constant { get; }

    internal IReadOnlyList<string> TitleSourceIds =>
        SourceIds(declarations.Annotations.Titles);

    internal IReadOnlyList<string> DescriptionSourceIds =>
        SourceIds(declarations.Annotations.Descriptions);

    internal MetadataDeclarationSnapshot Declarations => declarations;

    private static IReadOnlyList<string> SourceIds<TValue> (
        IEnumerable<MetadataDeclarationSnapshotEntry<TValue>> values)
    {
        return Array.AsReadOnly(
            values
                .Select(static value => value.SourceId)
                .ToArray());
    }
}
