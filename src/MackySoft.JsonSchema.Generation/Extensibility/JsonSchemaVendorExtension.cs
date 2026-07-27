using System.Text.Json;
using MackySoft.JsonSchema.Generation.Internal.Determinism;

namespace MackySoft.JsonSchema.Generation.Extensibility;

/// <summary> Declares one additive, delivery-only JSON Schema vendor annotation. </summary>
public sealed class JsonSchemaVendorExtension
{
    /// <summary> Initializes a vendor-extension declaration. </summary>
    /// <param name="targetPointer"> The JSON Pointer of the object that receives the annotation. An empty pointer selects the document root. </param>
    /// <param name="name"> The annotation property name, validated as an <c>x-</c> name by the generator. </param>
    /// <param name="value"> The annotation JSON value. </param>
    /// <exception cref="ArgumentNullException"> A required input string is <see langword="null" />. </exception>
    /// <exception cref="ArgumentException"> <paramref name="name" /> is empty or whitespace. </exception>
    public JsonSchemaVendorExtension (
        string targetPointer,
        string name,
        JsonElement value)
    {
        TargetPointer = targetPointer ?? throw new ArgumentNullException(nameof(targetPointer));

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "The vendor-extension property name must not be empty or whitespace.",
                nameof(name));
        }

        Name = name;
        Value = JsonElementUtility.Clone(value);
    }

    /// <summary> Gets the JSON Pointer of the target schema object. </summary>
    public string TargetPointer { get; }

    /// <summary> Gets the vendor-extension property name. </summary>
    public string Name { get; }

    /// <summary> Gets an independently owned copy of the annotation value. </summary>
    public JsonElement Value { get; }
}
