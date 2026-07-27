using MackySoft.JsonSchema.Generation.Extensibility;
using MackySoft.JsonSchema.Generation.Internal.Determinism;

namespace MackySoft.JsonSchema.Generation.Internal.Metadata.Declarations;

internal sealed class MetadataExtensionDeclarationCollector
{
    private readonly IReadOnlyList<MetadataExtensionInvoker> invokers;

    internal MetadataExtensionDeclarationCollector (
        IReadOnlyList<MetadataExtensionRegistration> registrations)
    {
        if (registrations is null)
        {
            throw new ArgumentNullException(nameof(registrations));
        }

        MetadataExtensionInvoker[] values = registrations
            .Select(MetadataExtensionInvoker.Create)
            .ToArray();
        Array.Sort(
            values,
            static (left, right) =>
                UnicodeCodePointComparer.Instance.Compare(
                    left.StableId,
                    right.StableId));
        invokers = Array.AsReadOnly(values);
    }

    internal void Collect (
        MetadataExtensionCollectionRequest request,
        MetadataDeclarationSet declarations)
    {
        foreach (MetadataExtensionInvoker invoker in invokers)
        {
            if (invoker.ValueType == request.ValueTypeInfo.Type)
            {
                invoker.Collect(request, declarations);
            }
        }
    }
}
