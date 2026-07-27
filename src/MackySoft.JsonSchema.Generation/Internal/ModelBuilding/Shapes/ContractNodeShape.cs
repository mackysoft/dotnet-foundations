using System.Text.Json;
using MackySoft.JsonSchema.Generation.ContractModel;

namespace MackySoft.JsonSchema.Generation.Internal.ModelBuilding.Shapes;

/// <summary>
/// Carries a normalized structural baseline from serializer metadata or an
/// explicitly mapped surrogate before source declarations are composed into
/// the immutable public model.
/// </summary>
internal sealed class ContractNodeShape
{
    internal ContractNodeShape (
        JsonContractNodeKind kind,
        JsonContractScalarKind? scalarKind = null,
        JsonElement? constant = null,
        IEnumerable<JsonElement>? allowedValues = null,
        string? referenceId = null,
        JsonContractNode? items = null,
        JsonContractNode? additionalProperties = null,
        IEnumerable<JsonContractProperty>? properties = null,
        IEnumerable<JsonContractVariant>? variants = null,
        JsonContractDiscriminator? discriminator = null,
        JsonContractAnnotations? annotations = null,
        string? format = null,
        JsonElement? minimum = null,
        JsonElement? exclusiveMinimum = null,
        JsonElement? maximum = null,
        JsonElement? exclusiveMaximum = null,
        int? minimumLength = null,
        int? maximumLength = null,
        int? minimumItems = null,
        int? maximumItems = null,
        int? minimumProperties = null,
        int? maximumProperties = null,
        string? pattern = null)
    {
        Kind = kind;
        ScalarKind = scalarKind;
        Constant = constant;
        AllowedValues = allowedValues?.ToArray() ?? Array.Empty<JsonElement>();
        ReferenceId = referenceId;
        Items = items;
        AdditionalProperties = additionalProperties;
        Properties = properties?.ToArray() ?? Array.Empty<JsonContractProperty>();
        Variants = variants?.ToArray() ?? Array.Empty<JsonContractVariant>();
        Discriminator = discriminator;
        Annotations = annotations;
        Format = format;
        Minimum = minimum;
        ExclusiveMinimum = exclusiveMinimum;
        Maximum = maximum;
        ExclusiveMaximum = exclusiveMaximum;
        MinimumLength = minimumLength;
        MaximumLength = maximumLength;
        MinimumItems = minimumItems;
        MaximumItems = maximumItems;
        MinimumProperties = minimumProperties;
        MaximumProperties = maximumProperties;
        Pattern = pattern;
    }

    internal JsonContractNodeKind Kind { get; }

    internal JsonContractScalarKind? ScalarKind { get; }

    internal JsonElement? Constant { get; }

    internal IReadOnlyList<JsonElement> AllowedValues { get; }

    internal string? ReferenceId { get; }

    internal JsonContractNode? Items { get; }

    internal JsonContractNode? AdditionalProperties { get; }

    internal IReadOnlyList<JsonContractProperty> Properties { get; }

    internal IReadOnlyList<JsonContractVariant> Variants { get; }

    internal JsonContractDiscriminator? Discriminator { get; }

    internal JsonContractAnnotations? Annotations { get; }

    internal string? Format { get; }

    internal JsonElement? Minimum { get; }

    internal JsonElement? ExclusiveMinimum { get; }

    internal JsonElement? Maximum { get; }

    internal JsonElement? ExclusiveMaximum { get; }

    internal int? MinimumLength { get; }

    internal int? MaximumLength { get; }

    internal int? MinimumItems { get; }

    internal int? MaximumItems { get; }

    internal int? MinimumProperties { get; }

    internal int? MaximumProperties { get; }

    internal string? Pattern { get; }
}
