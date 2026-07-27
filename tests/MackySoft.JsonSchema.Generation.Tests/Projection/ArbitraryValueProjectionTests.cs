using System.Text.Json;
using System.Text.Json.Serialization;
using MackySoft.JsonSchema.Generation.Annotations;
using MackySoft.JsonSchema.Generation.ContractModel;
using MackySoft.JsonSchema.Generation.Tests.Fixtures;

namespace MackySoft.JsonSchema.Generation.Tests.Projection;

public sealed class ArbitraryValueProjectionTests
{
    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenMemberAllowsAnyJson_LeavesItsValueShapeUnconstrained ()
    {
        JsonContractGenerationResult result =
            GenerationTestHarness.Generate<ArbitraryContract>(
                "tests.arbitrary-contract",
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    UnmappedMemberHandling =
                        JsonUnmappedMemberHandling.Disallow,
                });

        JsonContractNode payload = GenerationTestHarness
            .GetProperty(result.Model.Root, "payload")
            .Value;
        Assert.Equal(JsonContractNodeKind.Arbitrary, payload.Kind);

        using JsonDocument schema = JsonDocument.Parse(result.GetJsonSchemaUtf8());
        JsonElement payloadSchema = schema.RootElement
            .GetProperty("properties")
            .GetProperty("payload");
        Assert.Empty(payloadSchema.EnumerateObject());

        using JsonDocument typeMetadata = JsonDocument.Parse(
            result.GetTypeMetadataUtf8());
        JsonElement payloadMetadata =
            GenerationTestHarness.GetTypeMetadataProperty(
                typeMetadata.RootElement.GetProperty("root"),
                "payload");
        Assert.Equal(
            "arbitrary",
            payloadMetadata
                .GetProperty("value")
                .GetProperty("kind")
                .GetString());
    }

    private sealed class ArbitraryContract
    {
        [AnyValue]
        public JsonElement Payload { get; set; }
    }
}
