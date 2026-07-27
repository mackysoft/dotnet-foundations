using System.Text.Json;
using System.Text.Json.Serialization;
using MackySoft.JsonSchema.Generation.Annotations;
using MackySoft.JsonSchema.Generation.ContractModel;
using MackySoft.JsonSchema.Generation.Diagnostics;
using MackySoft.JsonSchema.Generation.Metadata;
using MackySoft.JsonSchema.Generation.Tests.Fixtures;

namespace MackySoft.JsonSchema.Generation.Tests.Generation;

public sealed class SystemTextJsonContractTests
{
    [Fact]
    [Trait("Size", "Small")]
    public void Generate_UsesSerializerNamesIgnoreRulesRequirednessAndNullability ()
    {
        var serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            UnmappedMemberHandling =
                JsonUnmappedMemberHandling.Disallow,
        };

        JsonContractGenerationResult result =
            GenerationTestHarness.Generate<SerializerContract>(
                "tests.serializer-contract",
                serializerOptions);

        JsonContractNode root = result.Model.Root;
        Assert.Equal(JsonContractNodeKind.Object, root.Kind);
        Assert.Equal(
            new[] { "optionalDisplayName", "serverName", "wire_id" },
            root.Properties.Select(static property => property.Name));

        JsonContractProperty optional = GenerationTestHarness.GetProperty(
            root,
            "optionalDisplayName");
        Assert.False(optional.IsRequired);
        Assert.True(optional.Value.IsNullable);

        JsonContractProperty serializerRequired = GenerationTestHarness.GetProperty(
            root,
            "serverName");
        Assert.True(serializerRequired.IsRequired);
        Assert.False(serializerRequired.Value.IsNullable);

        JsonContractProperty contractRequired = GenerationTestHarness.GetProperty(
            root,
            "wire_id");
        Assert.True(contractRequired.IsRequired);
        Assert.False(contractRequired.Value.IsNullable);
        Assert.DoesNotContain(
            root.Properties,
            static property => property.Name == "ignoredValue");

        using JsonDocument schema = JsonDocument.Parse(result.GetJsonSchemaUtf8());
        JsonElement schemaRoot = schema.RootElement;
        JsonElement schemaProperties = schemaRoot.GetProperty("properties");
        Assert.True(schemaProperties.TryGetProperty("optionalDisplayName", out _));
        Assert.True(schemaProperties.TryGetProperty("serverName", out _));
        Assert.True(schemaProperties.TryGetProperty("wire_id", out _));
        Assert.False(schemaProperties.TryGetProperty("ignoredValue", out _));
        Assert.Equal(
            new[] { "serverName", "wire_id" },
            schemaRoot
                .GetProperty("required")
                .EnumerateArray()
                .Select(static value => value.GetString()));

        using JsonDocument typeMetadata = JsonDocument.Parse(
            result.GetTypeMetadataUtf8());
        JsonElement metadataRoot = typeMetadata.RootElement.GetProperty("root");
        JsonElement optionalMetadata =
            GenerationTestHarness.GetTypeMetadataProperty(
                metadataRoot,
                "optionalDisplayName");
        Assert.False(optionalMetadata.GetProperty("isRequired").GetBoolean());
        Assert.True(
            optionalMetadata
                .GetProperty("value")
                .GetProperty("isNullable")
                .GetBoolean());
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenRequiredMetadataDisagreesWithSerializer_ReportsConflict ()
    {
        JsonContractGenerationException exception =
            Assert.Throws<JsonContractGenerationException>(
                () => GenerationTestHarness.Generate<
                    RequiredMetadataConflictContract>(
                        "tests.required-metadata-conflict"));

        Assert.Equal(
            JsonContractGenerationFailureKind.ConflictingMetadata,
            exception.FailureKind);
        Assert.Equal(typeof(int), exception.TargetType);
        Assert.Equal("Value", exception.JsonPropertyName);
        Assert.Equal(JsonContractMetadataKind.Required, exception.MetadataKind);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenAllowNullMetadataDisagreesWithClrNullability_ReportsConflict ()
    {
        JsonContractGenerationException exception =
            Assert.Throws<JsonContractGenerationException>(
                () => GenerationTestHarness.Generate<
                    NullMetadataConflictContract>(
                        "tests.null-metadata-conflict"));

        Assert.Equal(
            JsonContractGenerationFailureKind.ConflictingMetadata,
            exception.FailureKind);
        Assert.Equal(typeof(string), exception.TargetType);
        Assert.Equal("Value", exception.JsonPropertyName);
        Assert.Equal(JsonContractMetadataKind.AllowNull, exception.MetadataKind);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_TypeAllowNullMakesReferenceRootNullableInBothProjections ()
    {
        JsonContractGenerationResult result =
            GenerationTestHarness.Generate<NullableRootContract>(
                "tests.nullable-root");

        Assert.True(result.Model.Root.IsNullable);

        using JsonDocument schema = JsonDocument.Parse(result.GetJsonSchemaUtf8());
        Assert.Equal(
            new[] { "object", "null" },
            schema.RootElement
                .GetProperty("type")
                .EnumerateArray()
                .Select(static value => value.GetString()));

        using JsonDocument typeMetadata = JsonDocument.Parse(
            result.GetTypeMetadataUtf8());
        Assert.True(
            typeMetadata.RootElement
                .GetProperty("root")
                .GetProperty("isNullable")
                .GetBoolean());
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_WhenObjectMemberHasUnmappedConverter_ReportsUnsupportedConverter ()
    {
        JsonContractGenerationException exception =
            Assert.Throws<JsonContractGenerationException>(
                () => GenerationTestHarness.Generate<
                    ObjectConverterContract>(
                        "tests.object-member-converter"));

        Assert.Equal(
            JsonContractGenerationFailureKind.UnsupportedConverter,
            exception.FailureKind);
        Assert.Equal(typeof(ObjectValue), exception.TargetType);
        Assert.Equal("Value", exception.JsonPropertyName);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_IntegerContractsCarrySerializerBoundsIntoBothProjections ()
    {
        JsonContractGenerationResult result =
            GenerationTestHarness.Generate<IntegerContract>(
                "tests.integer-bounds");

        (string Name, string Minimum, string Maximum)[] expected =
        {
            ("Byte", "0", "255"),
            ("Int16", "-32768", "32767"),
            ("Int32", "-2147483648", "2147483647"),
            ("Int64", "-9223372036854775808", "9223372036854775807"),
            ("SByte", "-128", "127"),
            ("UInt16", "0", "65535"),
            ("UInt32", "0", "4294967295"),
            ("UInt64", "0", "18446744073709551615"),
        };

        using JsonDocument schema = JsonDocument.Parse(result.GetJsonSchemaUtf8());
        using JsonDocument typeMetadata = JsonDocument.Parse(
            result.GetTypeMetadataUtf8());
        JsonElement schemaProperties =
            schema.RootElement.GetProperty("properties");
        JsonElement metadataRoot =
            typeMetadata.RootElement.GetProperty("root");

        foreach ((string name, string minimum, string maximum) in expected)
        {
            JsonContractNode modelValue =
                GenerationTestHarness.GetProperty(result.Model.Root, name).Value;
            Assert.Equal(minimum, modelValue.Constraints.Minimum?.GetRawText());
            Assert.Equal(maximum, modelValue.Constraints.Maximum?.GetRawText());

            JsonElement schemaValue = schemaProperties.GetProperty(name);
            Assert.Equal(
                minimum,
                schemaValue.GetProperty("minimum").GetRawText());
            Assert.Equal(
                maximum,
                schemaValue.GetProperty("maximum").GetRawText());

            JsonElement metadataValue =
                GenerationTestHarness.GetTypeMetadataProperty(
                        metadataRoot,
                        name)
                    .GetProperty("value")
                    .GetProperty("constraints");
            Assert.Equal(
                minimum,
                metadataValue.GetProperty("minimum").GetRawText());
            Assert.Equal(
                maximum,
                metadataValue.GetProperty("maximum").GetRawText());
        }
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_DecimalContractCarriesExactSerializerBoundsIntoBothProjections ()
    {
        const string Minimum = "-79228162514264337593543950335";
        const string Maximum = "79228162514264337593543950335";
        JsonContractGenerationResult result =
            GenerationTestHarness.Generate<DecimalContract>(
                "tests.decimal-bounds");

        JsonContractNode modelValue = GenerationTestHarness
            .GetProperty(result.Model.Root, "Value")
            .Value;
        Assert.Equal(Minimum, modelValue.Constraints.Minimum?.GetRawText());
        Assert.Equal(Maximum, modelValue.Constraints.Maximum?.GetRawText());

        using JsonDocument schema = JsonDocument.Parse(
            result.GetJsonSchemaUtf8());
        JsonElement schemaValue = schema.RootElement
            .GetProperty("properties")
            .GetProperty("Value");
        Assert.Equal(
            Minimum,
            schemaValue.GetProperty("minimum").GetRawText());
        Assert.Equal(
            Maximum,
            schemaValue.GetProperty("maximum").GetRawText());

        using JsonDocument typeMetadata = JsonDocument.Parse(
            result.GetTypeMetadataUtf8());
        JsonElement metadataValue = GenerationTestHarness
            .GetTypeMetadataProperty(
                typeMetadata.RootElement.GetProperty("root"),
                "Value")
            .GetProperty("value")
            .GetProperty("constraints");
        Assert.Equal(
            Minimum,
            metadataValue.GetProperty("minimum").GetRawText());
        Assert.Equal(
            Maximum,
            metadataValue.GetProperty("maximum").GetRawText());
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_FloatingPointContractsCarrySerializerBoundsIntoBothProjections ()
    {
        JsonContractGenerationResult result =
            GenerationTestHarness.Generate<FloatingPointContract>(
                "tests.floating-point-bounds");
        (string Name, string Minimum, string Maximum)[] expected =
        {
            ("Single", "-3.4028235E+38", "3.4028235E+38"),
            (
                "Double",
                "-1.7976931348623157E+308",
                "1.7976931348623157E+308"
            ),
        };

        using JsonDocument schema = JsonDocument.Parse(
            result.GetJsonSchemaUtf8());
        using JsonDocument typeMetadata = JsonDocument.Parse(
            result.GetTypeMetadataUtf8());
        foreach ((string name, string minimum, string maximum) in expected)
        {
            JsonContractNode modelValue = GenerationTestHarness
                .GetProperty(result.Model.Root, name)
                .Value;
            JsonElement schemaValue = schema.RootElement
                .GetProperty("properties")
                .GetProperty(name);
            JsonElement metadataValue = GenerationTestHarness
                .GetTypeMetadataProperty(
                    typeMetadata.RootElement.GetProperty("root"),
                    name)
                .GetProperty("value")
                .GetProperty("constraints");

            Assert.Equal(minimum, modelValue.Constraints.Minimum?.GetRawText());
            Assert.Equal(maximum, modelValue.Constraints.Maximum?.GetRawText());
            Assert.Equal(
                minimum,
                schemaValue.GetProperty("minimum").GetRawText());
            Assert.Equal(
                maximum,
                schemaValue.GetProperty("maximum").GetRawText());
            Assert.Equal(
                minimum,
                metadataValue.GetProperty("minimum").GetRawText());
            Assert.Equal(
                maximum,
                metadataValue.GetProperty("maximum").GetRawText());
        }
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_ExplicitIntegerRangeNarrowsSerializerBounds ()
    {
        JsonContractGenerationResult result =
            GenerationTestHarness.Generate<NarrowIntegerContract>(
                "tests.narrow-integer-bounds");

        JsonContractConstraints constraints = GenerationTestHarness
            .GetProperty(result.Model.Root, "Value")
            .Value
            .Constraints;
        Assert.Equal("1", constraints.Minimum?.GetRawText());
        Assert.Equal("254", constraints.Maximum?.GetRawText());
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Generate_ExplicitIntegerRangeCannotExtendSerializerBounds ()
    {
        JsonContractGenerationException exception =
            Assert.Throws<JsonContractGenerationException>(
                () => GenerationTestHarness.Generate<
                    WidenedIntegerContract>(
                        "tests.widened-integer-bounds"));

        Assert.Equal(
            JsonContractGenerationFailureKind.InvalidMetadataValue,
            exception.FailureKind);
        Assert.Equal(typeof(byte), exception.TargetType);
        Assert.Equal("Value", exception.JsonPropertyName);
        Assert.Equal(JsonContractMetadataKind.Minimum, exception.MetadataKind);
    }

    private sealed class SerializerContract
    {
        [JsonContractAllowNull]
        public string? OptionalDisplayName { get; set; }

        [JsonRequired]
        public string ServerName { get; set; } = string.Empty;

        [JsonRequired]
        [JsonContractRequired]
        [JsonPropertyName("wire_id")]
        public int WireIdentifier { get; set; }

        [JsonIgnore]
        public string IgnoredValue { get; set; } = string.Empty;
    }

    private sealed class RequiredMetadataConflictContract
    {
        [JsonContractRequired]
        public int Value { get; set; }
    }

    private sealed class NullMetadataConflictContract
    {
        [JsonContractAllowNull]
        public string Value { get; set; } = string.Empty;
    }

    [JsonContractAllowNull]
    private sealed class NullableRootContract
    {
        public int Value { get; set; }
    }

    private sealed class ObjectConverterContract
    {
        [JsonConverter(typeof(ObjectValueConverter))]
        public ObjectValue Value { get; set; } = new();
    }

    private sealed class ObjectValue
    {
        public string Text { get; set; } = string.Empty;
    }

    private sealed class ObjectValueConverter : JsonConverter<ObjectValue>
    {
        public override ObjectValue Read (
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            return new ObjectValue
            {
                Text = reader.GetString() ?? string.Empty,
            };
        }

        public override void Write (
            Utf8JsonWriter writer,
            ObjectValue value,
            JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.Text);
        }
    }

    private sealed class IntegerContract
    {
        public byte Byte { get; set; }

        public short Int16 { get; set; }

        public int Int32 { get; set; }

        public long Int64 { get; set; }

        public sbyte SByte { get; set; }

        public ushort UInt16 { get; set; }

        public uint UInt32 { get; set; }

        public ulong UInt64 { get; set; }
    }

    private sealed class DecimalContract
    {
        public decimal Value { get; set; }
    }

    private sealed class FloatingPointContract
    {
        public float Single { get; set; }

        public double Double { get; set; }
    }

    private sealed class NarrowIntegerContract
    {
        [JsonContractRange("1", "254")]
        public byte Value { get; set; }
    }

    private sealed class WidenedIntegerContract
    {
        [JsonContractRange("-1", "255")]
        public byte Value { get; set; }
    }
}
