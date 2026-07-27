namespace MackySoft.JsonSchema.Generation.Extensibility;

/// <summary>
/// Collects explicit typed metadata providers and consumer attribute
/// interpreters before generator options are fixed.
/// </summary>
public sealed class JsonContractMetadataRegistry
{
    private readonly List<MetadataExtensionRegistration> registrations = new();

    /// <summary> Initializes an empty typed metadata registry. </summary>
    public JsonContractMetadataRegistry ()
    {
    }

    /// <summary> Registers one typed metadata provider. </summary>
    /// <typeparam name="TValue"> The CLR value type handled by the provider. </typeparam>
    /// <param name="provider"> The provider to register. </param>
    /// <returns> This registry. </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="provider" /> is <see langword="null" />.
    /// </exception>
    public JsonContractMetadataRegistry RegisterProvider<TValue> (
        IJsonContractMetadataProvider<TValue> provider)
    {
        if (provider is null)
        {
            throw new ArgumentNullException(nameof(provider));
        }

        registrations.Add(
            MetadataExtensionRegistration.Provider(provider));
        return this;
    }

    /// <summary> Registers one typed consumer attribute interpreter. </summary>
    /// <typeparam name="TAttribute"> The consumer-owned attribute type. </typeparam>
    /// <typeparam name="TValue"> The CLR value type handled by the interpreter. </typeparam>
    /// <param name="interpreter"> The interpreter to register. </param>
    /// <returns> This registry. </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="interpreter" /> is <see langword="null" />.
    /// </exception>
    public JsonContractMetadataRegistry RegisterAttributeInterpreter<
        TAttribute,
        TValue> (
        IJsonContractAttributeInterpreter<TAttribute, TValue> interpreter)
        where TAttribute : Attribute
    {
        if (interpreter is null)
        {
            throw new ArgumentNullException(nameof(interpreter));
        }

        registrations.Add(
            MetadataExtensionRegistration.AttributeInterpreter(interpreter));
        return this;
    }

    internal IReadOnlyList<MetadataExtensionRegistration> CreateSnapshot ()
    {
        return Array.AsReadOnly(registrations.ToArray());
    }
}
