using System.Collections;

namespace MackySoft.JsonSchema.Generation.Internal.ModelBuilding.TypeSystem;

/// <summary>
/// Resolves only CLR element and string-key dictionary types. JSON behavior is
/// still selected from <c>JsonTypeInfo.Kind</c> by the model builder.
/// </summary>
internal static class ClrCollectionShapeResolver
{
    internal static bool TryGetDictionaryValueType (
        Type type,
        out Type? valueType,
        out int genericArgumentIndex)
    {
        Type[] candidates = GetSelfAndInterfaces(type)
            .Where(
                static candidate =>
                    candidate.IsGenericType
                    && (candidate.GetGenericTypeDefinition()
                            == typeof(IDictionary<,>)
                        || candidate.GetGenericTypeDefinition()
                            == typeof(IReadOnlyDictionary<,>)))
            .Where(
                static candidate =>
                    candidate.GetGenericArguments()[0] == typeof(string))
            .ToArray();
        Type[] distinctValues = candidates
            .Select(static candidate => candidate.GetGenericArguments()[1])
            .Distinct()
            .ToArray();
        if (distinctValues.Length != 1)
        {
            valueType = null;
            genericArgumentIndex = -1;
            return false;
        }

        valueType = distinctValues[0];
        genericArgumentIndex = GetDirectGenericArgumentIndex(
            type,
            valueType,
            preferredIndex: 1);
        return true;
    }

    internal static bool TryGetEnumerableElementType (
        Type type,
        out Type? elementType,
        out int genericArgumentIndex)
    {
        if (type.IsArray)
        {
            elementType = type.GetElementType();
            genericArgumentIndex = 0;
            return elementType is not null;
        }

        Type[] candidates = GetSelfAndInterfaces(type)
            .Where(
                static candidate =>
                    candidate.IsGenericType
                    && candidate.GetGenericTypeDefinition()
                        == typeof(IEnumerable<>))
            .ToArray();
        Type[] distinctElements = candidates
            .Select(static candidate => candidate.GetGenericArguments()[0])
            .Distinct()
            .ToArray();
        if (distinctElements.Length != 1)
        {
            elementType = null;
            genericArgumentIndex = -1;
            return false;
        }

        elementType = distinctElements[0];
        genericArgumentIndex = GetDirectGenericArgumentIndex(
            type,
            elementType,
            preferredIndex: 0);
        return true;
    }

    internal static bool ImplementsNonGenericDictionary (Type type)
    {
        return typeof(IDictionary).IsAssignableFrom(type);
    }

    private static IEnumerable<Type> GetSelfAndInterfaces (Type type)
    {
        yield return type;
        foreach (Type interfaceType in type.GetInterfaces())
        {
            yield return interfaceType;
        }
    }

    private static int GetDirectGenericArgumentIndex (
        Type type,
        Type selectedType,
        int preferredIndex)
    {
        if (!type.IsGenericType)
        {
            return -1;
        }

        Type[] arguments = type.GetGenericArguments();
        if ((uint)preferredIndex < (uint)arguments.Length
            && arguments[preferredIndex] == selectedType)
        {
            return preferredIndex;
        }

        int foundIndex = -1;
        for (int index = 0; index < arguments.Length; index++)
        {
            if (arguments[index] != selectedType)
            {
                continue;
            }

            if (foundIndex >= 0)
            {
                return -1;
            }

            foundIndex = index;
        }

        return foundIndex;
    }
}
