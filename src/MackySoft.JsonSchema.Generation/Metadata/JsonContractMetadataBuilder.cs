using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace MackySoft.JsonSchema.Generation.Metadata;

/// <summary>
/// Collects typed annotations and value constraints for one effective
/// serializer contract.
/// </summary>
/// <typeparam name="TValue"> The CLR value type described by the target contract. </typeparam>
/// <remarks>
/// <para>
/// A builder is scoped to one registered provider or interpreter callback.
/// Calling it after that callback returns is invalid.
/// </para>
/// <para>
/// Declarations are resolved as a complete set after the callback. Contract
/// generation fails when values conflict, are malformed, or do not apply to
/// the completed JSON shape.
/// </para>
/// </remarks>
public sealed class JsonContractMetadataBuilder<TValue>
{
    private readonly JsonContractMetadataContext<TValue> context;
    private readonly IJsonContractMetadataDeclarationSink sink;
    private bool isCompleted;

    internal JsonContractMetadataBuilder (
        JsonContractMetadataContext<TValue> context,
        IJsonContractMetadataDeclarationSink sink)
    {
        this.context = context
            ?? throw new ArgumentNullException(nameof(context));
        this.sink = sink
            ?? throw new ArgumentNullException(nameof(sink));
    }

    /// <summary> Declares a human-readable title. </summary>
    /// <param name="title"> Non-null display text. </param>
    /// <exception cref="ArgumentNullException"> <paramref name="title" /> is <see langword="null" />. </exception>
    /// <exception cref="InvalidOperationException"> The callback that owns this builder has completed. </exception>
    public void SetTitle (string title)
    {
        EnsureActive();
        sink.AddTitle(
            title ?? throw new ArgumentNullException(nameof(title)));
    }

    /// <summary> Declares explanatory text. </summary>
    /// <param name="description"> Non-null explanatory text. </param>
    /// <exception cref="ArgumentNullException"> <paramref name="description" /> is <see langword="null" />. </exception>
    /// <exception cref="InvalidOperationException"> The callback that owns this builder has completed. </exception>
    public void SetDescription (string description)
    {
        EnsureActive();
        sink.AddDescription(
            description
            ?? throw new ArgumentNullException(nameof(description)));
    }

    /// <summary>
    /// Declares a JSON Schema pattern from the supported ECMA-262 subset.
    /// </summary>
    /// <param name="pattern">
    /// Non-null pattern text. Use <c>$(?![\s\S])</c> instead of a trailing
    /// <c>$</c> when a final line terminator must not satisfy the end assertion.
    /// </param>
    /// <exception cref="ArgumentNullException"> <paramref name="pattern" /> is <see langword="null" />. </exception>
    /// <exception cref="InvalidOperationException"> The callback that owns this builder has completed. </exception>
    public void SetPattern (string pattern)
    {
        EnsureActive();
        sink.AddPattern(
            pattern ?? throw new ArgumentNullException(nameof(pattern)));
    }

    /// <summary> Declares a minimum string length. </summary>
    /// <param name="value"> The declared minimum. Generation rejects a negative value. </param>
    /// <exception cref="InvalidOperationException"> The callback that owns this builder has completed. </exception>
    public void SetMinimumLength (int value)
    {
        EnsureActive();
        sink.AddMinimumLength(value);
    }

    /// <summary> Declares a maximum string length. </summary>
    /// <param name="value"> The declared maximum. Generation rejects a negative value. </param>
    /// <exception cref="InvalidOperationException"> The callback that owns this builder has completed. </exception>
    public void SetMaximumLength (int value)
    {
        EnsureActive();
        sink.AddMaximumLength(value);
    }

    /// <summary> Declares a minimum array item count. </summary>
    /// <param name="value"> The declared minimum. Generation rejects a negative value. </param>
    /// <exception cref="InvalidOperationException"> The callback that owns this builder has completed. </exception>
    public void SetMinimumItemCount (int value)
    {
        EnsureActive();
        sink.AddMinimumItemCount(value);
    }

    /// <summary> Declares a maximum array item count. </summary>
    /// <param name="value"> The declared maximum. Generation rejects a negative value. </param>
    /// <exception cref="InvalidOperationException"> The callback that owns this builder has completed. </exception>
    public void SetMaximumItemCount (int value)
    {
        EnsureActive();
        sink.AddMaximumItemCount(value);
    }

    /// <summary> Declares a minimum object property count. </summary>
    /// <param name="value"> The declared minimum. Generation rejects a negative value. </param>
    /// <exception cref="InvalidOperationException"> The callback that owns this builder has completed. </exception>
    public void SetMinimumPropertyCount (int value)
    {
        EnsureActive();
        sink.AddMinimumPropertyCount(value);
    }

    /// <summary> Declares a maximum object property count. </summary>
    /// <param name="value"> The declared maximum. Generation rejects a negative value. </param>
    /// <exception cref="InvalidOperationException"> The callback that owns this builder has completed. </exception>
    public void SetMaximumPropertyCount (int value)
    {
        EnsureActive();
        sink.AddMaximumPropertyCount(value);
    }

    /// <summary>
    /// Serializes and adds one example with the effective
    /// <see cref="JsonTypeInfo{T}" /> for this target.
    /// </summary>
    /// <param name="value"> The actual typed value to serialize. </param>
    /// <remarks>
    /// Generation rejects a property-scoped declaration when a property-only
    /// converter or number-handling override cannot be reproduced by
    /// <see cref="JsonContractMetadataContext{TValue}.TypeInfo" />.
    /// </remarks>
    /// <exception cref="InvalidOperationException"> The callback that owns this builder has completed. </exception>
    public void AddExample (TValue value)
    {
        EnsureActive();
        sink.EnsureTypedValueSerializationIsAuthoritative(
            context.PropertyInfo,
            "example");
        sink.AddExample(
            JsonSerializer.SerializeToElement(value, context.TypeInfo));
    }

    /// <summary>
    /// Serializes and declares one constant with the effective
    /// <see cref="JsonTypeInfo{T}" /> for this target.
    /// </summary>
    /// <param name="value"> The actual typed value to serialize. </param>
    /// <remarks>
    /// Generation rejects a property-scoped declaration when a property-only
    /// converter or number-handling override cannot be reproduced by
    /// <see cref="JsonContractMetadataContext{TValue}.TypeInfo" />.
    /// </remarks>
    /// <exception cref="InvalidOperationException"> The callback that owns this builder has completed. </exception>
    public void SetConst (TValue value)
    {
        EnsureActive();
        sink.EnsureTypedValueSerializationIsAuthoritative(
            context.PropertyInfo,
            "const");
        sink.SetConstant(
            JsonSerializer.SerializeToElement(value, context.TypeInfo));
    }

    /// <summary> Declares an inclusive numeric lower bound. </summary>
    /// <param name="value"> The exact bound. </param>
    /// <exception cref="ArgumentNullException"> <paramref name="value" /> is <see langword="null" />. </exception>
    /// <exception cref="InvalidOperationException"> The callback that owns this builder has completed. </exception>
    public void SetMinimum (JsonContractNumber value)
    {
        EnsureActive();
        sink.AddMinimum(GetNumber(value).ToJsonElement());
    }

    /// <summary> Declares an exclusive numeric lower bound. </summary>
    /// <param name="value"> The exact bound. </param>
    /// <exception cref="ArgumentNullException"> <paramref name="value" /> is <see langword="null" />. </exception>
    /// <exception cref="InvalidOperationException"> The callback that owns this builder has completed. </exception>
    public void SetExclusiveMinimum (JsonContractNumber value)
    {
        EnsureActive();
        sink.AddExclusiveMinimum(GetNumber(value).ToJsonElement());
    }

    /// <summary> Declares an inclusive numeric upper bound. </summary>
    /// <param name="value"> The exact bound. </param>
    /// <exception cref="ArgumentNullException"> <paramref name="value" /> is <see langword="null" />. </exception>
    /// <exception cref="InvalidOperationException"> The callback that owns this builder has completed. </exception>
    public void SetMaximum (JsonContractNumber value)
    {
        EnsureActive();
        sink.AddMaximum(GetNumber(value).ToJsonElement());
    }

    /// <summary> Declares an exclusive numeric upper bound. </summary>
    /// <param name="value"> The exact bound. </param>
    /// <exception cref="ArgumentNullException"> <paramref name="value" /> is <see langword="null" />. </exception>
    /// <exception cref="InvalidOperationException"> The callback that owns this builder has completed. </exception>
    public void SetExclusiveMaximum (JsonContractNumber value)
    {
        EnsureActive();
        sink.AddExclusiveMaximum(GetNumber(value).ToJsonElement());
    }

    internal void Complete ()
    {
        EnsureActive();
        isCompleted = true;
    }

    internal void Abandon ()
    {
        isCompleted = true;
    }

    private void EnsureActive ()
    {
        if (isCompleted)
        {
            throw new InvalidOperationException(
                "The metadata builder is valid only during its registered extension callback.");
        }
    }

    private static JsonContractNumber GetNumber (JsonContractNumber? value)
    {
        return value ?? throw new ArgumentNullException(nameof(value));
    }
}
