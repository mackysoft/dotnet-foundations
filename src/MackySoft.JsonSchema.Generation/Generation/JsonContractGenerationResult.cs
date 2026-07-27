using MackySoft.JsonSchema.Generation.ContractModel;
namespace MackySoft.JsonSchema.Generation;

/// <summary> Carries one immutable model and deterministic projections generated from it. </summary>
public sealed class JsonContractGenerationResult
{
    private readonly byte[] jsonSchemaUtf8;

    private readonly byte[] typeMetadataUtf8;

    internal JsonContractGenerationResult (
        JsonContractModel model,
        byte[] jsonSchemaUtf8,
        byte[] typeMetadataUtf8)
    {
        Model = model;
        this.jsonSchemaUtf8 = (byte[])jsonSchemaUtf8.Clone();
        this.typeMetadataUtf8 = (byte[])typeMetadataUtf8.Clone();
    }

    /// <summary> Gets the normalized read-only JSON Contract Model. </summary>
    public JsonContractModel Model { get; }

    /// <summary> Gets the semantic digest shared by the model and both projections. </summary>
    public string ContractDigest => Model.ContractDigest;

    /// <summary> Returns a caller-owned copy of the deterministic UTF-8 JSON Schema projection. </summary>
    /// <returns> A new byte array that does not share mutable storage with this result. </returns>
    /// <remarks>
    /// A fragment omits <c>$schema</c> and <c>$id</c>. When it contains local
    /// <c>#/$defs/...</c> references, the fragment is a schema resource root;
    /// inserting it below another resource root without an explicit resource
    /// boundary changes reference resolution and is not supported.
    /// </remarks>
    public byte[] GetJsonSchemaUtf8 ()
    {
        return (byte[])jsonSchemaUtf8.Clone();
    }

    /// <summary> Returns a caller-owned copy of the deterministic UTF-8 type metadata projection. </summary>
    /// <returns> A new byte array that does not share mutable storage with this result. </returns>
    /// <remarks>
    /// <para>
    /// In package version 0.2.0 the root object always contains, in order,
    /// <c>contractId</c>, <c>contractDigest</c>, <c>schemaName</c>, <c>root</c>,
    /// <c>definitions</c>, and <c>contributions</c>.
    /// </para>
    /// <para>
    /// Every node always contains <c>kind</c>, <c>isNullable</c>,
    /// <c>scalarKind</c>, <c>annotations</c>, <c>constraints</c>,
    /// <c>constant</c>, <c>allowedValues</c>, <c>referenceId</c>, <c>items</c>,
    /// <c>additionalProperties</c>, <c>properties</c>, <c>variants</c>, and
    /// <c>discriminator</c>. Annotation objects contain <c>title</c>,
    /// <c>description</c>, and <c>examples</c>. Constraint objects contain
    /// <c>minimum</c>, <c>exclusiveMinimum</c>, <c>maximum</c>,
    /// <c>exclusiveMaximum</c>, <c>minimumLength</c>, <c>maximumLength</c>,
    /// <c>minimumItems</c>, <c>maximumItems</c>, <c>minimumProperties</c>,
    /// <c>maximumProperties</c>, <c>pattern</c>, and <c>format</c>.
    /// </para>
    /// <para>
    /// Property objects contain <c>name</c>, <c>isRequired</c>, and
    /// <c>value</c>. Variant objects contain <c>name</c>, <c>value</c>,
    /// <c>requiredProperties</c>, <c>discriminatorValue</c>, and
    /// <c>annotations</c>. A non-null discriminator object contains
    /// <c>propertyName</c>. Definition objects contain <c>id</c> and
    /// <c>value</c>. Contribution objects contain <c>targetPointer</c>,
    /// <c>name</c>, <c>value</c>, and <c>sourceId</c>.
    /// </para>
    /// <para>
    /// Listed fields remain present when their value is JSON <see langword="null" />
    /// or an empty array. Arrays preserve the deterministic order of the
    /// corresponding <see cref="JsonContractModel" /> collections.
    /// </para>
    /// <para>
    /// Version 0.2.0 emits no independent metadata-format version field.
    /// Consumers pinning exact package version 0.2.0 may depend on this shape;
    /// compatibility with another package version requires an explicit upgrade
    /// review by the consumer.
    /// </para>
    /// </remarks>
    public byte[] GetTypeMetadataUtf8 ()
    {
        return (byte[])typeMetadataUtf8.Clone();
    }
}
