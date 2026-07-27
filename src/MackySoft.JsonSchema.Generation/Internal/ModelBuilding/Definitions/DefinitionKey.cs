namespace MackySoft.JsonSchema.Generation.Internal.ModelBuilding.Definitions;

/// <summary>
/// Identifies one reusable contract shape, including injected discriminator
/// semantics that distinguish polymorphic branches of the same CLR type.
/// </summary>
internal readonly struct DefinitionKey : IEquatable<DefinitionKey>
{
    internal DefinitionKey (
        Type type,
        string? discriminatorPropertyName,
        string? discriminatorCanonicalValue)
    {
        Type = type;
        DiscriminatorPropertyName = discriminatorPropertyName;
        DiscriminatorCanonicalValue = discriminatorCanonicalValue;
    }

    internal Type Type { get; }

    internal string? DiscriminatorPropertyName { get; }

    internal string? DiscriminatorCanonicalValue { get; }

    public bool Equals (DefinitionKey other)
    {
        return Type == other.Type
            && string.Equals(
                DiscriminatorPropertyName,
                other.DiscriminatorPropertyName,
                StringComparison.Ordinal)
            && string.Equals(
                DiscriminatorCanonicalValue,
                other.DiscriminatorCanonicalValue,
                StringComparison.Ordinal);
    }

    public override bool Equals (object? obj)
    {
        return obj is DefinitionKey other && Equals(other);
    }

    public override int GetHashCode ()
    {
        unchecked
        {
            int hash = Type.GetHashCode();
            hash = (hash * 397)
                ^ (DiscriminatorPropertyName?.GetHashCode() ?? 0);
            hash = (hash * 397)
                ^ (DiscriminatorCanonicalValue?.GetHashCode() ?? 0);
            return hash;
        }
    }
}
