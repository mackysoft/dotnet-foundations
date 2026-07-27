namespace MackySoft.JsonSchema.Generation.Extensibility;

internal sealed class MetadataExtensionRegistration : IJsonContractExtension
{
    private MetadataExtensionRegistration (
        IJsonContractExtension extension,
        Type valueType,
        Type? attributeType)
    {
        Extension = extension
            ?? throw new ArgumentNullException(nameof(extension));
        ValueType = valueType
            ?? throw new ArgumentNullException(nameof(valueType));
        AttributeType = attributeType;
    }

    internal IJsonContractExtension Extension { get; }

    public string StableId => Extension.StableId;

    public string ContractVersion => Extension.ContractVersion;

    internal Type ValueType { get; }

    internal Type? AttributeType { get; }

    internal bool IsAttributeInterpreter => AttributeType is not null;

    internal static MetadataExtensionRegistration Provider<TValue> (
        IJsonContractMetadataProvider<TValue> provider)
    {
        return new MetadataExtensionRegistration(
            provider,
            typeof(TValue),
            attributeType: null);
    }

    internal static MetadataExtensionRegistration AttributeInterpreter<
        TAttribute,
        TValue> (
        IJsonContractAttributeInterpreter<TAttribute, TValue> interpreter)
        where TAttribute : Attribute
    {
        return new MetadataExtensionRegistration(
            interpreter,
            typeof(TValue),
            typeof(TAttribute));
    }
}
