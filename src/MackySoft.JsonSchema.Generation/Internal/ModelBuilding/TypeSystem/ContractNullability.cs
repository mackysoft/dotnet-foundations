using System.Reflection;

namespace MackySoft.JsonSchema.Generation.Internal.ModelBuilding.TypeSystem;

/// <summary>
/// Tracks the nullable metadata path from one serialized member into its nested
/// generic or array value.
/// </summary>
internal readonly struct ContractNullability
{
    private readonly int[] childPath;

    private ContractNullability (
        MemberInfo? member,
        Type declaredType,
        int[] childPath,
        bool isRoot,
        bool rootAcceptsNull)
    {
        Member = member;
        DeclaredType = declaredType;
        this.childPath = childPath;
        IsRoot = isRoot;
        RootAcceptsNull = rootAcceptsNull;
    }

    internal MemberInfo? Member { get; }

    internal Type DeclaredType { get; }

    internal bool IsRoot { get; }

    private bool RootAcceptsNull { get; }

    internal static ContractNullability Root (
        Type type,
        bool acceptsNull)
    {
        return new ContractNullability(
            member: null,
            type,
            Array.Empty<int>(),
            isRoot: true,
            rootAcceptsNull: acceptsNull);
    }

    internal static ContractNullability ForMember (
        MemberInfo member,
        Type declaredType)
    {
        return new ContractNullability(
            member,
            declaredType,
            Array.Empty<int>(),
            isRoot: false,
            rootAcceptsNull: false);
    }

    internal ContractNullability Child (Type childType, int genericArgumentIndex)
    {
        if (Member is null || genericArgumentIndex < 0)
        {
            return new ContractNullability(
                member: null,
                childType,
                Array.Empty<int>(),
                isRoot: false,
                rootAcceptsNull: false);
        }

        var nestedPath = new int[childPath.Length + 1];
        Array.Copy(childPath, nestedPath, childPath.Length);
        nestedPath[nestedPath.Length - 1] = genericArgumentIndex;
        return new ContractNullability(
            Member,
            DeclaredType,
            nestedPath,
            isRoot: false,
            rootAcceptsNull: false);
    }

    internal NullableContractState ResolveState (Type valueType)
    {
        if (Nullable.GetUnderlyingType(valueType) is not null)
        {
            return NullableContractState.Nullable;
        }

        if (valueType.IsValueType)
        {
            return NullableContractState.NotNullable;
        }

        if (IsRoot)
        {
            return RootAcceptsNull
                ? NullableContractState.Nullable
                : NullableContractState.NotNullable;
        }

        if (Member is null)
        {
            return NullableContractState.Nullable;
        }

        return NullableMetadataInspector.GetState(
            Member,
            DeclaredType,
            childPath);
    }
}
