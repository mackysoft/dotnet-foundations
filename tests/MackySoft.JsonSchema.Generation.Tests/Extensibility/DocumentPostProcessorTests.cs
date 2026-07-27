using System.Collections;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using MackySoft.JsonSchema.Generation.Diagnostics;
using MackySoft.JsonSchema.Generation.Extensibility;
using MackySoft.JsonSchema.Generation.Tests.Fixtures;

namespace MackySoft.JsonSchema.Generation.Tests.Extensibility;

public sealed class DocumentPostProcessorTests
{
    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenProcessorAddsVendorAnnotation_ChangesOnlySchemaArtifact ()
    {
        JsonContractGenerationResult baseline =
            GenerationTestHarness.Generate<SimpleContract>(
                "tests.vendor-extension");
        var processor = new TestDocumentPostProcessor(
            "tests.document.delivery",
            static _ =>
                new[]
                {
                    new JsonSchemaVendorExtension(
                        string.Empty,
                        "x-delivery",
                        JsonSerializer.SerializeToElement(
                            new { channel = "nuget" })),
                });

        JsonContractGenerationResult extended =
            GenerationTestHarness.Generate<SimpleContract>(
                "tests.vendor-extension",
                documentPostProcessors: new[] { processor });

        Assert.Equal(baseline.ContractDigest, extended.ContractDigest);
        Assert.Equal(
            baseline.GetTypeMetadataUtf8(),
            extended.GetTypeMetadataUtf8());
        Assert.NotEqual(
            baseline.GetJsonSchemaUtf8(),
            extended.GetJsonSchemaUtf8());

        using JsonDocument schema = JsonDocument.Parse(
            extended.GetJsonSchemaUtf8());
        Assert.Equal(
            "nuget",
            schema.RootElement
                .GetProperty("x-delivery")
                .GetProperty("channel")
                .GetString());
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenProcessorTargetsSchemaObjects_AppliesEveryVendorAnnotation ()
    {
        var processor = new TestDocumentPostProcessor(
            "tests.document.schema-targets",
            static context =>
            {
                Assert.False(
                    context.BaseDocument.TryGetProperty(
                        "x-document-post-processors",
                        out _));
                string definitionId = context.BaseDocument
                    .GetProperty("$defs")
                    .EnumerateObject()
                    .First()
                    .Name;

                return new[]
                {
                    Extension(string.Empty, "root"),
                    Extension("/properties/Mode", "property"),
                    Extension("/properties/Items/items", "items"),
                    Extension("/additionalProperties", "additionalProperties"),
                    Extension("/properties/Shape/oneOf/0", "oneOf"),
                    Extension(
                        "/$defs/" + EncodeJsonPointerSegment(definitionId),
                        "definition"),
                };
            });

        JsonContractGenerationResult result =
            GenerationTestHarness.Generate<StructuredContract>(
                "tests.document-schema-targets",
                documentPostProcessors: new[] { processor });

        using JsonDocument schema = JsonDocument.Parse(
            result.GetJsonSchemaUtf8());
        JsonElement root = schema.RootElement;
        Assert.Equal("root", root.GetProperty("x-target").GetString());
        Assert.Equal(
            "property",
            root.GetProperty("properties")
                .GetProperty("Mode")
                .GetProperty("x-target")
                .GetString());
        Assert.Equal(
            "items",
            root.GetProperty("properties")
                .GetProperty("Items")
                .GetProperty("items")
                .GetProperty("x-target")
                .GetString());
        Assert.Equal(
            "additionalProperties",
            root.GetProperty("additionalProperties")
                .GetProperty("x-target")
                .GetString());
        Assert.Equal(
            "oneOf",
            root.GetProperty("properties")
                .GetProperty("Shape")
                .GetProperty("oneOf")[0]
                .GetProperty("x-target")
                .GetString());
        Assert.Equal(
            "definition",
            root.GetProperty("$defs")
                .EnumerateObject()
                .Single(
                    static definition =>
                        definition.Value.TryGetProperty(
                            "x-target",
                            out _))
                .Value
                .GetProperty("x-target")
                .GetString());
    }

    [Theory]
    [InlineData("/properties")]
    [InlineData("/$defs")]
    [InlineData("/properties/Revision/minimum")]
    [InlineData("/properties/Mode/type")]
    [InlineData("/properties/Items/items/type")]
    [Trait("Size", "Small")]
    public void Generate_WhenProcessorTargetsNonSchemaValue_ReportsInvalidDocumentExtension (
        string targetPointer)
    {
        var processor = new TestDocumentPostProcessor(
            "tests.document.non-schema-target",
            _ =>
                new[]
                {
                    new JsonSchemaVendorExtension(
                        targetPointer,
                        "x-invalid-target",
                        JsonSerializer.SerializeToElement(true)),
                });

        JsonContractGenerationException exception =
            Assert.Throws<JsonContractGenerationException>(
                () => GenerationTestHarness.Generate<StructuredContract>(
                    "tests.invalid-document-target",
                    documentPostProcessors: new[] { processor }));

        Assert.Equal(
            JsonContractGenerationFailureKind.InvalidDocumentExtension,
            exception.FailureKind);
        Assert.Equal("tests.invalid-document-target", exception.ContractId);
        Assert.Equal(
            new[] { "tests.document.non-schema-target" },
            exception.SourceIds);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenProcessorIdentitiesDiffer_ChangesOnlySchemaBytesAndSha ()
    {
        JsonContractGenerationResult original =
            GenerateWithIdentity("tests.document.identity", "1");
        JsonContractGenerationResult changedId =
            GenerateWithIdentity("tests.document.changed-identity", "1");
        JsonContractGenerationResult changedVersion =
            GenerateWithIdentity("tests.document.identity", "2");

        Assert.Equal(original.ContractDigest, changedId.ContractDigest);
        Assert.Equal(original.ContractDigest, changedVersion.ContractDigest);
        Assert.Equal(
            original.GetTypeMetadataUtf8(),
            changedId.GetTypeMetadataUtf8());
        Assert.Equal(
            original.GetTypeMetadataUtf8(),
            changedVersion.GetTypeMetadataUtf8());

        Assert.NotEqual(
            original.GetJsonSchemaUtf8(),
            changedId.GetJsonSchemaUtf8());
        Assert.NotEqual(
            original.GetJsonSchemaUtf8(),
            changedVersion.GetJsonSchemaUtf8());
        Assert.NotEqual(
            ComputeSha256(original.GetJsonSchemaUtf8()),
            ComputeSha256(changedId.GetJsonSchemaUtf8()));
        Assert.NotEqual(
            ComputeSha256(original.GetJsonSchemaUtf8()),
            ComputeSha256(changedVersion.GetJsonSchemaUtf8()));
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WithProcessorsInAnyRegistrationOrder_EmitsUnicodeOrderedIdentities ()
    {
        var supplementaryPlane = new TestDocumentPostProcessor(
            "tests.document.\U00010000",
            static _ => Array.Empty<JsonSchemaVendorExtension>(),
            contractVersion: "2");
        var privateUse = new TestDocumentPostProcessor(
            "tests.document.\uE000",
            static _ => Array.Empty<JsonSchemaVendorExtension>(),
            contractVersion: "1");

        JsonContractGenerationResult reverseRegistration =
            GenerationTestHarness.Generate<SimpleContract>(
                "tests.document-identity-order",
                documentPostProcessors: new[]
                {
                    supplementaryPlane,
                    privateUse,
                });
        JsonContractGenerationResult orderedRegistration =
            GenerationTestHarness.Generate<SimpleContract>(
                "tests.document-identity-order",
                documentPostProcessors: new[]
                {
                    privateUse,
                    supplementaryPlane,
                });

        using JsonDocument schema = JsonDocument.Parse(
            reverseRegistration.GetJsonSchemaUtf8());
        Assert.Equal(
            reverseRegistration.GetJsonSchemaUtf8(),
            orderedRegistration.GetJsonSchemaUtf8());
        Assert.Collection(
            schema.RootElement
                .GetProperty("x-document-post-processors")
                .EnumerateArray(),
            first =>
            {
                Assert.Equal(
                    privateUse.StableId,
                    first.GetProperty("stableId").GetString());
                Assert.Equal(
                    privateUse.ContractVersion,
                    first.GetProperty("contractVersion").GetString());
            },
            second =>
            {
                Assert.Equal(
                    supplementaryPlane.StableId,
                    second.GetProperty("stableId").GetString());
                Assert.Equal(
                    supplementaryPlane.ContractVersion,
                    second.GetProperty("contractVersion").GetString());
            });
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenProcessorCollidesWithIdentityAnnotation_ReportsInvalidDocumentExtension ()
    {
        var processor = new TestDocumentPostProcessor(
            "tests.document.identity-collision",
            static _ =>
                new[]
                {
                    new JsonSchemaVendorExtension(
                        string.Empty,
                        "x-document-post-processors",
                        JsonSerializer.SerializeToElement(Array.Empty<object>())),
                });

        JsonContractGenerationException exception =
            Assert.Throws<JsonContractGenerationException>(
                () => GenerationTestHarness.Generate<SimpleContract>(
                    "tests.document-identity-collision",
                    documentPostProcessors: new[] { processor }));

        Assert.Equal(
            JsonContractGenerationFailureKind.InvalidDocumentExtension,
            exception.FailureKind);
        Assert.Equal(
            new[] { "tests.document.identity-collision" },
            exception.SourceIds);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenProcessorDeclaresStandardKeyword_ReportsInvalidDocumentExtension ()
    {
        var processor = new TestDocumentPostProcessor(
            "tests.document.invalid",
            static _ =>
                new[]
                {
                    new JsonSchemaVendorExtension(
                        string.Empty,
                        "description",
                        JsonSerializer.SerializeToElement("replacement")),
                });

        JsonContractGenerationException exception =
            Assert.Throws<JsonContractGenerationException>(
                () => GenerationTestHarness.Generate<SimpleContract>(
                    "tests.invalid-vendor-extension",
                    documentPostProcessors: new[] { processor }));

        Assert.Equal(
            JsonContractGenerationFailureKind.InvalidDocumentExtension,
            exception.FailureKind);
        Assert.Equal("tests.invalid-vendor-extension", exception.ContractId);
        Assert.Equal(
            new[] { "tests.document.invalid" },
            exception.SourceIds);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenProcessorReturnsUnreadableExtensionList_ReportsInvalidDocumentExtension ()
    {
        var processor = new TestDocumentPostProcessor(
            "tests.document.unreadable-list",
            static _ => new UnreadableVendorExtensionList());

        JsonContractGenerationException exception =
            Assert.Throws<JsonContractGenerationException>(
                () => GenerationTestHarness.Generate<SimpleContract>(
                    "tests.unreadable-vendor-extension-list",
                    documentPostProcessors: new[] { processor }));

        Assert.Equal(
            JsonContractGenerationFailureKind.InvalidDocumentExtension,
            exception.FailureKind);
        Assert.Equal(
            "tests.unreadable-vendor-extension-list",
            exception.ContractId);
        Assert.Equal(
            new[] { "tests.document.unreadable-list" },
            exception.SourceIds);
        Assert.IsType<InvalidOperationException>(exception.InnerException);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenProcessorsDisagree_ReportsConflict ()
    {
        TestDocumentPostProcessor alpha = DeliveryProcessor(
            "tests.document.alpha",
            "alpha");
        TestDocumentPostProcessor beta = DeliveryProcessor(
            "tests.document.beta",
            "beta");

        JsonContractGenerationException exception =
            Assert.Throws<JsonContractGenerationException>(
                () => GenerationTestHarness.Generate<SimpleContract>(
                    "tests.document-conflict",
                    documentPostProcessors: new[] { beta, alpha }));

        Assert.Equal(
            JsonContractGenerationFailureKind.DocumentExtensionConflict,
            exception.FailureKind);
        Assert.Equal("tests.document-conflict", exception.ContractId);
        Assert.Equal(
            new[] { "tests.document.alpha", "tests.document.beta" },
            exception.SourceIds);
    }

    private static JsonSchemaVendorExtension Extension (
        string targetPointer,
        string value)
    {
        return new JsonSchemaVendorExtension(
            targetPointer,
            "x-target",
            JsonSerializer.SerializeToElement(value));
    }

    private static JsonContractGenerationResult GenerateWithIdentity (
        string stableId,
        string contractVersion)
    {
        var processor = new TestDocumentPostProcessor(
            stableId,
            static _ => Array.Empty<JsonSchemaVendorExtension>(),
            contractVersion);

        return GenerationTestHarness.Generate<SimpleContract>(
            "tests.document-identity-artifact",
            documentPostProcessors: new[] { processor });
    }

    private static string ComputeSha256 (byte[] value)
    {
        return Convert.ToHexString(SHA256.HashData(value));
    }

    private static string EncodeJsonPointerSegment (string value)
    {
        return value
            .Replace("~", "~0", StringComparison.Ordinal)
            .Replace("/", "~1", StringComparison.Ordinal);
    }

    private static TestDocumentPostProcessor DeliveryProcessor (
        string stableId,
        string value)
    {
        return new TestDocumentPostProcessor(
            stableId,
            _ =>
                new[]
                {
                    new JsonSchemaVendorExtension(
                        string.Empty,
                        "x-delivery",
                        JsonSerializer.SerializeToElement(value)),
                });
    }

    private sealed class StructuredContract
    {
        public string Mode { get; set; } = string.Empty;

        public int Revision { get; set; }

        public int[] Items { get; set; } = Array.Empty<int>();

        public NestedContract Nested { get; set; } = new();

        public ShapeContract Shape { get; set; } = new CircleContract();

        [JsonExtensionData]
        public Dictionary<string, JsonElement> Additional { get; set; } =
            new();
    }

    [JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
    [JsonDerivedType(typeof(CircleContract), "circle")]
    private abstract class ShapeContract
    {
    }

    private sealed class CircleContract : ShapeContract
    {
        public int Radius { get; set; }
    }

    private sealed class NestedContract
    {
        public int Value { get; set; }
    }

    private sealed class SimpleContract
    {
        public string Value { get; set; } = string.Empty;
    }

    private sealed class UnreadableVendorExtensionList
        : IReadOnlyList<JsonSchemaVendorExtension>
    {
        public JsonSchemaVendorExtension this[int index] =>
            throw new InvalidOperationException(
                "The extension snapshot cannot be indexed.");

        public int Count =>
            throw new InvalidOperationException(
                "The extension snapshot count cannot be read.");

        public IEnumerator<JsonSchemaVendorExtension> GetEnumerator ()
        {
            throw new InvalidOperationException(
                "The extension snapshot cannot be enumerated.");
        }

        IEnumerator IEnumerable.GetEnumerator ()
        {
            return GetEnumerator();
        }
    }
}
