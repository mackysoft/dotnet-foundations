using System.Collections.ObjectModel;
using System.Reflection;

namespace MackySoft.JsonSchema.Generation.Internal.ModelBuilding.TypeSystem;

internal static class NullableMetadataInspector
{
    private const string NullableAttributeName =
        "System.Runtime.CompilerServices.NullableAttribute";

    private const string NullableContextAttributeName =
        "System.Runtime.CompilerServices.NullableContextAttribute";

    public static NullableContractState GetState (
        PropertyInfo property,
        params int[] childPath)
    {
        if (property == null)
        {
            throw new ArgumentNullException(nameof(property));
        }

        return GetState(
            property.PropertyType,
            property.GetCustomAttributesData(),
            property,
            childPath);
    }

    public static NullableContractState GetState (
        FieldInfo field,
        params int[] childPath)
    {
        if (field == null)
        {
            throw new ArgumentNullException(nameof(field));
        }

        return GetState(
            field.FieldType,
            field.GetCustomAttributesData(),
            field,
            childPath);
    }

    public static NullableContractState GetState (
        ParameterInfo parameter,
        params int[] childPath)
    {
        if (parameter == null)
        {
            throw new ArgumentNullException(nameof(parameter));
        }

        return GetState(
            parameter.ParameterType,
            parameter.GetCustomAttributesData(),
            parameter.Member,
            childPath);
    }

    public static NullableContractState GetState (
        MemberInfo member,
        Type type,
        params int[] childPath)
    {
        if (member == null)
        {
            throw new ArgumentNullException(nameof(member));
        }

        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        return GetState(
            type,
            member.GetCustomAttributesData(),
            member,
            childPath);
    }

    public static NullableContractState GetState (
        Type type,
        params int[] childPath)
    {
        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        return GetState(
            type,
            type.GetCustomAttributesData(),
            type,
            childPath);
    }

    private static NullableContractState GetState (
        Type type,
        IList<CustomAttributeData> attributes,
        MemberInfo contextOwner,
        int[] childPath)
    {
        if (childPath == null)
        {
            throw new ArgumentNullException(nameof(childPath));
        }

        object? nullableFlags = FindNullableFlags(attributes);
        NullableContractState context = FindNullableContext(contextOwner);
        int flagIndex = 0;

        return InspectType(
            type,
            nullableFlags,
            context,
            childPath,
            pathIndex: 0,
            ref flagIndex);
    }

    private static NullableContractState InspectType (
        Type type,
        object? nullableFlags,
        NullableContractState context,
        int[] childPath,
        int pathIndex,
        ref int flagIndex)
    {
        Type underlyingType = UnwrapIndirection(type);
        NullableContractState state;

        if (underlyingType.IsValueType)
        {
            Type? nullableUnderlyingType = Nullable.GetUnderlyingType(underlyingType);
            if (nullableUnderlyingType != null)
            {
                underlyingType = nullableUnderlyingType;
                state = NullableContractState.Nullable;
            }
            else
            {
                state = NullableContractState.NotNullable;
            }

            if (underlyingType.IsGenericType)
            {
                flagIndex++;
            }
        }
        else
        {
            state = TryReadState(nullableFlags, flagIndex++, out NullableContractState declaredState)
                ? declaredState
                : context;
        }

        if (pathIndex == childPath.Length)
        {
            return state;
        }

        int childIndex = childPath[pathIndex];
        if (underlyingType.IsArray)
        {
            if (childIndex != 0)
            {
                throw InvalidChildPath(type, childIndex, pathIndex);
            }

            return InspectType(
                underlyingType.GetElementType()!,
                nullableFlags,
                context,
                childPath,
                pathIndex + 1,
                ref flagIndex);
        }

        if (!underlyingType.IsGenericType)
        {
            throw InvalidChildPath(type, childIndex, pathIndex);
        }

        Type[] genericArguments = underlyingType.GetGenericArguments();
        if ((uint)childIndex >= (uint)genericArguments.Length)
        {
            throw InvalidChildPath(type, childIndex, pathIndex);
        }

        for (int index = 0; index < childIndex; index++)
        {
            SkipType(genericArguments[index], ref flagIndex);
        }

        return InspectType(
            genericArguments[childIndex],
            nullableFlags,
            context,
            childPath,
            pathIndex + 1,
            ref flagIndex);
    }

    private static void SkipType (Type type, ref int flagIndex)
    {
        Type underlyingType = UnwrapIndirection(type);
        if (underlyingType.IsValueType)
        {
            underlyingType = Nullable.GetUnderlyingType(underlyingType) ?? underlyingType;
            if (underlyingType.IsGenericType)
            {
                flagIndex++;
            }
        }
        else
        {
            flagIndex++;
            if (underlyingType.IsArray)
            {
                SkipType(underlyingType.GetElementType()!, ref flagIndex);
            }
        }

        if (!underlyingType.IsGenericType)
        {
            return;
        }

        foreach (Type genericArgument in underlyingType.GetGenericArguments())
        {
            SkipType(genericArgument, ref flagIndex);
        }
    }

    private static Type UnwrapIndirection (Type type)
    {
        while (type.IsByRef || type.IsPointer)
        {
            type = type.GetElementType()!;
        }

        return type;
    }

    private static object? FindNullableFlags (IList<CustomAttributeData> attributes)
    {
        foreach (CustomAttributeData attribute in attributes)
        {
            if (attribute.AttributeType.FullName == NullableAttributeName
                && attribute.ConstructorArguments.Count == 1)
            {
                return attribute.ConstructorArguments[0].Value;
            }
        }

        return null;
    }

    private static NullableContractState FindNullableContext (MemberInfo contextOwner)
    {
        MemberInfo? current = contextOwner;
        while (current != null)
        {
            if (TryReadContext(current.GetCustomAttributesData(), out NullableContractState state))
            {
                return state;
            }

            current = current.DeclaringType;
        }

        if (TryReadContext(contextOwner.Module.GetCustomAttributesData(), out NullableContractState moduleState))
        {
            return moduleState;
        }

        return TryReadContext(
            contextOwner.Module.Assembly.GetCustomAttributesData(),
            out NullableContractState assemblyState)
            ? assemblyState
            : NullableContractState.Unknown;
    }

    private static bool TryReadContext (
        IList<CustomAttributeData> attributes,
        out NullableContractState state)
    {
        foreach (CustomAttributeData attribute in attributes)
        {
            if (attribute.AttributeType.FullName != NullableContextAttributeName
                || attribute.ConstructorArguments.Count != 1)
            {
                continue;
            }

            state = Translate(attribute.ConstructorArguments[0].Value);
            return true;
        }

        state = NullableContractState.Unknown;
        return false;
    }

    private static bool TryReadState (
        object? nullableFlags,
        int index,
        out NullableContractState state)
    {
        if (nullableFlags is byte singleFlag)
        {
            state = Translate(singleFlag);
            return true;
        }

        if (nullableFlags is ReadOnlyCollection<CustomAttributeTypedArgument> arguments
            && index < arguments.Count
            && arguments[index].Value is byte collectionFlag)
        {
            state = Translate(collectionFlag);
            return true;
        }

        if (nullableFlags is byte[] flags && index < flags.Length)
        {
            state = Translate(flags[index]);
            return true;
        }

        state = NullableContractState.Unknown;
        return false;
    }

    private static NullableContractState Translate (object? flag)
    {
        return flag is byte value
            ? Translate(value)
            : NullableContractState.Unknown;
    }

    private static NullableContractState Translate (byte flag)
    {
        return flag switch
        {
            1 => NullableContractState.NotNullable,
            2 => NullableContractState.Nullable,
            _ => NullableContractState.Unknown,
        };
    }

    private static ArgumentOutOfRangeException InvalidChildPath (
        Type type,
        int childIndex,
        int pathIndex)
    {
        return new ArgumentOutOfRangeException(
            "childPath",
            childIndex,
            $"Child index {childIndex} at path position {pathIndex} is not valid for '{type}'.");
    }
}
