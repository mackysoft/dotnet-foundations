namespace MackySoft.Text.Vocabularies;

/// <summary> Declares that an enum is the carrier for one finite text vocabulary definition. </summary>
/// <remarks>
/// A declared definition must contain at least one member. Every member must declare exactly one
/// <see cref="VocabularyTextAttribute" />, and both member values and canonical texts must be unique.
/// </remarks>
[AttributeUsage(AttributeTargets.Enum, AllowMultiple = false, Inherited = false)]
public sealed class VocabularyDefinitionAttribute : Attribute
{
}
