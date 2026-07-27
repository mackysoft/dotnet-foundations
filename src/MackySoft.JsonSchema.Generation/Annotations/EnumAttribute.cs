using MackySoft.JsonSchema.Generation.Internal.Common;

namespace MackySoft.JsonSchema.Generation.Annotations;

/// <summary> Restricts a contract value to a finite set of declared JSON values. </summary>
[AttributeUsage(
    AttributeTargets.Class
    | AttributeTargets.Struct
    | AttributeTargets.Enum
    | AttributeTargets.Interface
    | AttributeTargets.Property
    | AttributeTargets.Field,
    Inherited = true)]
public sealed class EnumAttribute : Attribute
{
    /// <summary> Initializes an allowed-value declaration. </summary>
    /// <param name="jsonValues"> One or more JSON values parsed by the contract generator. </param>
    /// <exception cref="ArgumentNullException"> <paramref name="jsonValues" /> is <see langword="null" />. </exception>
    /// <exception cref="ArgumentException"> No value was supplied, or the collection contains <see langword="null" />. </exception>
    public EnumAttribute (params string[] jsonValues)
    {
        if (jsonValues is null)
        {
            throw new ArgumentNullException(nameof(jsonValues));
        }

        if (jsonValues.Length == 0)
        {
            throw new ArgumentException(
                "At least one JSON value must be supplied.",
                nameof(jsonValues));
        }

        JsonValues = JsonContractCollections.Copy(jsonValues, nameof(jsonValues));
    }

    /// <summary> Gets the declared JSON texts in source order. </summary>
    public IReadOnlyList<string> JsonValues { get; }
}
