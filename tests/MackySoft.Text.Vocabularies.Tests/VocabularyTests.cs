namespace MackySoft.Text.Vocabularies.Tests;

public sealed class VocabularyTests
{
    private static readonly InvalidDefinitionCase[] InvalidDefinitionCases =
    [
        new("missing text", static () => Vocabulary.GetEntries<MissingTextVocabulary>()),
        new("empty text", static () => Vocabulary.GetEntries<EmptyTextVocabulary>()),
        new("whitespace-only text", static () => Vocabulary.GetEntries<WhitespaceOnlyTextVocabulary>()),
        new("leading whitespace", static () => Vocabulary.GetEntries<LeadingWhitespaceTextVocabulary>()),
        new("trailing whitespace", static () => Vocabulary.GetEntries<TrailingWhitespaceTextVocabulary>()),
        new("duplicate text", static () => Vocabulary.GetEntries<DuplicateTextVocabulary>()),
        new("duplicate value", static () => Vocabulary.GetEntries<DuplicateValueVocabulary>()),
        new("empty definition", static () => Vocabulary.GetEntries<EmptyVocabulary>()),
    ];

    private static readonly string?[] NonCanonicalTexts =
    [
        null,
        "",
        "first ",
        "FIRST",
        "unknown",
    ];

    [Fact]
    [Trait("Size", "Small")]
    public void GetText_WhenValueIsDeclared_ReturnsCanonicalText ()
    {
        Assert.Equal("first", Vocabulary.GetText(OrderedVocabulary.First));
    }

    [Fact]
    [Trait("Size", "Small")]
    public void TryGetText_WhenValueIsDeclared_ReturnsCanonicalText ()
    {
        var result = Vocabulary.TryGetText(OrderedVocabulary.Second, out var text);

        Assert.True(result);
        Assert.Equal("secondValue", text);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void GetValue_WhenTextExactlyMatches_ReturnsTypedValue ()
    {
        Assert.Equal(OrderedVocabulary.Third, Vocabulary.GetValue<OrderedVocabulary>("third"));
    }

    [Fact]
    [Trait("Size", "Small")]
    public void TryGetValue_WhenTextExactlyMatches_ReturnsTypedValue ()
    {
        var result = Vocabulary.TryGetValue("secondValue", out OrderedVocabulary value);

        Assert.True(result);
        Assert.Equal(OrderedVocabulary.Second, value);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void TryGetValue_WhenTextDoesNotExactlyMatch_ReturnsFalse ()
    {
        foreach (string? text in NonCanonicalTexts)
        {
            var result = Vocabulary.TryGetValue<OrderedVocabulary>(text, out var value);

            Assert.False(result);
            Assert.Equal(default, value);
        }
    }

    [Fact]
    [Trait("Size", "Small")]
    public void IsDefined_ReturnsWhetherTextAndValueAreDeclared ()
    {
        Assert.True(Vocabulary.IsDefined<OrderedVocabulary>("first"));
        Assert.True(Vocabulary.IsDefined(OrderedVocabulary.First));
        Assert.False(Vocabulary.IsDefined<OrderedVocabulary>("FIRST"));
        Assert.False(Vocabulary.IsDefined((OrderedVocabulary)999));
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Matches_ReturnsWhetherTextMapsToExpectedValue ()
    {
        Assert.True(Vocabulary.Matches("first", OrderedVocabulary.First));
        Assert.False(Vocabulary.Matches("first", OrderedVocabulary.Second));
        Assert.False(Vocabulary.Matches("FIRST", OrderedVocabulary.First));
    }

    [Fact]
    [Trait("Size", "Small")]
    public void GetEntries_ReturnsTypedMappingsInDeclarationOrder ()
    {
        IReadOnlyList<VocabularyEntry<OrderedVocabulary>> entries = Vocabulary.GetEntries<OrderedVocabulary>();

        Assert.Collection(
            entries,
            static entry =>
            {
                Assert.Equal(OrderedVocabulary.First, entry.Value);
                Assert.Equal("first", entry.Text);
            },
            static entry =>
            {
                Assert.Equal(OrderedVocabulary.Second, entry.Value);
                Assert.Equal("secondValue", entry.Text);
            },
            static entry =>
            {
                Assert.Equal(OrderedVocabulary.Third, entry.Value);
                Assert.Equal("third", entry.Text);
            });
    }

    [Fact]
    [Trait("Size", "Small")]
    public void GetTexts_ReturnsCanonicalTextsInDeclarationOrder ()
    {
        Assert.Equal(
            ["first", "secondValue", "third"],
            Vocabulary.GetTexts<OrderedVocabulary>());
    }

    [Fact]
    [Trait("Size", "Small")]
    public void GetText_WhenValueIsUndeclared_ThrowsArgumentOutOfRangeException ()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            static () => Vocabulary.GetText((OrderedVocabulary)999));
    }

    [Fact]
    [Trait("Size", "Small")]
    public void TryGetText_WhenValueIsUndeclared_ReturnsFalse ()
    {
        var result = Vocabulary.TryGetText((OrderedVocabulary)999, out var text);

        Assert.False(result);
        Assert.Null(text);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void GetValue_WhenTextIsUnknown_ThrowsArgumentException ()
    {
        Assert.Throws<ArgumentException>(
            static () => Vocabulary.GetValue<OrderedVocabulary>("unknown"));
    }

    [Fact]
    [Trait("Size", "Small")]
    public void GetValue_WhenTextIsNull_ThrowsArgumentNullException ()
    {
        Assert.Throws<ArgumentNullException>(
            static () => Vocabulary.GetValue<OrderedVocabulary>(null!));
    }

    [Fact]
    [Trait("Size", "Small")]
    public void IsVocabulary_WhenTypeDeclaresValidVocabulary_ReturnsTrue ()
    {
        Assert.True(Vocabulary.IsVocabulary(typeof(OrderedVocabulary)));
    }

    [Theory]
    [InlineData(typeof(UnmarkedEnum))]
    [InlineData(typeof(string))]
    [Trait("Size", "Small")]
    public void IsVocabulary_WhenTypeDoesNotDeclareVocabulary_ReturnsFalse (Type type)
    {
        Assert.False(Vocabulary.IsVocabulary(type));
    }

    [Fact]
    [Trait("Size", "Small")]
    public void IsVocabulary_WhenTypeIsNull_ThrowsArgumentNullException ()
    {
        Assert.Throws<ArgumentNullException>(
            static () => Vocabulary.IsVocabulary(null!));
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Validate_WhenDefinitionIsValid_Completes ()
    {
        Vocabulary.Validate(typeof(OrderedVocabulary));
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Validate_WhenTypeDoesNotDeclareVocabulary_ThrowsArgumentException ()
    {
        Assert.Throws<ArgumentException>(
            static () => Vocabulary.Validate(typeof(UnmarkedEnum)));
    }

    [Fact]
    [Trait("Size", "Small")]
    public void GetEntries_WhenDefinitionIsInvalid_ThrowsInvalidOperationException ()
    {
        foreach (var testCase in InvalidDefinitionCases)
        {
            var exception = Record.Exception(testCase.GetEntries);

            Assert.IsType<InvalidOperationException>(exception);
        }
    }

    [Fact]
    [Trait("Size", "Small")]
    public void IsVocabulary_WhenDeclaredDefinitionIsInvalid_ThrowsInvalidOperationException ()
    {
        Assert.Throws<InvalidOperationException>(
            static () => Vocabulary.IsVocabulary(typeof(MissingTextVocabulary)));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData(" text")]
    [InlineData("text ")]
    [Trait("Size", "Small")]
    public void VocabularyTextAttribute_WhenTextIsInvalid_ThrowsArgumentException (string text)
    {
        Assert.Throws<ArgumentException>(() => new VocabularyTextAttribute(text));
    }

    [Fact]
    [Trait("Size", "Small")]
    public void VocabularyTextAttribute_WhenTextIsNull_ThrowsArgumentNullException ()
    {
        Assert.Throws<ArgumentNullException>(
            static () => new VocabularyTextAttribute(null!));
    }

    [Fact]
    [Trait("Size", "Small")]
    public void VocabularyTextAttribute_WhenTextHasInternalWhitespace_PreservesText ()
    {
        var attribute = new VocabularyTextAttribute("two words");

        Assert.Equal("two words", attribute.Text);
    }

    [VocabularyDefinition]
    private enum OrderedVocabulary
    {
        [VocabularyText("first")]
        First = 20,

        [VocabularyText("secondValue")]
        Second = 5,

        [VocabularyText("third")]
        Third = 10,
    }

    private enum UnmarkedEnum
    {
        Value = 0,
    }

    [VocabularyDefinition]
    private enum MissingTextVocabulary
    {
        Value = 0,
    }

    [VocabularyDefinition]
    private enum EmptyTextVocabulary
    {
        [VocabularyText("")]
        Value = 0,
    }

    [VocabularyDefinition]
    private enum WhitespaceOnlyTextVocabulary
    {
        [VocabularyText(" ")]
        Value = 0,
    }

    [VocabularyDefinition]
    private enum LeadingWhitespaceTextVocabulary
    {
        [VocabularyText(" value")]
        Value = 0,
    }

    [VocabularyDefinition]
    private enum TrailingWhitespaceTextVocabulary
    {
        [VocabularyText("value ")]
        Value = 0,
    }

    [VocabularyDefinition]
    private enum DuplicateTextVocabulary
    {
        [VocabularyText("value")]
        First = 0,

        [VocabularyText("value")]
        Second = 1,
    }

    [VocabularyDefinition]
    private enum DuplicateValueVocabulary
    {
        [VocabularyText("first")]
        First = 0,

        [VocabularyText("second")]
        Second = 0,
    }

    [VocabularyDefinition]
    private enum EmptyVocabulary
    {
    }

    private sealed record InvalidDefinitionCase (
        string Name,
        Action GetEntries);
}
