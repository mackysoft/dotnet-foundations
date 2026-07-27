using System.Reflection;
using System.Text.Json;
using MackySoft.JsonSchema.Generation.Extensibility;
using MackySoft.JsonSchema.Generation.Metadata;

namespace MackySoft.JsonSchema.Generation.Tests.Annotations;

public sealed class PublicAnnotationMetadataApiTests
{
    [Fact]
    [Trait("Size", "Small")]
    public void ExportedTypes_ExposeOnlyTheTypedAnnotationAndMetadataContracts ()
    {
        Assembly assembly = typeof(JsonContractGenerator).Assembly;

        Assert.Equal(
            new[]
            {
                "MackySoft.JsonSchema.Generation.Annotations.DescriptionAttribute",
                "MackySoft.JsonSchema.Generation.Annotations.ItemCountAttribute",
                "MackySoft.JsonSchema.Generation.Annotations.LengthAttribute",
                "MackySoft.JsonSchema.Generation.Annotations.PatternAttribute",
                "MackySoft.JsonSchema.Generation.Annotations.PropertyCountAttribute",
                "MackySoft.JsonSchema.Generation.Annotations.TitleAttribute",
            },
            assembly
                .GetExportedTypes()
                .Where(static type =>
                    typeof(Attribute).IsAssignableFrom(type))
                .Select(static type => type.FullName!)
                .OrderBy(static name => name, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal(
            new[]
            {
                "MackySoft.JsonSchema.Generation.Metadata.JsonContractMetadataBuilder`1",
                "MackySoft.JsonSchema.Generation.Metadata.JsonContractMetadataContext`1",
                "MackySoft.JsonSchema.Generation.Metadata.JsonContractNumber",
            },
            GetExportedTypeNames(
                assembly,
                "MackySoft.JsonSchema.Generation.Metadata"));
        Assert.Equal(
            new[]
            {
                "MackySoft.JsonSchema.Generation.Extensibility.IJsonContractAttributeInterpreter`2",
                "MackySoft.JsonSchema.Generation.Extensibility.IJsonContractMetadataProvider`1",
                "MackySoft.JsonSchema.Generation.Extensibility.JsonContractMetadataRegistry",
            },
            assembly
                .GetExportedTypes()
                .Where(static type =>
                    type == typeof(JsonContractMetadataRegistry)
                    || type.Name.StartsWith(
                        "IJsonContractAttributeInterpreter",
                        StringComparison.Ordinal)
                    || type.Name.StartsWith(
                        "IJsonContractMetadataProvider",
                        StringComparison.Ordinal))
                .Select(static type => type.FullName!)
                .OrderBy(static name => name, StringComparer.Ordinal)
                .ToArray());

        Assert.Null(
            assembly.GetType(
                "MackySoft.JsonSchema.Generation.Extensibility.IJsonContractMetadataProvider"));
        Assert.Null(
            assembly.GetType(
                "MackySoft.JsonSchema.Generation.Metadata.JsonContractMetadataBuilder"));
        Assert.Null(
            assembly.GetType(
                "MackySoft.JsonSchema.Generation.Metadata.JsonContractMetadataContext"));
        foreach (string removedTypeName in RemovedWeakTypeNames)
        {
            Assert.Null(assembly.GetType(removedTypeName));
        }
    }

    [Fact]
    [Trait("Size", "Small")]
    public void TypedMetadataAndRequestTypes_HaveTheCompleteCleanBreakSurface ()
    {
        Assert.Equal(
            new[]
            {
                "ProvideMetadata(JsonContractMetadataContext<TValue>,JsonContractMetadataBuilder<TValue>):Void",
            },
            GetPublicSurface(
                typeof(IJsonContractMetadataProvider<>)));
        Assert.Equal(
            new[]
            {
                "InterpretAttribute(TAttribute,JsonContractMetadataContext<TValue>,JsonContractMetadataBuilder<TValue>):Void",
            },
            GetPublicSurface(
                typeof(IJsonContractAttributeInterpreter<,>)));
        Assert.Equal(
            new[]
            {
                "DeclaringTypeInfo:JsonTypeInfo",
                "PropertyInfo:JsonPropertyInfo",
                "TypeInfo:JsonTypeInfo<TValue>",
            },
            GetPublicSurface(
                typeof(JsonContractMetadataContext<>)));
        Assert.Equal(
            new[]
            {
                "AddExample(TValue):Void",
                "SetConst(TValue):Void",
                "SetDescription(String):Void",
                "SetExclusiveMaximum(JsonContractNumber):Void",
                "SetExclusiveMinimum(JsonContractNumber):Void",
                "SetMaximum(JsonContractNumber):Void",
                "SetMaximumItemCount(Int32):Void",
                "SetMaximumLength(Int32):Void",
                "SetMaximumPropertyCount(Int32):Void",
                "SetMinimum(JsonContractNumber):Void",
                "SetMinimumItemCount(Int32):Void",
                "SetMinimumLength(Int32):Void",
                "SetMinimumPropertyCount(Int32):Void",
                "SetPattern(String):Void",
                "SetTitle(String):Void",
            },
            GetPublicSurface(
                typeof(JsonContractMetadataBuilder<>)));
        Assert.Equal(
            new[]
            {
                "FromBigInteger(BigInteger):JsonContractNumber",
                "FromDecimal(Decimal):JsonContractNumber",
                "FromInt64(Int64):JsonContractNumber",
                "FromUInt64(UInt64):JsonContractNumber",
                "Parse(String):JsonContractNumber",
                "Token:String",
            },
            GetPublicSurface(typeof(JsonContractNumber)));
        Assert.Equal(
            new[]
            {
                "JsonContractMetadataRegistry()",
                "RegisterAttributeInterpreter<TAttribute,TValue>(IJsonContractAttributeInterpreter<TAttribute,TValue>):JsonContractMetadataRegistry",
                "RegisterProvider<TValue>(IJsonContractMetadataProvider<TValue>):JsonContractMetadataRegistry",
            },
            GetPublicSurface(
                typeof(JsonContractMetadataRegistry)));
        Assert.Equal(
            new[]
            {
                "ContractId:String",
                "DocumentOptions:JsonSchemaDocumentOptions",
                "JsonContractGenerationRequest(String,JsonTypeInfo,JsonSchemaDocumentOptions)",
                "TypeInfo:JsonTypeInfo",
            },
            GetPublicSurface(
                typeof(JsonContractGenerationRequest)));
        Assert.Equal(
            new[]
            {
                "DeclaringTypeInfo:JsonTypeInfo",
                "PropertyInfo:JsonPropertyInfo",
                "TypeInfo:JsonTypeInfo",
            },
            GetPublicSurface(
                typeof(JsonContractTypeMapperContext)));
        Assert.Equal(
            new[]
            {
                "CanMap(JsonContractTypeMapperContext):Boolean",
                "Map(JsonContractTypeMapperContext):JsonContractTypeMapping",
            },
            GetPublicSurface(
                typeof(IJsonContractTypeMapper)));

        Assert.True(
            typeof(IJsonContractMetadataProvider<>)
                .IsGenericTypeDefinition);
        Assert.True(
            typeof(IJsonContractAttributeInterpreter<,>)
                .IsGenericTypeDefinition);
        Assert.Contains(
            typeof(IJsonContractMetadataProvider<>).GetInterfaces(),
            static contract =>
                contract == typeof(IJsonContractExtension));
        Assert.Contains(
            typeof(IJsonContractAttributeInterpreter<,>).GetInterfaces(),
            static contract =>
                contract == typeof(IJsonContractExtension));
    }

    [Fact]
    [Trait("Size", "Small")]
    public void TypedMetadataAndRequestTypes_ExposeNoWeakInputsOrCompatibilityShims ()
    {
        Type[] publicTypes =
        {
            typeof(IJsonContractMetadataProvider<>),
            typeof(IJsonContractAttributeInterpreter<,>),
            typeof(JsonContractMetadataRegistry),
            typeof(JsonContractMetadataContext<>),
            typeof(JsonContractMetadataBuilder<>),
            typeof(JsonContractNumber),
            typeof(JsonContractGenerationRequest),
            typeof(IJsonContractTypeMapper),
            typeof(JsonContractTypeMapperContext),
            typeof(JsonContractTypeMapping),
        };
        MemberInfo[] members = publicTypes
            .SelectMany(GetDeclaredPublicMembers)
            .ToArray();

        Assert.DoesNotContain(
            publicTypes.Cast<MemberInfo>().Concat(members),
            static member =>
                member.GetCustomAttribute<ObsoleteAttribute>() is not null);
        Assert.DoesNotContain(
            members.OfType<MethodBase>().SelectMany(
                static method => method.GetParameters()),
            static parameter =>
                parameter.ParameterType == typeof(JsonElement)
                || parameter.ParameterType == typeof(JsonDocument)
                || parameter.ParameterType == typeof(double)
                || parameter.ParameterType == typeof(float)
                || parameter.ParameterType.IsArray
                || parameter.Name?.Contains(
                    "propertyName",
                    StringComparison.OrdinalIgnoreCase) == true
                || parameter.Name?.Contains(
                    "requiredProperty",
                    StringComparison.OrdinalIgnoreCase) == true);
        Assert.Null(
            typeof(JsonContractTypeMapping).GetProperty(
                "AllowedValues",
                BindingFlags.Public
                    | BindingFlags.Instance
                    | BindingFlags.Static));
        Assert.DoesNotContain(
            typeof(JsonContractTypeMapping).GetMethods(
                BindingFlags.Public
                    | BindingFlags.Static
                    | BindingFlags.DeclaredOnly),
            static method => string.Equals(
                method.Name,
                "Enum",
                StringComparison.Ordinal));
    }

    private static IReadOnlyList<string> RemovedWeakTypeNames { get; } =
        new[]
        {
            "MackySoft.JsonSchema.Generation.Annotations.AllowNullAttribute",
            "MackySoft.JsonSchema.Generation.Annotations.AnyValueAttribute",
            "MackySoft.JsonSchema.Generation.Annotations.ConstAttribute",
            "MackySoft.JsonSchema.Generation.Annotations.DiscriminatorAttribute",
            "MackySoft.JsonSchema.Generation.Annotations.EnumAttribute",
            "MackySoft.JsonSchema.Generation.Annotations.ExampleAttribute",
            "MackySoft.JsonSchema.Generation.Annotations.OneOfBranchAttribute",
            "MackySoft.JsonSchema.Generation.Annotations.RangeAttribute",
            "MackySoft.JsonSchema.Generation.Annotations.RequiredAttribute",
            "MackySoft.JsonSchema.Generation.Metadata.JsonContractBranchMetadata",
            "MackySoft.JsonSchema.Generation.Metadata.JsonContractMetadata",
            "MackySoft.JsonSchema.Generation.Metadata.JsonContractMetadataKind",
        };

    private static string[] GetExportedTypeNames (
        Assembly assembly,
        string @namespace)
    {
        return assembly
            .GetExportedTypes()
            .Where(type => string.Equals(
                type.Namespace,
                @namespace,
                StringComparison.Ordinal))
            .Select(static type => type.FullName!)
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToArray();
    }

    private static string[] GetPublicSurface (Type type)
    {
        return GetDeclaredPublicMembers(type)
            .Select(FormatMember)
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToArray();
    }

    private static IEnumerable<MemberInfo> GetDeclaredPublicMembers (
        Type type)
    {
        const BindingFlags flags =
            BindingFlags.Public
            | BindingFlags.Instance
            | BindingFlags.Static
            | BindingFlags.DeclaredOnly;
        return type
            .GetConstructors(flags)
            .Cast<MemberInfo>()
            .Concat(type.GetProperties(flags))
            .Concat(
                type
                    .GetMethods(flags)
                    .Where(static method => !method.IsSpecialName));
    }

    private static string FormatMember (MemberInfo member)
    {
        return member switch
        {
            ConstructorInfo constructor =>
                constructor.DeclaringType!.Name.Split('`')[0]
                + FormatParameters(constructor),
            PropertyInfo property =>
                property.Name + ":" + FormatType(property.PropertyType),
            MethodInfo method =>
                method.Name
                + FormatGenericParameters(method)
                + FormatParameters(method)
                + ":"
                + FormatType(method.ReturnType),
            _ => throw new InvalidOperationException(
                $"Unsupported public member kind '{member.MemberType}'."),
        };
    }

    private static string FormatGenericParameters (MethodInfo method)
    {
        Type[] arguments = method.GetGenericArguments();
        return arguments.Length == 0
            ? string.Empty
            : "<"
                + string.Join(
                    ",",
                    arguments.Select(static argument => argument.Name))
                + ">";
    }

    private static string FormatParameters (MethodBase method)
    {
        return "("
            + string.Join(
                ",",
                method
                    .GetParameters()
                    .Select(static parameter =>
                        FormatType(parameter.ParameterType)))
            + ")";
    }

    private static string FormatType (Type type)
    {
        if (type.IsGenericParameter)
        {
            return type.Name;
        }

        if (type.IsArray)
        {
            return FormatType(type.GetElementType()!) + "[]";
        }

        if (!type.IsGenericType)
        {
            return type.Name;
        }

        return type.Name.Split('`')[0]
            + "<"
            + string.Join(
                ",",
                type
                    .GetGenericArguments()
                    .Select(FormatType))
            + ">";
    }
}
