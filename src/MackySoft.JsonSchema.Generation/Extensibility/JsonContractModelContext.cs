using MackySoft.JsonSchema.Generation.ContractModel;
namespace MackySoft.JsonSchema.Generation.Extensibility;

/// <summary> Exposes a completed JSON structure to model contributors before projection metadata is attached. </summary>
public sealed class JsonContractModelContext
{
    private readonly IReadOnlyDictionary<JsonContractNode, JsonContractModelTarget>
        nodeTargets;

    private readonly IReadOnlyDictionary<JsonContractProperty, JsonContractModelTarget>
        propertyTargets;

    private readonly IReadOnlyDictionary<JsonContractVariant, JsonContractModelTarget>
        variantTargets;

    private readonly IReadOnlyDictionary<JsonContractDefinition, JsonContractModelTarget>
        definitionTargets;

    internal JsonContractModelContext (
        string contractId,
        JsonContractNode root,
        IReadOnlyList<JsonContractDefinition> definitions,
        JsonContractModelTarget modelTarget,
        JsonContractModelTarget rootTarget,
        IReadOnlyDictionary<JsonContractNode, JsonContractModelTarget> nodeTargets,
        IReadOnlyDictionary<JsonContractProperty, JsonContractModelTarget> propertyTargets,
        IReadOnlyDictionary<JsonContractVariant, JsonContractModelTarget> variantTargets,
        IReadOnlyDictionary<JsonContractDefinition, JsonContractModelTarget> definitionTargets)
    {
        ContractId = contractId ?? throw new ArgumentNullException(nameof(contractId));
        Root = root ?? throw new ArgumentNullException(nameof(root));
        Definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
        ModelTarget = modelTarget ?? throw new ArgumentNullException(nameof(modelTarget));
        RootTarget = rootTarget ?? throw new ArgumentNullException(nameof(rootTarget));
        this.nodeTargets = nodeTargets ?? throw new ArgumentNullException(nameof(nodeTargets));
        this.propertyTargets = propertyTargets ?? throw new ArgumentNullException(nameof(propertyTargets));
        this.variantTargets = variantTargets ?? throw new ArgumentNullException(nameof(variantTargets));
        this.definitionTargets = definitionTargets ?? throw new ArgumentNullException(nameof(definitionTargets));
    }

    /// <summary> Gets the product-assigned stable contract identifier. </summary>
    public string ContractId { get; }

    /// <summary> Gets the completed root JSON value contract. </summary>
    public JsonContractNode Root { get; }

    /// <summary> Gets the completed reusable definitions in deterministic order. </summary>
    public IReadOnlyList<JsonContractDefinition> Definitions { get; }

    /// <summary> Gets the target for metadata attached to the contract model itself. </summary>
    public JsonContractModelTarget ModelTarget { get; }

    /// <summary> Gets the target for metadata attached to the root value node. </summary>
    public JsonContractModelTarget RootTarget { get; }

    /// <summary> Gets the contribution target for a node exposed by this context. </summary>
    /// <param name="node"> The root or nested node obtained from this context. </param>
    /// <returns> The context-scoped contribution target for <paramref name="node" />. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="node" /> is <see langword="null" />. </exception>
    /// <exception cref="ArgumentException"> <paramref name="node" /> does not belong to this context. </exception>
    public JsonContractModelTarget GetTarget (JsonContractNode node)
    {
        return ResolveTarget(nodeTargets, node, nameof(node));
    }

    /// <summary> Gets the contribution target for a property exposed by this context. </summary>
    /// <param name="property"> The property obtained from this context. </param>
    /// <returns> The context-scoped contribution target for <paramref name="property" />. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="property" /> is <see langword="null" />. </exception>
    /// <exception cref="ArgumentException"> <paramref name="property" /> does not belong to this context. </exception>
    public JsonContractModelTarget GetTarget (JsonContractProperty property)
    {
        return ResolveTarget(propertyTargets, property, nameof(property));
    }

    /// <summary> Gets the contribution target for a union variant exposed by this context. </summary>
    /// <param name="variant"> The variant obtained from this context. </param>
    /// <returns> The context-scoped contribution target for <paramref name="variant" />. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="variant" /> is <see langword="null" />. </exception>
    /// <exception cref="ArgumentException"> <paramref name="variant" /> does not belong to this context. </exception>
    public JsonContractModelTarget GetTarget (JsonContractVariant variant)
    {
        return ResolveTarget(variantTargets, variant, nameof(variant));
    }

    /// <summary> Gets the contribution target for a reusable definition exposed by this context. </summary>
    /// <param name="definition"> The definition obtained from this context. </param>
    /// <returns> The context-scoped contribution target for <paramref name="definition" />. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="definition" /> is <see langword="null" />. </exception>
    /// <exception cref="ArgumentException"> <paramref name="definition" /> does not belong to this context. </exception>
    public JsonContractModelTarget GetTarget (JsonContractDefinition definition)
    {
        return ResolveTarget(definitionTargets, definition, nameof(definition));
    }

    private static JsonContractModelTarget ResolveTarget<TElement> (
        IReadOnlyDictionary<TElement, JsonContractModelTarget> targets,
        TElement element,
        string parameterName)
        where TElement : class
    {
        if (element is null)
        {
            throw new ArgumentNullException(parameterName);
        }

        if (!targets.TryGetValue(element, out JsonContractModelTarget? target))
        {
            throw new ArgumentException(
                "The contract model element does not belong to this generation context.",
                parameterName);
        }

        return target;
    }
}
