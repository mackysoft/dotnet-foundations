using System.Text.Json;
using MackySoft.JsonSchema.Generation.Internal.Common;

namespace MackySoft.JsonSchema.Generation.Metadata;

/// <summary> Carries one strongly classified metadata declaration from a registered provider. </summary>
public sealed class JsonContractMetadata
{
    private JsonContractMetadata (
        JsonContractMetadataKind kind,
        string? stringValue,
        JsonElement? jsonValue,
        int? integerValue,
        JsonContractBranchMetadata? branchValue)
    {
        Kind = kind;
        StringValue = stringValue;
        JsonValue = JsonContractCollections.CloneNullableJsonElement(jsonValue);
        IntegerValue = integerValue;
        BranchValue = branchValue;
    }

    /// <summary> Gets the declaration category. </summary>
    public JsonContractMetadataKind Kind { get; }

    /// <summary> Gets the text payload for title, description, pattern, and format declarations. </summary>
    public string? StringValue { get; }

    /// <summary> Gets the JSON payload for examples, constants, enum values, and numeric bounds. </summary>
    public JsonElement? JsonValue { get; }

    /// <summary> Gets the integer payload for length and count declarations. </summary>
    public int? IntegerValue { get; }

    /// <summary> Gets the oneOf branch payload for a branch declaration. </summary>
    public JsonContractBranchMetadata? BranchValue { get; }

    /// <summary> Creates a title declaration. </summary>
    /// <param name="title"> Title text validated during model construction. </param>
    /// <exception cref="ArgumentNullException"> <paramref name="title" /> is <see langword="null" />. </exception>
    public static JsonContractMetadata Title (string title)
    {
        return ForString(JsonContractMetadataKind.Title, title, nameof(title));
    }

    /// <summary> Creates a description declaration. </summary>
    /// <param name="description"> Description text validated during model construction. </param>
    /// <exception cref="ArgumentNullException"> <paramref name="description" /> is <see langword="null" />. </exception>
    public static JsonContractMetadata Description (string description)
    {
        return ForString(
            JsonContractMetadataKind.Description,
            description,
            nameof(description));
    }

    /// <summary> Creates a JSON example declaration. </summary>
    public static JsonContractMetadata Example (JsonElement value)
    {
        return ForJson(JsonContractMetadataKind.Example, value);
    }

    /// <summary> Creates a required-property declaration. </summary>
    public static JsonContractMetadata Required ()
    {
        return Marker(JsonContractMetadataKind.Required);
    }

    /// <summary>
    /// Creates an explicit JSON null-acceptance declaration for a reference-type
    /// root or a serialized member whose authoritative contract accepts
    /// <see langword="null" />.
    /// </summary>
    public static JsonContractMetadata AllowNull ()
    {
        return Marker(JsonContractMetadataKind.AllowNull);
    }

    /// <summary> Creates a required constant declaration. </summary>
    public static JsonContractMetadata Const (JsonElement value)
    {
        return ForJson(JsonContractMetadataKind.Const, value);
    }

    /// <summary> Creates one finite allowed-value declaration. </summary>
    public static JsonContractMetadata EnumValue (JsonElement value)
    {
        return ForJson(JsonContractMetadataKind.EnumValue, value);
    }

    /// <summary> Creates an inclusive numeric lower-bound declaration. </summary>
    public static JsonContractMetadata Minimum (JsonElement value)
    {
        return ForJson(JsonContractMetadataKind.Minimum, value);
    }

    /// <summary> Creates an exclusive numeric lower-bound declaration. </summary>
    public static JsonContractMetadata ExclusiveMinimum (JsonElement value)
    {
        return ForJson(JsonContractMetadataKind.ExclusiveMinimum, value);
    }

    /// <summary> Creates an inclusive numeric upper-bound declaration. </summary>
    public static JsonContractMetadata Maximum (JsonElement value)
    {
        return ForJson(JsonContractMetadataKind.Maximum, value);
    }

    /// <summary> Creates an exclusive numeric upper-bound declaration. </summary>
    public static JsonContractMetadata ExclusiveMaximum (JsonElement value)
    {
        return ForJson(JsonContractMetadataKind.ExclusiveMaximum, value);
    }

    /// <summary> Creates a minimum string-length declaration. </summary>
    public static JsonContractMetadata MinimumLength (int value)
    {
        return ForInteger(JsonContractMetadataKind.MinimumLength, value);
    }

    /// <summary> Creates a maximum string-length declaration. </summary>
    public static JsonContractMetadata MaximumLength (int value)
    {
        return ForInteger(JsonContractMetadataKind.MaximumLength, value);
    }

    /// <summary> Creates a minimum array item-count declaration. </summary>
    public static JsonContractMetadata MinimumItems (int value)
    {
        return ForInteger(JsonContractMetadataKind.MinimumItems, value);
    }

    /// <summary> Creates a maximum array item-count declaration. </summary>
    public static JsonContractMetadata MaximumItems (int value)
    {
        return ForInteger(JsonContractMetadataKind.MaximumItems, value);
    }

    /// <summary> Creates a minimum object property-count declaration. </summary>
    public static JsonContractMetadata MinimumProperties (int value)
    {
        return ForInteger(JsonContractMetadataKind.MinimumProperties, value);
    }

    /// <summary> Creates a maximum object property-count declaration. </summary>
    public static JsonContractMetadata MaximumProperties (int value)
    {
        return ForInteger(JsonContractMetadataKind.MaximumProperties, value);
    }

    /// <summary>
    /// Creates a JSON Schema regular-expression pattern declaration from the
    /// interoperable ECMA-262 token subset recommended by Draft 2020-12.
    /// </summary>
    /// <param name="pattern"> Pattern text validated during model construction. </param>
    /// <remarks>
    /// Contract generation fails when the declared text uses syntax outside
    /// the supported subset or is otherwise malformed.
    /// </remarks>
    /// <exception cref="ArgumentNullException"> <paramref name="pattern" /> is <see langword="null" />. </exception>
    public static JsonContractMetadata Pattern (string pattern)
    {
        return ForString(JsonContractMetadataKind.Pattern, pattern, nameof(pattern));
    }

    /// <summary> Creates a semantic string format declaration. </summary>
    /// <param name="format"> Format text validated during model construction. </param>
    /// <exception cref="ArgumentNullException"> <paramref name="format" /> is <see langword="null" />. </exception>
    public static JsonContractMetadata Format (string format)
    {
        return ForString(JsonContractMetadataKind.Format, format, nameof(format));
    }

    /// <summary> Creates a declaration that accepts any JSON value without shape constraints. </summary>
    public static JsonContractMetadata Arbitrary ()
    {
        return Marker(JsonContractMetadataKind.Arbitrary);
    }

    /// <summary> Creates one exclusive branch declaration. </summary>
    /// <param name="branch"> The complete branch metadata. </param>
    /// <exception cref="ArgumentNullException"> <paramref name="branch" /> is <see langword="null" />. </exception>
    public static JsonContractMetadata OneOfBranch (
        JsonContractBranchMetadata branch)
    {
        return new JsonContractMetadata(
            JsonContractMetadataKind.OneOfBranch,
            stringValue: null,
            jsonValue: null,
            integerValue: null,
            branch ?? throw new ArgumentNullException(nameof(branch)));
    }

    /// <summary> Creates a tagged-union discriminator declaration. </summary>
    /// <param name="propertyName"> The exact serialized JSON property name. </param>
    /// <exception cref="ArgumentNullException"> <paramref name="propertyName" /> is <see langword="null" />. </exception>
    public static JsonContractMetadata Discriminator (string propertyName)
    {
        return ForString(
            JsonContractMetadataKind.Discriminator,
            propertyName,
            nameof(propertyName));
    }

    private static JsonContractMetadata ForString (
        JsonContractMetadataKind kind,
        string value,
        string parameterName)
    {
        if (value is null)
        {
            throw new ArgumentNullException(parameterName);
        }

        return new JsonContractMetadata(kind, value, null, null, null);
    }

    private static JsonContractMetadata ForJson (
        JsonContractMetadataKind kind,
        JsonElement value)
    {
        return new JsonContractMetadata(kind, null, value, null, null);
    }

    private static JsonContractMetadata ForInteger (
        JsonContractMetadataKind kind,
        int value)
    {
        return new JsonContractMetadata(kind, null, null, value, null);
    }

    private static JsonContractMetadata Marker (JsonContractMetadataKind kind)
    {
        return new JsonContractMetadata(kind, null, null, null, null);
    }
}
