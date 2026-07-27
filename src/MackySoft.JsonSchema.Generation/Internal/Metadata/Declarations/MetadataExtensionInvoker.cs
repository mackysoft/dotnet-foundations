using System.Reflection;
using MackySoft.JsonSchema.Generation.Extensibility;
using MackySoft.JsonSchema.Generation.Internal.Metadata.Contracts;
using MackySoft.JsonSchema.Generation.Metadata;

namespace MackySoft.JsonSchema.Generation.Internal.Metadata.Declarations;

internal abstract class MetadataExtensionInvoker : IJsonContractExtension
{
    private readonly IJsonContractExtension extension;

    private MetadataExtensionInvoker (
        IJsonContractExtension extension,
        Type valueType)
    {
        this.extension = extension
            ?? throw new ArgumentNullException(nameof(extension));
        ValueType = valueType
            ?? throw new ArgumentNullException(nameof(valueType));
    }

    public string StableId => extension.StableId;

    public string ContractVersion => extension.ContractVersion;

    internal Type ValueType { get; }

    internal abstract void Collect (
        MetadataExtensionCollectionRequest request,
        MetadataDeclarationSet declarations);

    internal static MetadataExtensionInvoker Create (
        MetadataExtensionRegistration registration)
    {
        if (registration is null)
        {
            throw new ArgumentNullException(nameof(registration));
        }

        Type invokerType = registration.IsAttributeInterpreter
            ? typeof(AttributeInterpreterInvoker<,>).MakeGenericType(
                registration.AttributeType!,
                registration.ValueType)
            : typeof(ProviderInvoker<>).MakeGenericType(
                registration.ValueType);
        return (MetadataExtensionInvoker)(
            Activator.CreateInstance(
                invokerType,
                registration.Extension)
            ?? throw new InvalidOperationException(
                "A typed metadata extension invoker could not be created."));
    }

    protected JsonContractMetadataBuilder<TValue> CreateBuilder<TValue> (
        MetadataResolutionTarget target,
        JsonContractMetadataContext<TValue> context,
        MetadataDeclarationSet declarations)
    {
        return new JsonContractMetadataBuilder<TValue>(
            context,
            new MetadataDeclarationSink(
                target,
                StableId,
                declarations));
    }

    protected Exception CallbackFailure (
        MetadataResolutionTarget target,
        string callbackKind,
        Exception exception)
    {
        return MetadataFailure.Invalid(
            target,
            new[] { StableId },
            $"{callbackKind} '{StableId}' failed to declare metadata.",
            exception);
    }

    protected void CollectCallbackDeclarations<TValue> (
        MetadataResolutionTarget target,
        JsonContractMetadataContext<TValue> context,
        MetadataDeclarationSet declarations,
        string callbackKind,
        Action<JsonContractMetadataBuilder<TValue>> callback)
    {
        var callbackDeclarations = new MetadataDeclarationSet();
        JsonContractMetadataBuilder<TValue> builder =
            CreateBuilder(
                target,
                context,
                callbackDeclarations);
        try
        {
            callback(builder);
            builder.Complete();
            declarations.AddRange(callbackDeclarations);
        }
        catch (Exception exception)
        {
            builder.Abandon();
            if (MetadataFailure.IsGenerationFailure(exception))
            {
                throw;
            }

            throw CallbackFailure(
                target,
                callbackKind,
                exception);
        }
    }

    private sealed class ProviderInvoker<TValue> : MetadataExtensionInvoker
    {
        private readonly IJsonContractMetadataProvider<TValue> provider;

        public ProviderInvoker (IJsonContractExtension extension)
            : base(extension, typeof(TValue))
        {
            provider = extension
                as IJsonContractMetadataProvider<TValue>
                ?? throw new ArgumentException(
                    "The registered extension does not implement the expected typed metadata provider.",
                    nameof(extension));
        }

        internal override void Collect (
            MetadataExtensionCollectionRequest request,
            MetadataDeclarationSet declarations)
        {
            JsonContractMetadataContext<TValue> context =
                request.CreateContext<TValue>();
            CollectCallbackDeclarations(
                request.Target,
                context,
                declarations,
                "Metadata provider",
                builder => provider.ProvideMetadata(context, builder));
        }
    }

    private sealed class AttributeInterpreterInvoker<TAttribute, TValue>
        : MetadataExtensionInvoker
        where TAttribute : Attribute
    {
        private readonly IJsonContractAttributeInterpreter<TAttribute, TValue>
            interpreter;

        public AttributeInterpreterInvoker (IJsonContractExtension extension)
            : base(extension, typeof(TValue))
        {
            interpreter = extension
                as IJsonContractAttributeInterpreter<TAttribute, TValue>
                ?? throw new ArgumentException(
                    "The registered extension does not implement the expected typed attribute interpreter.",
                    nameof(extension));
        }

        internal override void Collect (
            MetadataExtensionCollectionRequest request,
            MetadataDeclarationSet declarations)
        {
            JsonContractMetadataContext<TValue> context =
                request.CreateContext<TValue>();
            foreach (TAttribute attribute in GetAttributes(
                request.Target,
                request.AttributeSource))
            {
                CollectCallbackDeclarations(
                    request.Target,
                    context,
                    declarations,
                    "Attribute interpreter",
                    builder => interpreter.InterpretAttribute(
                        attribute,
                        context,
                        builder));
            }
        }

        private IReadOnlyList<TAttribute> GetAttributes (
            MetadataResolutionTarget target,
            MemberInfo attributeSource)
        {
            try
            {
                return Attribute
                    .GetCustomAttributes(
                        attributeSource,
                        typeof(TAttribute),
                        inherit: true)
                    .Cast<TAttribute>()
                    .ToArray();
            }
            catch (Exception exception) when (
                exception is ArgumentException
                    or CustomAttributeFormatException
                    or TargetInvocationException
                    or TypeLoadException)
            {
                throw CallbackFailure(
                    target,
                    "Attribute interpreter",
                    exception);
            }
        }
    }
}
