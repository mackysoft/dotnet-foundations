using System.Text.Json;

namespace MackySoft.Text.Vocabularies.Json.Tests;

public sealed class VocabularyJsonConverterFactoryTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters =
        {
            new VocabularyJsonConverterFactory(),
        },
    };

    [Fact]
    [Trait("Size", "Small")]
    public void Serialize_WhenValueIsDeclared_WritesCanonicalString ()
    {
        var json = JsonSerializer.Serialize(SampleVocabulary.Second, Options);

        Assert.Equal("\"secondValue\"", json);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Deserialize_WhenStringIsCanonical_ReturnsTypedValue ()
    {
        var value = JsonSerializer.Deserialize<SampleVocabulary>("\"first\"", Options);

        Assert.Equal(SampleVocabulary.First, value);
    }

    [Theory]
    [InlineData("\"FIRST\"")]
    [InlineData("\" first \"")]
    [InlineData("\"unknown\"")]
    [InlineData("0")]
    [InlineData("true")]
    [InlineData("{}")]
    [InlineData("[]")]
    [Trait("Size", "Small")]
    public void Deserialize_WhenJsonDoesNotContainCanonicalString_ThrowsJsonException (string json)
    {
        Assert.Throws<JsonException>(
            () => JsonSerializer.Deserialize<SampleVocabulary>(json, Options));
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Serialize_WhenValueIsUndeclared_ThrowsJsonException ()
    {
        Assert.Throws<JsonException>(
            static () => JsonSerializer.Serialize((SampleVocabulary)999, Options));
    }

    [Fact]
    [Trait("Size", "Small")]
    public void SerializeDictionary_WhenKeyIsDeclared_WritesCanonicalPropertyName ()
    {
        var values = new Dictionary<SampleVocabulary, int>
        {
            [SampleVocabulary.Second] = 1,
        };

        var json = JsonSerializer.Serialize(values, Options);

        Assert.Equal("{\"secondValue\":1}", json);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void DeserializeDictionary_WhenPropertyNameIsCanonical_ReturnsTypedKey ()
    {
        var values = JsonSerializer.Deserialize<Dictionary<SampleVocabulary, int>>(
            "{\"first\":2}",
            Options);

        Assert.NotNull(values);
        Assert.Single(values);
        Assert.Equal(2, values[SampleVocabulary.First]);
    }

    [Theory]
    [InlineData("{\"FIRST\":1}")]
    [InlineData("{\" first \":1}")]
    [InlineData("{\"unknown\":1}")]
    [Trait("Size", "Small")]
    public void DeserializeDictionary_WhenPropertyNameIsNotCanonical_ThrowsJsonException (string json)
    {
        Assert.Throws<JsonException>(
            () => JsonSerializer.Deserialize<Dictionary<SampleVocabulary, int>>(json, Options));
    }

    [Fact]
    [Trait("Size", "Small")]
    public void SerializeDictionary_WhenKeyIsUndeclared_ThrowsJsonException ()
    {
        var values = new Dictionary<SampleVocabulary, int>
        {
            [(SampleVocabulary)999] = 1,
        };

        Assert.Throws<JsonException>(
            () => JsonSerializer.Serialize(values, Options));
    }

    [Fact]
    [Trait("Size", "Small")]
    public void CanConvert_WhenTypeDeclaresVocabulary_ReturnsTrue ()
    {
        var factory = new VocabularyJsonConverterFactory();

        Assert.True(factory.CanConvert(typeof(SampleVocabulary)));
    }

    [Theory]
    [InlineData(typeof(PlainEnum))]
    [InlineData(typeof(string))]
    [Trait("Size", "Small")]
    public void CanConvert_WhenTypeDoesNotDeclareVocabulary_ReturnsFalse (Type type)
    {
        var factory = new VocabularyJsonConverterFactory();

        Assert.False(factory.CanConvert(type));
    }

    [Fact]
    [Trait("Size", "Small")]
    public void CanConvert_WhenDefinitionIsInvalid_ThrowsInvalidOperationException ()
    {
        var factory = new VocabularyJsonConverterFactory();

        Assert.Throws<InvalidOperationException>(
            () => factory.CanConvert(typeof(IncompleteVocabulary)));
    }

    [VocabularyDefinition]
    private enum SampleVocabulary
    {
        [VocabularyText("first")]
        First = 0,

        [VocabularyText("secondValue")]
        Second = 1,
    }

    private enum PlainEnum
    {
        Value = 0,
    }

    [VocabularyDefinition]
    private enum IncompleteVocabulary
    {
        [VocabularyText("first")]
        First = 0,

        Second = 1,
    }
}
