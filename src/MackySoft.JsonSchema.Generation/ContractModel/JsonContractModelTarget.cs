namespace MackySoft.JsonSchema.Generation.ContractModel;

/// <summary>
/// Identifies one contribution target in the completed semantic contract model exposed to a model contributor.
/// </summary>
/// <remarks>
/// Instances are obtained from <c>JsonContractModelContext</c>. A target belongs
/// to the generation context that created it and cannot be reused with another
/// context.
/// </remarks>
public sealed class JsonContractModelTarget
{
    internal JsonContractModelTarget (
        object owner,
        string pointer)
    {
        Owner = owner ?? throw new ArgumentNullException(nameof(owner));
        Pointer = pointer ?? throw new ArgumentNullException(nameof(pointer));
    }

    internal object Owner { get; }

    /// <summary>
    /// Gets the semantic JSON Pointer written to the contract model and type metadata projections.
    /// </summary>
    public string Pointer { get; }
}
