using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using MackySoft.JsonSchema.Generation.ContractModel;
using MackySoft.JsonSchema.Generation.Internal.Determinism;
using MackySoft.JsonSchema.Generation.Internal.ModelBuilding.Shapes;

namespace MackySoft.JsonSchema.Generation.Internal.ModelBuilding.Variants;

/// <summary>
/// Resolves a finite System.Text.Json polymorphism registration into
/// deterministic oneOf branches and applies each synthetic discriminator to
/// its registered derived-type definition.
/// </summary>
internal sealed class SystemTextJsonPolymorphismResolver
{
    private readonly Func<
        Type,
        string,
        string,
        JsonElement,
        JsonContractNode> registerDefinitionReference;
    private readonly Func<
        Type,
        string,
        JsonElement,
        JsonContractNode> composeDiscriminatorNode;
    private readonly Func<Type, string, string, Exception?, Exception>
        invalidDiscriminator;
    private readonly Dictionary<Type, PolymorphicRegistration>
        registrations = new();

    internal SystemTextJsonPolymorphismResolver (
        Func<
            Type,
            string,
            string,
            JsonElement,
            JsonContractNode> registerDefinitionReference,
        Func<
            Type,
            string,
            JsonElement,
            JsonContractNode> composeDiscriminatorNode,
        Func<Type, string, string, Exception?, Exception>
            invalidDiscriminator)
    {
        this.registerDefinitionReference = registerDefinitionReference
            ?? throw new ArgumentNullException(
                nameof(registerDefinitionReference));
        this.composeDiscriminatorNode = composeDiscriminatorNode
            ?? throw new ArgumentNullException(
                nameof(composeDiscriminatorNode));
        this.invalidDiscriminator = invalidDiscriminator
            ?? throw new ArgumentNullException(nameof(invalidDiscriminator));
    }

    internal void ValidateRegistration (
        Type targetType,
        JsonPolymorphismOptions polymorphism)
    {
        if (targetType is null)
        {
            throw new ArgumentNullException(nameof(targetType));
        }

        if (polymorphism is null)
        {
            throw new ArgumentNullException(nameof(polymorphism));
        }

        if (registrations.ContainsKey(targetType))
        {
            return;
        }

        ValidateClosedRegistration(targetType, polymorphism);

        string discriminatorPropertyName =
            polymorphism.TypeDiscriminatorPropertyName;
        IReadOnlyList<PolymorphicBranch> branches = ResolveBranches(
            targetType,
            discriminatorPropertyName,
            polymorphism.DerivedTypes);
        registrations.Add(
            targetType,
            new PolymorphicRegistration(
                discriminatorPropertyName,
                branches));
    }

    internal ContractNodeShape ResolveShape (Type targetType)
    {
        if (!registrations.TryGetValue(
            targetType,
            out PolymorphicRegistration? registration))
        {
            throw new InvalidOperationException(
                $"Polymorphism registration for '{targetType.FullName}' was not validated before model construction.");
        }

        var variants = new List<JsonContractVariant>(
            registration.Branches.Count);
        foreach (PolymorphicBranch branch in registration.Branches)
        {
            JsonContractNode value = registerDefinitionReference(
                branch.DerivedType,
                registration.DiscriminatorPropertyName,
                branch.CanonicalDiscriminatorValue,
                branch.DiscriminatorValue);
            variants.Add(
                new JsonContractVariant(
                    GetVariantName(branch.DiscriminatorValue),
                    value,
                    Array.Empty<string>(),
                    branch.DiscriminatorValue,
                    EmptyAnnotations()));
        }

        return new ContractNodeShape(
            JsonContractNodeKind.OneOf,
            variants: variants,
            discriminator: new JsonContractDiscriminator(
                registration.DiscriminatorPropertyName));
    }

    internal JsonContractNode ApplyDefinitionDiscriminator (
        Type targetType,
        JsonContractNode value,
        string? propertyName,
        JsonElement? discriminatorValue)
    {
        if (targetType is null)
        {
            throw new ArgumentNullException(nameof(targetType));
        }

        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        if (propertyName is null || !discriminatorValue.HasValue)
        {
            return value;
        }

        return InjectDiscriminator(
            targetType,
            value,
            propertyName,
            discriminatorValue.Value);
    }

    private void ValidateClosedRegistration (
        Type targetType,
        JsonPolymorphismOptions polymorphism)
    {
        if (!targetType.IsAbstract && !targetType.IsInterface)
        {
            throw invalidDiscriminator(
                targetType,
                polymorphism.TypeDiscriminatorPropertyName,
                "A concrete polymorphic base can serialize an untagged base value and is not a closed oneOf contract.",
                null);
        }

        if (polymorphism.IgnoreUnrecognizedTypeDiscriminators
            || polymorphism.UnknownDerivedTypeHandling
                != JsonUnknownDerivedTypeHandling.FailSerialization)
        {
            throw invalidDiscriminator(
                targetType,
                polymorphism.TypeDiscriminatorPropertyName,
                "Polymorphism must reject unrecognized discriminator values and undeclared derived runtime types.",
                null);
        }

        if (polymorphism.DerivedTypes.Count == 0)
        {
            throw invalidDiscriminator(
                targetType,
                polymorphism.TypeDiscriminatorPropertyName,
                "Polymorphism must declare at least one derived type.",
                null);
        }

        if (string.IsNullOrWhiteSpace(
            polymorphism.TypeDiscriminatorPropertyName))
        {
            throw invalidDiscriminator(
                targetType,
                polymorphism.TypeDiscriminatorPropertyName,
                "Polymorphism must declare a non-empty discriminator property name.",
                null);
        }

        if (IsReservedMetadataPropertyName(
            polymorphism.TypeDiscriminatorPropertyName))
        {
            throw invalidDiscriminator(
                targetType,
                polymorphism.TypeDiscriminatorPropertyName,
                "A polymorphic discriminator cannot use a reserved System.Text.Json metadata property name.",
                null);
        }
    }

    private IReadOnlyList<PolymorphicBranch> ResolveBranches (
        Type targetType,
        string discriminatorPropertyName,
        IList<JsonDerivedType> derivedTypes)
    {
        var branches = new List<PolymorphicBranch>(derivedTypes.Count);
        var seenTypes = new HashSet<Type>();
        var seenValues = new List<JsonElement>();
        foreach (JsonDerivedType derivedType in derivedTypes)
        {
            Type? registeredType = derivedType.DerivedType;
            if (registeredType is null)
            {
                throw invalidDiscriminator(
                    targetType,
                    discriminatorPropertyName,
                    "Polymorphism contains a derived-type registration with no CLR type.",
                    null);
            }

            if (registeredType == targetType
                || !targetType.IsAssignableFrom(registeredType))
            {
                throw invalidDiscriminator(
                    targetType,
                    discriminatorPropertyName,
                    $"Registered type '{registeredType.FullName}' must be a strict subtype of '{targetType.FullName}'.",
                    null);
            }

            if (registeredType.IsAbstract || registeredType.IsInterface)
            {
                throw invalidDiscriminator(
                    targetType,
                    discriminatorPropertyName,
                    $"Registered type '{registeredType.FullName}' must be instantiable for a closed deserializable union.",
                    null);
            }

            if (registeredType.ContainsGenericParameters)
            {
                throw invalidDiscriminator(
                    targetType,
                    discriminatorPropertyName,
                    $"Registered type '{registeredType.FullName}' cannot contain unbound generic parameters.",
                    null);
            }

            if (!seenTypes.Add(registeredType))
            {
                throw invalidDiscriminator(
                    targetType,
                    discriminatorPropertyName,
                    $"Derived type '{registeredType.FullName}' is registered more than once.",
                    null);
            }

            object discriminator = derivedType.TypeDiscriminator
                ?? throw invalidDiscriminator(
                    targetType,
                    discriminatorPropertyName,
                    $"Derived type '{registeredType.FullName}' does not declare a discriminator.",
                    null);
            JsonElement discriminatorValue = SerializeDiscriminator(
                targetType,
                discriminatorPropertyName,
                discriminator);
            if (seenValues.Any(
                value => JsonElementUtility.CompareCanonical(
                    value,
                    discriminatorValue) == 0))
            {
                throw invalidDiscriminator(
                    targetType,
                    discriminatorPropertyName,
                    "Polymorphism contains duplicate discriminator values.",
                    null);
            }

            seenValues.Add(discriminatorValue);
            branches.Add(
                new PolymorphicBranch(
                    registeredType,
                    discriminatorValue,
                    Encoding.UTF8.GetString(
                        JsonElementUtility.GetCanonicalBytes(
                            discriminatorValue))));
        }

        branches.Sort(
            static (left, right) =>
                JsonElementUtility.CompareCanonical(
                    left.DiscriminatorValue,
                    right.DiscriminatorValue));
        return branches;
    }

    private JsonContractNode InjectDiscriminator (
        Type targetType,
        JsonContractNode value,
        string propertyName,
        JsonElement discriminatorValue)
    {
        if (value.Kind != JsonContractNodeKind.Object)
        {
            throw invalidDiscriminator(
                targetType,
                propertyName,
                "A polymorphic derived type must resolve to an object contract.",
                null);
        }

        if (value.Properties.Any(
            property => string.Equals(
                property.Name,
                propertyName,
                StringComparison.Ordinal)))
        {
            throw invalidDiscriminator(
                targetType,
                propertyName,
                "A polymorphic discriminator collides with a declared derived-type property.",
                null);
        }

        JsonContractNode discriminatorNode = composeDiscriminatorNode(
            targetType,
            propertyName,
            discriminatorValue);
        var injectedProperties = new List<JsonContractProperty>(
            value.Properties.Count + 1)
        {
            new(
                propertyName,
                isRequired: true,
                discriminatorNode,
                new JsonContractSource(targetType, member: null)),
        };
        injectedProperties.AddRange(value.Properties);
        return new JsonContractNode(
            value.Kind,
            value.IsNullable,
            value.ScalarKind,
            value.Annotations,
            value.Constraints,
            value.Constant,
            value.AllowedValues,
            value.ReferenceId,
            value.Items,
            value.AdditionalProperties,
            injectedProperties,
            value.Variants,
            value.Discriminator,
            value.Source);
    }

    private JsonElement SerializeDiscriminator (
        Type targetType,
        string propertyName,
        object discriminator)
    {
        JsonElement result;
        try
        {
            result = JsonSerializer.SerializeToElement(
                discriminator,
                discriminator.GetType());
        }
        catch (Exception exception)
        {
            throw invalidDiscriminator(
                targetType,
                propertyName,
                "A polymorphic discriminator could not be serialized.",
                exception);
        }

        if (result.ValueKind is not JsonValueKind.String
            and not JsonValueKind.Number)
        {
            throw invalidDiscriminator(
                targetType,
                propertyName,
                "A polymorphic discriminator must be a string or integer JSON value.",
                null);
        }

        if (result.ValueKind == JsonValueKind.Number
            && (result.GetRawText().IndexOf('.') >= 0
                || result.GetRawText().IndexOf('e') >= 0
                || result.GetRawText().IndexOf('E') >= 0))
        {
            throw invalidDiscriminator(
                targetType,
                propertyName,
                "A numeric polymorphic discriminator must be an integer.",
                null);
        }

        return result;
    }

    private static string GetVariantName (JsonElement discriminatorValue)
    {
        return discriminatorValue.ValueKind == JsonValueKind.String
            ? discriminatorValue.GetString()!
            : discriminatorValue.GetRawText();
    }

    private static bool IsReservedMetadataPropertyName (string propertyName)
    {
        return propertyName is "$id" or "$ref" or "$values";
    }

    private static JsonContractAnnotations EmptyAnnotations ()
    {
        return new JsonContractAnnotations(
            title: null,
            description: null,
            Array.Empty<JsonElement>());
    }

    private sealed class PolymorphicBranch
    {
        internal PolymorphicBranch (
            Type derivedType,
            JsonElement discriminatorValue,
            string canonicalDiscriminatorValue)
        {
            DerivedType = derivedType;
            DiscriminatorValue = discriminatorValue.Clone();
            CanonicalDiscriminatorValue = canonicalDiscriminatorValue;
        }

        internal Type DerivedType { get; }

        internal JsonElement DiscriminatorValue { get; }

        internal string CanonicalDiscriminatorValue { get; }
    }

    private sealed class PolymorphicRegistration
    {
        internal PolymorphicRegistration (
            string discriminatorPropertyName,
            IReadOnlyList<PolymorphicBranch> branches)
        {
            DiscriminatorPropertyName = discriminatorPropertyName;
            Branches = branches;
        }

        internal string DiscriminatorPropertyName { get; }

        internal IReadOnlyList<PolymorphicBranch> Branches { get; }
    }
}
