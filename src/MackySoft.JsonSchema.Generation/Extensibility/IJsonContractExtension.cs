namespace MackySoft.JsonSchema.Generation.Extensibility;

/// <summary> Provides deterministic identity for a registered JSON contract extension. </summary>
/// <remarks>
/// An implementation must return the same finite snapshot for the same
/// effective generation input and identity. It must not depend on current
/// time, network state, mutable ambient state, or nondeterministic enumeration.
/// </remarks>
public interface IJsonContractExtension
{
    /// <summary> Gets the stable, product-qualified identifier used for ordering and conflict diagnostics. </summary>
    string StableId { get; }

    /// <summary> Gets the extension contract version included in deterministic generation settings. </summary>
    string ContractVersion { get; }
}
