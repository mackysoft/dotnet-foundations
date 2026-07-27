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
        bool isRoot)
    {
        Member = member;
        DeclaredType = declaredType;
        this.childPath = childPath;
        IsRoot = isRoot;
    }

    internal MemberInfo? Member { get; }

    internal Type DeclaredType { get; }

    internal bool IsRoot { get; }

    internal static ContractNullability Root (Type type)
    {
        return new ContractNullability(
            member: null,
            type,
            Array.Empty<int>(),
            isRoot: true);
    }

    internal static ContractNullability ForMember (
        MemberInfo member,
        Type declaredType)
    {
        return new ContractNullability(
            member,
            declaredType,
            Array.Empty<int>(),
            isRoot: false);
    }

    internal ContractNullability Child (Type childType, int genericArgumentIndex)
    {
        if (Member is null || genericArgumentIndex < 0)
        {
            return new ContractNullability(
                member: null,
                childType,
                Array.Empty<int>(),
                isRoot: false);
        }

        var nestedPath = new int[childPath.Length + 1];
        Array.Copy(childPath, nestedPath, childPath.Length);
        nestedPath[nestedPath.Length - 1] = genericArgumentIndex;
        return new ContractNullability(
            Member,
            DeclaredType,
            nestedPath,
            isRoot: false);
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
            return NullableContractState.NotNullable;
        }

        if (Member is null)
        {
            return NullableContractState.Unknown;
        }

        return NullableMetadataInspector.GetState(
            Member,
            DeclaredType,
            childPath);
    }
}
