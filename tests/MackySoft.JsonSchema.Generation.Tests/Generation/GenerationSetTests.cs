using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using MackySoft.JsonSchema.Generation.Configuration;
using MackySoft.JsonSchema.Generation.Diagnostics;
using MackySoft.JsonSchema.Generation.Projection;

namespace MackySoft.JsonSchema.Generation.Tests.Generation;

public sealed class GenerationSetTests
{
    [Fact]
    [Trait("Size", "Small")]
    public void GenerateSet_OrdersResultsByContractId ()
    {
        JsonContractGenerator generator = CreateGenerator();

        IReadOnlyList<JsonContractGenerationResult> results =
            generator.GenerateSet(
                new[]
                {
                    CreateRequest<SecondContract>("tests.zulu"),
                    CreateRequest<FirstContract>("tests.alpha"),
                });

        Assert.Equal(
            new[] { "tests.alpha", "tests.zulu" },
            results.Select(static result => result.Model.ContractId));
    }

    [Fact]
    [Trait("Size", "Small")]
    public void GenerateSet_WhenContractIdIsDuplicated_ReportsTypedFailure ()
    {
        JsonContractGenerator generator = CreateGenerator();

        JsonContractGenerationException exception = Assert.Throws<
            JsonContractGenerationException>(
            () => generator.GenerateSet(
                new[]
                {
                    CreateRequest<FirstContract>("tests.duplicate"),
                    CreateRequest<SecondContract>("tests.duplicate"),
                }));

        Assert.Equal(
            JsonContractGenerationFailureKind.DuplicateContractId,
            exception.FailureKind);
        Assert.Equal("tests.duplicate", exception.ContractId);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenContractIdIsInvalid_ReportsTypedFailure ()
    {
        JsonContractGenerator generator = CreateGenerator();

        JsonContractGenerationException exception =
            Assert.Throws<JsonContractGenerationException>(
                () => generator.Generate(
                    CreateRequest<FirstContract>(" invalid")));

        Assert.Equal(
            JsonContractGenerationFailureKind.InvalidContractId,
            exception.FailureKind);
        Assert.Equal(" invalid", exception.ContractId);
        Assert.Equal(typeof(FirstContract), exception.TargetType);
    }

    private static JsonContractGenerator CreateGenerator ()
    {
        return new JsonContractGenerator(
            new JsonContractGeneratorOptions(
                JsonContractGenerationSettings.ClosedObjects));
    }

    private static JsonContractGenerationRequest CreateRequest<TContract> (
        string contractId)
    {
        return new JsonContractGenerationRequest(
            contractId,
            typeof(TContract),
            new JsonSerializerOptions
            {
                UnmappedMemberHandling =
                    JsonUnmappedMemberHandling.Disallow,
            },
            new DefaultJsonTypeInfoResolver(),
            new JsonSchemaDocumentOptions(
                JsonSchemaDocumentKind.Complete,
                id: null,
                logicalName: null));
    }

    private sealed class FirstContract
    {
        public int Value { get; set; }
    }

    private sealed class SecondContract
    {
        public string Value { get; set; } = string.Empty;
    }
}
