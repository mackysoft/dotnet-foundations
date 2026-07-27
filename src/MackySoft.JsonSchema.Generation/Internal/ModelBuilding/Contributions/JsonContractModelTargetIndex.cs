using System.Globalization;
using System.Runtime.CompilerServices;
using MackySoft.JsonSchema.Generation.ContractModel;
using MackySoft.JsonSchema.Generation.Extensibility;

namespace MackySoft.JsonSchema.Generation.Internal.ModelBuilding.Contributions;

/// <summary>
/// Owns the semantic model topology shared by contributor navigation and
/// contribution validation.
/// </summary>
internal sealed class JsonContractModelTargetIndex
{
    private readonly object ownerToken = new();

    private readonly Dictionary<string, SemanticTarget> semanticTargets =
        new(StringComparer.Ordinal);

    private readonly Dictionary<JsonContractNode, JsonContractModelTarget>
        nodeTargets =
            new(ReferenceIdentityComparer<JsonContractNode>.Instance);

    private readonly Dictionary<JsonContractProperty, JsonContractModelTarget>
        propertyTargets =
            new(ReferenceIdentityComparer<JsonContractProperty>.Instance);

    private readonly Dictionary<JsonContractVariant, JsonContractModelTarget>
        variantTargets =
            new(ReferenceIdentityComparer<JsonContractVariant>.Instance);

    private readonly Dictionary<JsonContractDefinition, JsonContractModelTarget>
        definitionTargets =
            new(ReferenceIdentityComparer<JsonContractDefinition>.Instance);

    private JsonContractModelTargetIndex (
        JsonContractNode root,
        IReadOnlyList<JsonContractDefinition> definitions)
    {
        ModelTarget = AddTarget(
            string.Empty,
            root.Source.TargetType,
            jsonPropertyName: null);
        RootTarget = AddNodeTarget(
            "/root",
            root,
            jsonPropertyName: null);

        for (int index = 0; index < definitions.Count; index++)
        {
            JsonContractDefinition definition = definitions[index];
            string definitionPointer =
                $"/definitions/{index.ToString(CultureInfo.InvariantCulture)}";
            JsonContractModelTarget definitionTarget = AddTarget(
                definitionPointer,
                definition.Source.TargetType,
                jsonPropertyName: null);
            definitionTargets.Add(definition, definitionTarget);
            AddNodeTarget(
                $"{definitionPointer}/value",
                definition.Value,
                jsonPropertyName: null);
        }
    }

    internal JsonContractModelTarget ModelTarget { get; }

    internal JsonContractModelTarget RootTarget { get; }

    internal static JsonContractModelTargetIndex Create (
        JsonContractNode root,
        IReadOnlyList<JsonContractDefinition> definitions)
    {
        if (root is null)
        {
            throw new ArgumentNullException(nameof(root));
        }

        if (definitions is null)
        {
            throw new ArgumentNullException(nameof(definitions));
        }

        return new JsonContractModelTargetIndex(root, definitions);
    }

    internal JsonContractModelContext CreateContext (
        string contractId,
        JsonContractNode root,
        IReadOnlyList<JsonContractDefinition> definitions)
    {
        return new JsonContractModelContext(
            contractId,
            root,
            definitions,
            ModelTarget,
            RootTarget,
            nodeTargets,
            propertyTargets,
            variantTargets,
            definitionTargets);
    }

    internal bool TryResolve (
        JsonContractModelTarget target,
        out Type? targetType,
        out string? jsonPropertyName)
    {
        if (target is not null
            && ReferenceEquals(target.Owner, ownerToken)
            && semanticTargets.TryGetValue(
                target.Pointer,
                out SemanticTarget? semanticTarget))
        {
            targetType = semanticTarget.TargetType;
            jsonPropertyName = semanticTarget.JsonPropertyName;
            return true;
        }

        targetType = null;
        jsonPropertyName = null;
        return false;
    }

    private JsonContractModelTarget AddNodeTarget (
        string pointer,
        JsonContractNode node,
        string? jsonPropertyName)
    {
        JsonContractModelTarget nodeTarget = AddTarget(
            pointer,
            node.Source.TargetType,
            jsonPropertyName);
        nodeTargets.Add(node, nodeTarget);

        if (node.Items is not null)
        {
            AddNodeTarget(
                $"{pointer}/items",
                node.Items,
                jsonPropertyName: null);
        }

        if (node.AdditionalProperties is not null)
        {
            AddNodeTarget(
                $"{pointer}/additionalProperties",
                node.AdditionalProperties,
                jsonPropertyName: null);
        }

        for (int index = 0; index < node.Properties.Count; index++)
        {
            JsonContractProperty property = node.Properties[index];
            string propertyPointer =
                $"{pointer}/properties/{index.ToString(CultureInfo.InvariantCulture)}";
            JsonContractModelTarget propertyTarget = AddTarget(
                propertyPointer,
                property.Source.TargetType,
                property.Name);
            propertyTargets.Add(property, propertyTarget);
            AddNodeTarget(
                $"{propertyPointer}/value",
                property.Value,
                property.Name);
        }

        for (int index = 0; index < node.Variants.Count; index++)
        {
            JsonContractVariant variant = node.Variants[index];
            string variantPointer =
                $"{pointer}/variants/{index.ToString(CultureInfo.InvariantCulture)}";
            JsonContractModelTarget variantTarget = AddTarget(
                variantPointer,
                node.Source.TargetType,
                jsonPropertyName);
            variantTargets.Add(variant, variantTarget);
            AddNodeTarget(
                $"{variantPointer}/value",
                variant.Value,
                jsonPropertyName);
        }

        return nodeTarget;
    }

    private JsonContractModelTarget AddTarget (
        string pointer,
        Type targetType,
        string? jsonPropertyName)
    {
        var target = new JsonContractModelTarget(ownerToken, pointer);
        semanticTargets.Add(
            pointer,
            new SemanticTarget(targetType, jsonPropertyName));
        return target;
    }

    private sealed class SemanticTarget
    {
        internal SemanticTarget (
            Type targetType,
            string? jsonPropertyName)
        {
            TargetType = targetType;
            JsonPropertyName = jsonPropertyName;
        }

        internal Type TargetType { get; }

        internal string? JsonPropertyName { get; }
    }

    private sealed class ReferenceIdentityComparer<T> : IEqualityComparer<T>
        where T : class
    {
        internal static readonly ReferenceIdentityComparer<T> Instance = new();

        public bool Equals (T? left, T? right)
        {
            return ReferenceEquals(left, right);
        }

        public int GetHashCode (T value)
        {
            return RuntimeHelpers.GetHashCode(value);
        }
    }
}
