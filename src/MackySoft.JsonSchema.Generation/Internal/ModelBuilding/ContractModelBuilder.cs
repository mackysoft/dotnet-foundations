using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using MackySoft.JsonSchema.Generation.Configuration;
using MackySoft.JsonSchema.Generation.ContractModel;
using MackySoft.JsonSchema.Generation.Diagnostics;
using MackySoft.JsonSchema.Generation.Extensibility;
using MackySoft.JsonSchema.Generation.Internal.Metadata;
using MackySoft.JsonSchema.Generation.Internal.Metadata.Contracts;
using MackySoft.JsonSchema.Generation.Internal.ModelBuilding.Definitions;
using MackySoft.JsonSchema.Generation.Internal.ModelBuilding.SerializerMetadata;
using MackySoft.JsonSchema.Generation.Internal.ModelBuilding.Shapes;
using MackySoft.JsonSchema.Generation.Internal.ModelBuilding.TypeMappings;
using MackySoft.JsonSchema.Generation.Internal.ModelBuilding.TypeSystem;
using MackySoft.JsonSchema.Generation.Internal.ModelBuilding.Validation;
using MackySoft.JsonSchema.Generation.Internal.ModelBuilding.Variants;
using MackySoft.JsonSchema.Generation.Metadata;

namespace MackySoft.JsonSchema.Generation.Internal.ModelBuilding;

/// <summary>
/// Owns the only traversal from authoritative <see cref="JsonTypeInfo"/> contracts
/// into the serializer-independent Contract Model.
/// </summary>
internal sealed class ContractModelBuilder
{
    private readonly string contractId;
    private readonly JsonSerializerOptions serializerOptions;
    private readonly JsonContractGenerationSettings settings;
    private readonly ContractMetadataResolver metadataResolver;
    private readonly BuiltInScalarContractResolver builtInScalarResolver;
    private readonly SerializedObjectContractResolver serializedObjectResolver;
    private readonly SerializerFiniteValueValidator serializerValueValidator;
    private readonly TypeMappingResolver typeMappingResolver;
    private readonly MappedContractShapeResolver mappedShapeResolver;
    private readonly ContractDefinitionRegistry definitionRegistry = new();
    private readonly JsonContractValueValidator valueValidator = new();
    private readonly SystemTextJsonPolymorphismResolver polymorphismResolver;
    private readonly Dictionary<Type, ResolvedContractMetadata> typeMetadataCache =
        new();
    private readonly Dictionary<Type, JsonTypeInfo> typeInfoCache = new();

    internal ContractModelBuilder (
        string contractId,
        JsonSerializerOptions serializerOptions,
        JsonContractGenerationSettings settings,
        IReadOnlyList<IJsonContractMetadataProvider> metadataProviders,
        IReadOnlyList<IJsonContractTypeMapper> typeMappers)
    {
        this.contractId = contractId
            ?? throw new ArgumentNullException(nameof(contractId));
        this.serializerOptions = serializerOptions
            ?? throw new ArgumentNullException(nameof(serializerOptions));
        this.serializerOptions.MakeReadOnly();
        this.settings = settings
            ?? throw new ArgumentNullException(nameof(settings));
        metadataResolver = new ContractMetadataResolver(
            metadataProviders
                ?? throw new ArgumentNullException(nameof(metadataProviders)));
        builtInScalarResolver = new BuiltInScalarContractResolver(
            contractId,
            serializerOptions);
        serializedObjectResolver = new SerializedObjectContractResolver(
            contractId,
            serializerOptions,
            settings.ObjectClosure
                == JsonObjectClosure.AllowAdditionalProperties);
        serializerValueValidator = new SerializerFiniteValueValidator(
            serializerOptions);
        typeMappingResolver = new TypeMappingResolver(
            contractId,
            serializerOptions,
            typeMappers
                ?? throw new ArgumentNullException(nameof(typeMappers)));
        mappedShapeResolver = new MappedContractShapeResolver(contractId);
        polymorphismResolver = new SystemTextJsonPolymorphismResolver(
            RegisterPolymorphicDefinitionReference,
            ComposePolymorphicDiscriminatorNode,
            InvalidPolymorphicDiscriminator);
    }

    internal ContractModelStructure Build (Type contractType)
    {
        if (contractType is null)
        {
            throw new ArgumentNullException(nameof(contractType));
        }

        serializedObjectResolver.ValidateGlobalContract(contractType);
        if (serializerOptions.ReferenceHandler is not null)
        {
            throw UnsupportedTypeInfo(
                contractType,
                jsonPropertyName: null,
                "ReferenceHandler changes the JSON wire shape and is not represented by this contract model.");
        }

        JsonContractNode root = BuildNode(
            contractType,
            ContractNullability.Root(contractType),
            member: null,
            jsonPropertyName: null,
            propertyConverter: null,
            propertyNumberHandling: null,
            allowObjectReference: false);

        while (definitionRegistry.TryDequeuePending(
            out DefinitionRegistration? registration))
        {
            if (registration is null)
            {
                throw new InvalidOperationException(
                    "The definition registry returned an empty pending registration.");
            }

            JsonContractNode value = BuildNode(
                registration.Key.Type,
                ContractNullability.Root(registration.Key.Type),
                member: null,
                jsonPropertyName: null,
                propertyConverter: null,
                propertyNumberHandling: null,
                allowObjectReference: false);
            value = polymorphismResolver.ApplyDefinitionDiscriminator(
                registration.Key.Type,
                value,
                registration.Key.DiscriminatorPropertyName,
                registration.DiscriminatorValue);

            registration.Complete(value);
        }

        valueValidator.ValidateAll(definitionRegistry.ResolveCompleted);

        return new ContractModelStructure(
            root,
            definitionRegistry.GetCompletedDefinitions());
    }

    private JsonContractNode BuildNode (
        Type declaredType,
        ContractNullability nullability,
        MemberInfo? member,
        string? jsonPropertyName,
        JsonConverter? propertyConverter,
        JsonNumberHandling? propertyNumberHandling,
        bool allowObjectReference,
        ResolvedContractMetadata? resolvedMemberMetadata = null)
    {
        Type targetType = Nullable.GetUnderlyingType(declaredType) ?? declaredType;
        ResolvedContractMetadata typeMetadata = ResolveTypeMetadata(targetType);
        ResolvedContractMetadata? memberMetadata = member is null
            ? null
            : resolvedMemberMetadata
                ?? metadataResolver.ResolveMember(
                    contractId,
                    targetType,
                    member,
                    jsonPropertyName
                        ?? throw new InvalidOperationException(
                            "A serialized member must have a JSON property name."));

        ResolvedContractMetadata effectiveMetadata = memberMetadata is null
            ? typeMetadata
            : ContractMetadataResolver.Merge(
                typeMetadata,
                memberMetadata,
                contractId,
                targetType,
                jsonPropertyName);
        JsonTypeInfo typeInfo = ResolveTypeInfo(targetType, jsonPropertyName);
        ResolvedTypeMapping? mapping = typeMappingResolver.Resolve(
            targetType,
            typeInfo,
            member,
            jsonPropertyName,
            propertyConverter);
        TypeMappingAuthorityValidator.Validate(
            contractId,
            targetType,
            typeInfo,
            propertyConverter,
            jsonPropertyName,
            mapping);
        if (effectiveMetadata.IsArbitrary)
        {
            return BuildArbitraryNode(
                targetType,
                member,
                jsonPropertyName,
                propertyConverter,
                typeInfo,
                mapping,
                effectiveMetadata);
        }

        bool isNullable = ResolveNullability(
            declaredType,
            targetType,
            jsonPropertyName,
            nullability,
            effectiveMetadata);

        if (allowObjectReference
            && mapping is null
            && propertyConverter is null
            && BuiltInScalarContractResolver.IsSystemTextJsonConverter(
                typeInfo.Converter)
            && typeInfo.Kind == JsonTypeInfoKind.Object
            && typeInfo.PolymorphismOptions is null)
        {
            ValidateFiniteSerializerValues(
                targetType,
                jsonPropertyName,
                propertyConverter,
                effectiveMetadata);
            ContractNodeComposer.ValidateMetadataCompatibility(
                contractId,
                targetType,
                jsonPropertyName,
                effectiveMetadata);
            DefinitionRegistration definition = definitionRegistry.GetOrAdd(
                new DefinitionKey(targetType, null, null),
                discriminatorValue: null);
            return ContractNodeComposer.Compose(
                contractId,
                targetType,
                jsonPropertyName,
                new ContractNodeShape(
                    JsonContractNodeKind.Reference,
                    referenceId: definition.Id),
                valueValidator,
                memberMetadata,
                isNullable,
                new JsonContractSource(targetType, member));
        }

        ContractNodeShape shape = mapping is null
            ? BuildSerializerShape(
                targetType,
                typeInfo,
                nullability,
                member,
                jsonPropertyName,
                propertyConverter,
                propertyNumberHandling)
            : mappedShapeResolver.Resolve(
                targetType,
                mapping,
                jsonPropertyName,
                BuildMappedSurrogate);
        if (mapping is null
            || BuiltInScalarContractResolver.IsSystemTextJsonConverter(
                propertyConverter ?? typeInfo.Converter))
        {
            ValidateFiniteSerializerValues(
                targetType,
                jsonPropertyName,
                propertyConverter,
                effectiveMetadata);
        }

        return ContractNodeComposer.Compose(
            contractId,
            targetType,
            jsonPropertyName,
            shape,
            valueValidator,
            effectiveMetadata,
            isNullable,
            new JsonContractSource(targetType, member));
    }

    private JsonContractNode BuildArbitraryNode (
        Type targetType,
        MemberInfo? member,
        string? jsonPropertyName,
        JsonConverter? propertyConverter,
        JsonTypeInfo typeInfo,
        ResolvedTypeMapping? mapping,
        ResolvedContractMetadata metadata)
    {
        JsonConverter effectiveConverter =
            propertyConverter ?? typeInfo.Converter;
        bool hasUnknownConverter =
            !BuiltInScalarContractResolver.IsSystemTextJsonConverter(
                effectiveConverter);
        bool hasArbitraryRepresentation =
            IsArbitraryJsonType(targetType)
            || hasUnknownConverter
            || mapping?.Mapping.Kind
                == JsonContractTypeMappingKind.Arbitrary;
        if (!hasArbitraryRepresentation
            || (mapping is not null
                && mapping.Mapping.Kind
                    != JsonContractTypeMappingKind.Arbitrary))
        {
            throw ConflictingSerializerMetadata(
                targetType,
                jsonPropertyName,
                JsonContractMetadataKind.Arbitrary,
                metadata,
                "An arbitrary JSON declaration conflicts with an interpreted serializer or type-mapper contract.");
        }

        ContractNodeShape shape = mapping is null
            ? new ContractNodeShape(JsonContractNodeKind.Arbitrary)
            : mappedShapeResolver.Resolve(
                targetType,
                mapping,
                jsonPropertyName,
                BuildMappedSurrogate);
        return ContractNodeComposer.Compose(
            contractId,
            targetType,
            jsonPropertyName,
            shape,
            valueValidator,
            metadata,
            isNullable: true,
            new JsonContractSource(targetType, member));
    }

    private void ValidateFiniteSerializerValues (
        Type targetType,
        string? jsonPropertyName,
        JsonConverter? propertyConverter,
        ResolvedContractMetadata metadata)
    {
        if (!SerializerFiniteValueValidator.Supports(targetType))
        {
            return;
        }

        foreach (ResolvedContractMetadata.MetadataProvenance declaration
            in metadata.MetadataDeclarations)
        {
            JsonContractMetadataKind metadataKind =
                declaration.Metadata.Kind;
            if (metadataKind is not (
                    JsonContractMetadataKind.Const
                    or JsonContractMetadataKind.EnumValue))
            {
                continue;
            }

            ValidateFiniteSerializerValue(
                targetType,
                jsonPropertyName,
                propertyConverter,
                declaration.Metadata.JsonValue!.Value,
                metadataKind);
        }
    }

    private void ValidateFiniteSerializerValue (
        Type targetType,
        string? jsonPropertyName,
        JsonConverter? propertyConverter,
        JsonElement value,
        JsonContractMetadataKind metadataKind)
    {
        if (serializerValueValidator.IsRoundTripStable(
            targetType,
            propertyConverter,
            value))
        {
            return;
        }

        throw ContractMetadataFailure.Invalid(
            contractId,
            targetType,
            jsonPropertyName,
            metadataKind,
            "A declared JSON value is not accepted and emitted unchanged by the authoritative serializer contract.");
    }

    private ContractNodeShape BuildSerializerShape (
        Type targetType,
        JsonTypeInfo typeInfo,
        ContractNullability nullability,
        MemberInfo? member,
        string? jsonPropertyName,
        JsonConverter? propertyConverter,
        JsonNumberHandling? propertyNumberHandling)
    {
        if (IsArbitraryJsonType(targetType))
        {
            return new ContractNodeShape(JsonContractNodeKind.Arbitrary);
        }

        if (targetType.IsEnum)
        {
            bool isVocabulary =
                VocabularyContractReader.IsVocabulary(targetType);
            if (isVocabulary)
            {
                throw UnsupportedConverter(
                    targetType,
                    jsonPropertyName,
                    "A text vocabulary converter requires an explicitly registered type mapper.");
            }

            return builtInScalarResolver.ResolveNumericEnum(
                targetType,
                typeInfo,
                propertyConverter,
                propertyNumberHandling,
                jsonPropertyName);
        }

        if (builtInScalarResolver.TryResolve(
            targetType,
            typeInfo,
            propertyConverter,
            propertyNumberHandling,
            jsonPropertyName,
            out ContractNodeShape? builtInScalar))
        {
            return builtInScalar
                ?? throw new InvalidOperationException(
                    "The built-in scalar resolver returned no shape.");
        }

        if (BuiltInScalarContractResolver.RequiresExplicitTypeMapping(
            targetType))
        {
            throw UnsupportedTypeInfo(
                targetType,
                jsonPropertyName,
                "The built-in lexical serializer contract has no exact JSON Schema projection and requires an explicit type mapper.");
        }

        if (!BuiltInScalarContractResolver.IsSystemTextJsonConverter(
            typeInfo.Converter)
            || (propertyConverter is not null
                && !BuiltInScalarContractResolver.IsSystemTextJsonConverter(
                    propertyConverter)))
        {
            throw UnsupportedConverter(
                targetType,
                jsonPropertyName,
                "The configured converter has no built-in interpretation or registered type mapper.");
        }

        return typeInfo.Kind switch
        {
            JsonTypeInfoKind.Object => BuildObjectShape(
                targetType,
                typeInfo),
            JsonTypeInfoKind.Enumerable => BuildArrayShape(
                targetType,
                nullability),
            JsonTypeInfoKind.Dictionary => BuildDictionaryShape(
                targetType,
                nullability),
            _ => throw UnsupportedTypeInfo(
                targetType,
                jsonPropertyName,
                $"JsonTypeInfo kind '{typeInfo.Kind}' does not expose a supported deterministic structure."),
        };
    }

    private ContractNodeShape BuildObjectShape (
        Type targetType,
        JsonTypeInfo typeInfo)
    {
        if (typeInfo.PolymorphismOptions is not null)
        {
            return polymorphismResolver.ResolveShape(targetType);
        }

        if (targetType.IsAbstract || targetType.IsInterface)
        {
            throw UnsupportedTypeInfo(
                targetType,
                jsonPropertyName: null,
                "An abstract or interface contract requires a finite System.Text.Json polymorphism registration.");
        }

        var properties = new List<JsonContractProperty>();
        JsonContractNode? extensionDataValue = null;
        IReadOnlyList<SerializedObjectProperty> serializedProperties =
            serializedObjectResolver.ResolveProperties(targetType, typeInfo);
        foreach (SerializedObjectProperty serializedProperty
            in serializedProperties)
        {
            JsonPropertyInfo propertyInfo = serializedProperty.PropertyInfo;
            MemberInfo member = serializedProperty.Member;

            if (propertyInfo.IsExtensionData)
            {
                if (extensionDataValue is not null)
                {
                    throw UnsupportedTypeInfo(
                        targetType,
                        propertyInfo.Name,
                        "JsonTypeInfo contains more than one extension-data property.");
                }

                extensionDataValue = BuildExtensionDataValue(
                    propertyInfo,
                    member);
                continue;
            }

            ResolvedContractMetadata memberMetadata =
                metadataResolver.ResolveMember(
                    contractId,
                    Nullable.GetUnderlyingType(propertyInfo.PropertyType)
                        ?? propertyInfo.PropertyType,
                    member,
                    propertyInfo.Name);
            ValidateRequiredMetadata(
                propertyInfo,
                memberMetadata);
            bool isRequired = propertyInfo.IsRequired;
            JsonContractNode propertyValue = BuildNode(
                propertyInfo.PropertyType,
                ContractNullability.ForMember(
                    member,
                    propertyInfo.PropertyType),
                member,
                propertyInfo.Name,
                propertyInfo.CustomConverter,
                propertyInfo.NumberHandling,
                allowObjectReference: true,
                resolvedMemberMetadata: memberMetadata);
            properties.Add(
                new JsonContractProperty(
                    propertyInfo.Name,
                    isRequired,
                    propertyValue,
                    new JsonContractSource(
                        Nullable.GetUnderlyingType(propertyInfo.PropertyType)
                            ?? propertyInfo.PropertyType,
                        member)));
        }

        serializedObjectResolver.ValidateObjectClosure(
            targetType,
            typeInfo,
            extensionDataValue is not null);
        JsonContractNode? additionalProperties = extensionDataValue
            ?? (settings.ObjectClosure == JsonObjectClosure.AllowAdditionalProperties
                ? CreateArbitraryNode(targetType)
                : null);
        return new ContractNodeShape(
            JsonContractNodeKind.Object,
            additionalProperties: additionalProperties,
            properties: properties);
    }

    private ContractNodeShape BuildArrayShape (
        Type targetType,
        ContractNullability nullability)
    {
        if (!ClrCollectionShapeResolver.TryGetEnumerableElementType(
            targetType,
            out Type? elementType,
            out int genericArgumentIndex)
            || elementType is null)
        {
            throw UnsupportedTypeInfo(
                targetType,
                jsonPropertyName: null,
                "Enumerable JsonTypeInfo does not expose one unambiguous element type.");
        }

        JsonContractNode items = BuildNode(
            elementType,
            nullability.Child(elementType, genericArgumentIndex),
            member: null,
            jsonPropertyName: null,
            propertyConverter: null,
            propertyNumberHandling: null,
            allowObjectReference: true);
        return new ContractNodeShape(
            JsonContractNodeKind.Array,
            items: items);
    }

    private ContractNodeShape BuildDictionaryShape (
        Type targetType,
        ContractNullability nullability)
    {
        if (!ClrCollectionShapeResolver.TryGetDictionaryValueType(
            targetType,
            out Type? valueType,
            out int genericArgumentIndex)
            || valueType is null)
        {
            string reason = ClrCollectionShapeResolver
                .ImplementsNonGenericDictionary(targetType)
                    ? "Non-generic or non-string-key dictionaries require an explicit type mapper."
                    : "Dictionary JsonTypeInfo does not expose one string-key value type.";
            throw UnsupportedTypeInfo(
                targetType,
                jsonPropertyName: null,
                reason);
        }

        JsonContractNode values = BuildNode(
            valueType,
            nullability.Child(valueType, genericArgumentIndex),
            member: null,
            jsonPropertyName: null,
            propertyConverter: null,
            propertyNumberHandling: null,
            allowObjectReference: true);
        return new ContractNodeShape(
            JsonContractNodeKind.Dictionary,
            additionalProperties: values);
    }

    private JsonContractNode BuildExtensionDataValue (
        JsonPropertyInfo propertyInfo,
        MemberInfo member)
    {
        Type propertyType = propertyInfo.PropertyType;
        if (propertyType == typeof(JsonObject)
            || propertyType == typeof(JsonElement)
            || propertyType == typeof(object))
        {
            return CreateArbitraryNode(propertyType);
        }

        if (!ClrCollectionShapeResolver.TryGetDictionaryValueType(
            propertyType,
            out Type? valueType,
            out int genericArgumentIndex)
            || valueType is null)
        {
            throw UnsupportedTypeInfo(
                propertyType,
                propertyInfo.Name,
                "Extension data must expose a string-key dictionary value contract or an arbitrary JSON object.");
        }

        return BuildNode(
            valueType,
            ContractNullability
                .ForMember(member, propertyType)
                .Child(valueType, genericArgumentIndex),
            member: null,
            jsonPropertyName: null,
            propertyConverter: null,
            propertyNumberHandling: null,
            allowObjectReference: true);
    }

    private JsonTypeInfo ResolveTypeInfo (
        Type targetType,
        string? jsonPropertyName)
    {
        if (typeInfoCache.TryGetValue(targetType, out JsonTypeInfo? cached))
        {
            return cached;
        }

        try
        {
            IJsonTypeInfoResolver resolver =
                serializerOptions.TypeInfoResolver
                ?? throw new InvalidOperationException(
                    "Serializer options do not expose a type-info resolver.");
            JsonTypeInfo typeInfo = resolver.GetTypeInfo(
                targetType,
                serializerOptions)
                ?? throw new NotSupportedException(
                    $"The configured resolver did not provide type information for '{targetType.FullName}'.");
            if (typeInfo.Type != targetType)
            {
                throw new InvalidOperationException(
                    $"Resolver returned type information for '{typeInfo.Type.FullName}'.");
            }

            if (typeInfo.PolymorphismOptions
                is JsonPolymorphismOptions polymorphism)
            {
                // STJ configures polymorphism while making JsonTypeInfo
                // read-only. Validate first so registration failures retain
                // deterministic contract diagnostics.
                polymorphismResolver.ValidateRegistration(
                    targetType,
                    polymorphism);
            }

            typeInfo.MakeReadOnly();
            typeInfoCache.Add(targetType, typeInfo);
            return typeInfo;
        }
        catch (JsonContractGenerationException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new JsonContractGenerationException(
                JsonContractGenerationFailureKind.UnsupportedTypeInfo,
                $"System.Text.Json type information for '{targetType.FullName}' could not be resolved.",
                contractId,
                targetType,
                jsonPropertyName,
                innerException: exception);
        }
    }

    private ResolvedContractMetadata ResolveTypeMetadata (Type targetType)
    {
        if (typeMetadataCache.TryGetValue(
            targetType,
            out ResolvedContractMetadata? cached))
        {
            return cached;
        }

        ResolvedContractMetadata resolved = metadataResolver.ResolveType(
            contractId,
            targetType);
        typeMetadataCache.Add(targetType, resolved);
        return resolved;
    }

    private bool ResolveNullability (
        Type declaredType,
        Type targetType,
        string? jsonPropertyName,
        ContractNullability nullability,
        ResolvedContractMetadata effectiveMetadata)
    {
        NullableContractState state = nullability.ResolveState(declaredType);
        if (effectiveMetadata.AllowsNull == true)
        {
            // CLR type declarations have no use-site nullable context, while
            // System.Text.Json can serialize a null reference-type root.
            bool isNullableReferenceRoot =
                nullability.IsRoot && !declaredType.IsValueType;
            if (state == NullableContractState.NotNullable
                && !isNullableReferenceRoot)
            {
                throw ConflictingSerializerMetadata(
                    targetType,
                    jsonPropertyName,
                    JsonContractMetadataKind.AllowNull,
                    effectiveMetadata,
                    "The declared null acceptance conflicts with CLR nullable metadata.");
            }

            return true;
        }

        return state switch
        {
            NullableContractState.Nullable => true,
            NullableContractState.NotNullable => false,
            _ => throw UnsupportedTypeInfo(
                targetType,
                jsonPropertyName,
                "Nullable reference metadata is unavailable. Declare null acceptance explicitly or provide authoritative member metadata."),
        };
    }

    private void ValidateRequiredMetadata (
        JsonPropertyInfo propertyInfo,
        ResolvedContractMetadata memberMetadata)
    {
        if (memberMetadata.IsRequired == true && !propertyInfo.IsRequired)
        {
            throw ConflictingSerializerMetadata(
                Nullable.GetUnderlyingType(propertyInfo.PropertyType)
                    ?? propertyInfo.PropertyType,
                propertyInfo.Name,
                JsonContractMetadataKind.Required,
                memberMetadata,
                "The declared required property conflicts with System.Text.Json requiredness.");
        }
    }

    private JsonContractGenerationException ConflictingSerializerMetadata (
        Type targetType,
        string? jsonPropertyName,
        JsonContractMetadataKind metadataKind,
        ResolvedContractMetadata metadata,
        string message)
    {
        IReadOnlyList<string> sourceIds = MetadataFailure.SortSourceIds(
            metadata.MetadataDeclarations
                .Where(
                    declaration =>
                        declaration.Metadata.Kind == metadataKind)
                .Select(static declaration => declaration.SourceId));
        return new JsonContractGenerationException(
            JsonContractGenerationFailureKind.ConflictingMetadata,
            message,
            contractId,
            targetType,
            jsonPropertyName,
            metadataKind,
            sourceIds);
    }

    private JsonContractNode BuildMappedSurrogate (
        Type surrogateType,
        string? jsonPropertyName)
    {
        return BuildNode(
            surrogateType,
            ContractNullability.Root(surrogateType),
            member: null,
            jsonPropertyName,
            propertyConverter: null,
            propertyNumberHandling: null,
            allowObjectReference: true);
    }

    private JsonContractNode RegisterPolymorphicDefinitionReference (
        Type derivedType,
        string discriminatorPropertyName,
        string canonicalDiscriminatorValue,
        JsonElement discriminatorValue)
    {
        DefinitionRegistration definition = definitionRegistry.GetOrAdd(
            new DefinitionKey(
                derivedType,
                discriminatorPropertyName,
                canonicalDiscriminatorValue),
            discriminatorValue);
        return ContractNodeComposer.Compose(
            contractId,
            derivedType,
            discriminatorPropertyName,
            new ContractNodeShape(
                JsonContractNodeKind.Reference,
                referenceId: definition.Id),
            valueValidator,
            metadata: null,
            isNullable: false,
            new JsonContractSource(derivedType, member: null));
    }

    private JsonContractNode ComposePolymorphicDiscriminatorNode (
        Type targetType,
        string propertyName,
        JsonElement discriminatorValue)
    {
        return ContractNodeComposer.Compose(
            contractId,
            discriminatorValue.ValueKind == JsonValueKind.String
                ? typeof(string)
                : typeof(int),
            propertyName,
            new ContractNodeShape(
                JsonContractNodeKind.Const,
                scalarKind:
                    discriminatorValue.ValueKind == JsonValueKind.String
                        ? JsonContractScalarKind.String
                        : JsonContractScalarKind.Integer,
                constant: discriminatorValue),
            valueValidator,
            metadata: null,
            isNullable: false,
            new JsonContractSource(targetType, member: null));
    }

    private Exception InvalidPolymorphicDiscriminator (
        Type targetType,
        string propertyName,
        string message,
        Exception? innerException)
    {
        return new JsonContractGenerationException(
            JsonContractGenerationFailureKind.UnsupportedTypeInfo,
            message,
            contractId,
            targetType: targetType,
            jsonPropertyName: propertyName,
            metadataKind: JsonContractMetadataKind.Discriminator,
            innerException: innerException);
    }

    private static bool IsArbitraryJsonType (Type type)
    {
        return type == typeof(object)
            || type == typeof(JsonElement)
            || type == typeof(JsonDocument)
            || typeof(JsonNode).IsAssignableFrom(type);
    }

    private static JsonContractNode CreateArbitraryNode (Type sourceType)
    {
        return new JsonContractNode(
            JsonContractNodeKind.Arbitrary,
            isNullable: true,
            scalarKind: null,
            EmptyAnnotations(),
            EmptyConstraints(),
            constant: null,
            Array.Empty<JsonElement>(),
            referenceId: null,
            items: null,
            additionalProperties: null,
            Array.Empty<JsonContractProperty>(),
            Array.Empty<JsonContractVariant>(),
            discriminator: null,
            new JsonContractSource(sourceType, member: null));
    }

    private static JsonContractAnnotations EmptyAnnotations ()
    {
        return new JsonContractAnnotations(
            title: null,
            description: null,
            Array.Empty<JsonElement>());
    }

    private static JsonContractConstraints EmptyConstraints ()
    {
        return new JsonContractConstraints(
            minimum: null,
            exclusiveMinimum: null,
            maximum: null,
            exclusiveMaximum: null,
            minimumLength: null,
            maximumLength: null,
            minimumItems: null,
            maximumItems: null,
            minimumProperties: null,
            maximumProperties: null,
            pattern: null,
            format: null);
    }

    private JsonContractGenerationException UnsupportedConverter (
        Type targetType,
        string? jsonPropertyName,
        string message)
    {
        return new JsonContractGenerationException(
            JsonContractGenerationFailureKind.UnsupportedConverter,
            message,
            contractId,
            targetType,
            jsonPropertyName);
    }

    private JsonContractGenerationException UnsupportedTypeInfo (
        Type targetType,
        string? jsonPropertyName,
        string message)
    {
        return new JsonContractGenerationException(
            JsonContractGenerationFailureKind.UnsupportedTypeInfo,
            message,
            contractId,
            targetType,
            jsonPropertyName);
    }

}
