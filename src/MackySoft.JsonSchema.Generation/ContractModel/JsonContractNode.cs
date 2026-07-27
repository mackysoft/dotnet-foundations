using System.Text.Json;
using MackySoft.JsonSchema.Generation.Internal.Common;

namespace MackySoft.JsonSchema.Generation.ContractModel;

/// <summary> Represents one normalized, read-only value shape in a JSON contract model. </summary>
public sealed class JsonContractNode
{
    internal JsonContractNode (
        JsonContractNodeKind kind,
        bool isNullable,
        JsonContractScalarKind? scalarKind,
        JsonContractAnnotations annotations,
        JsonContractConstraints constraints,
        JsonElement? constant,
        IEnumerable<JsonElement> allowedValues,
        string? referenceId,
        JsonContractNode? items,
        JsonContractNode? additionalProperties,
        IEnumerable<JsonContractProperty> properties,
        IEnumerable<JsonContractVariant> variants,
        JsonContractDiscriminator? discriminator,
        JsonContractSource source)
    {
        Kind = kind;
        IsNullable = isNullable;
        ScalarKind = scalarKind;
        Annotations = annotations ?? throw new ArgumentNullException(nameof(annotations));
        Constraints = constraints ?? throw new ArgumentNullException(nameof(constraints));
        Constant = JsonContractCollections.CloneNullableJsonElement(constant);
        AllowedValues = JsonContractCollections.CloneJsonElements(
            allowedValues,
            nameof(allowedValues));
        ReferenceId = referenceId;
        Items = items;
        AdditionalProperties = additionalProperties;
        Properties = JsonContractCollections.Copy(properties, nameof(properties));
        Variants = JsonContractCollections.Copy(variants, nameof(variants));
        Discriminator = discriminator;
        Source = source ?? throw new ArgumentNullException(nameof(source));
    }

    /// <summary> Gets the node's structural role. </summary>
    public JsonContractNodeKind Kind { get; }

    /// <summary> Gets whether the node also accepts the JSON <see langword="null" /> value. </summary>
    public bool IsNullable { get; }

    /// <summary> Gets the scalar category for scalar, enum, or constant nodes. </summary>
    public JsonContractScalarKind? ScalarKind { get; }

    /// <summary> Gets descriptive metadata shared by every projection. </summary>
    public JsonContractAnnotations Annotations { get; }

    /// <summary> Gets normalized value constraints. </summary>
    public JsonContractConstraints Constraints { get; }

    /// <summary> Gets the required constant value for a constant node. </summary>
    public JsonElement? Constant { get; }

    /// <summary> Gets the finite allowed values for an enum node in deterministic order. </summary>
    public IReadOnlyList<JsonElement> AllowedValues { get; }

    /// <summary> Gets the referenced definition identifier for a reference node. </summary>
    public string? ReferenceId { get; }

    /// <summary> Gets the shared item contract for an array node. </summary>
    public JsonContractNode? Items { get; }

    /// <summary>
    /// Gets the value contract for undeclared object properties, or <see langword="null" /> when they are forbidden.
    /// </summary>
    public JsonContractNode? AdditionalProperties { get; }

    /// <summary> Gets declared object properties in deterministic serialized order. </summary>
    public IReadOnlyList<JsonContractProperty> Properties { get; }

    /// <summary> Gets exclusive alternatives in deterministic order. </summary>
    public IReadOnlyList<JsonContractVariant> Variants { get; }

    /// <summary> Gets the tagged-union discriminator, when one was declared. </summary>
    public JsonContractDiscriminator? Discriminator { get; }

    /// <summary> Gets the CLR declaration from which the node was derived. </summary>
    public JsonContractSource Source { get; }
}
