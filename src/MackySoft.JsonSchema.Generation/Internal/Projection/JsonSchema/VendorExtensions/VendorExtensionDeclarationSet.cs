using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using MackySoft.Json.Canonicalization;
using MackySoft.JsonSchema.Generation.Extensibility;
using MackySoft.JsonSchema.Generation.Internal.Determinism;

namespace MackySoft.JsonSchema.Generation.Internal.Projection.JsonSchema.VendorExtensions;

/// <summary>Validates, normalizes, and applies additive vendor-extension declarations.</summary>
internal sealed class VendorExtensionDeclarationSet
{
    private readonly JsonObject baseDocument;
    private readonly string contractId;
    private readonly Dictionary<
        (string TargetPointer, string Name),
        PendingVendorExtension> declarations = new();

    public VendorExtensionDeclarationSet (
        JsonObject baseDocument,
        string contractId)
    {
        this.baseDocument = baseDocument;
        this.contractId = contractId;
    }

    public void Add (
        string sourceId,
        JsonSchemaVendorExtension? extension)
    {
        if (extension is null)
        {
            throw VendorExtensionFailure.Invalid(
                contractId,
                sourceId,
                $"Document post-processor '{sourceId}' returned a null vendor-extension declaration.");
        }

        if (extension.Name.Length <= 2
            || !extension.Name.StartsWith("x-", StringComparison.Ordinal))
        {
            throw VendorExtensionFailure.Invalid(
                contractId,
                sourceId,
                $"Document post-processor '{sourceId}' declared invalid vendor-extension name '{extension.Name}'.");
        }

        if (extension.Value.ValueKind == JsonValueKind.Undefined)
        {
            throw VendorExtensionFailure.Invalid(
                contractId,
                sourceId,
                $"Document post-processor '{sourceId}' declared an undefined value for '{extension.Name}'.");
        }

        ValidateUnicode(sourceId, extension);
        ValidateTarget(sourceId, extension);
        byte[] canonicalValue = CanonicalizeValue(sourceId, extension);

        var location = (extension.TargetPointer, extension.Name);
        if (!declarations.TryGetValue(
                location,
                out PendingVendorExtension? existing))
        {
            declarations.Add(
                location,
                new PendingVendorExtension(
                    extension.TargetPointer,
                    extension.Name,
                    canonicalValue,
                    sourceId));
            return;
        }

        existing.AddSource(sourceId);
        if (!existing.CanonicalValue.AsSpan().SequenceEqual(canonicalValue))
        {
            throw VendorExtensionFailure.Conflict(
                contractId,
                $"Document post-processors declared conflicting values for vendor extension '{extension.Name}' at '{extension.TargetPointer}'.",
                existing.GetOrderedSourceIds());
        }
    }

    public void ApplyTo (JsonObject document)
    {
        PendingVendorExtension[] orderedDeclarations = declarations.Values.ToArray();
        Array.Sort(
            orderedDeclarations,
            static (left, right) =>
            {
                int pointerComparison = UnicodeCodePointComparer.Instance.Compare(
                    left.TargetPointer,
                    right.TargetPointer);
                return pointerComparison != 0
                    ? pointerComparison
                    : UnicodeCodePointComparer.Instance.Compare(
                        left.Name,
                        right.Name);
            });

        foreach (PendingVendorExtension declaration in orderedDeclarations)
        {
            JsonObject target = JsonPointerResolver.ResolveSchemaObject(
                document,
                declaration.TargetPointer);
            target.Add(
                declaration.Name,
                JsonNode.Parse(Encoding.UTF8.GetString(declaration.CanonicalValue)));
        }
    }

    private void ValidateUnicode (
        string sourceId,
        JsonSchemaVendorExtension extension)
    {
        try
        {
            _ = UnicodeCodePointComparer.Instance.Compare(
                extension.TargetPointer,
                extension.TargetPointer);
            _ = UnicodeCodePointComparer.Instance.Compare(
                extension.Name,
                extension.Name);
        }
        catch (ArgumentException exception)
        {
            throw VendorExtensionFailure.Invalid(
                contractId,
                sourceId,
                $"Document post-processor '{sourceId}' declared a vendor-extension location containing invalid Unicode.",
                exception);
        }
    }

    private void ValidateTarget (
        string sourceId,
        JsonSchemaVendorExtension extension)
    {
        JsonObject target;
        try
        {
            target = JsonPointerResolver.ResolveSchemaObject(
                baseDocument,
                extension.TargetPointer);
        }
        catch (Exception exception) when (
            exception is ArgumentException
            or InvalidOperationException
            or KeyNotFoundException
            or IndexOutOfRangeException)
        {
            throw VendorExtensionFailure.Invalid(
                contractId,
                sourceId,
                $"Document post-processor '{sourceId}' targeted invalid JSON Pointer '{extension.TargetPointer}'.",
                exception);
        }

        if (target.ContainsKey(extension.Name))
        {
            throw VendorExtensionFailure.Invalid(
                contractId,
                sourceId,
                $"Vendor extension '{extension.Name}' at '{extension.TargetPointer}' collides with the base JSON Schema document.");
        }
    }

    private byte[] CanonicalizeValue (
        string sourceId,
        JsonSchemaVendorExtension extension)
    {
        try
        {
            return JsonElementUtility.GetCanonicalBytes(extension.Value);
        }
        catch (Exception exception) when (
            exception is JsonException
            or JsonCanonicalizationException
            or ArgumentException
            or InvalidOperationException)
        {
            throw VendorExtensionFailure.Invalid(
                contractId,
                sourceId,
                $"Vendor extension '{extension.Name}' at '{extension.TargetPointer}' is not a canonicalizable JSON value.",
                exception);
        }
    }

    private sealed class PendingVendorExtension
    {
        private readonly HashSet<string> sourceIds = new(StringComparer.Ordinal);

        public PendingVendorExtension (
            string targetPointer,
            string name,
            byte[] canonicalValue,
            string sourceId)
        {
            TargetPointer = targetPointer;
            Name = name;
            CanonicalValue = canonicalValue;
            sourceIds.Add(sourceId);
        }

        public string TargetPointer { get; }

        public string Name { get; }

        public byte[] CanonicalValue { get; }

        public void AddSource (string sourceId)
        {
            sourceIds.Add(sourceId);
        }

        public IReadOnlyList<string> GetOrderedSourceIds ()
        {
            string[] orderedSourceIds = sourceIds.ToArray();
            Array.Sort(orderedSourceIds, UnicodeCodePointComparer.Instance);
            return Array.AsReadOnly(orderedSourceIds);
        }
    }
}
