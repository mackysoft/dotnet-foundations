using System.Text;
using System.Text.Json;

namespace MackySoft.Json.Canonicalization.Tests;

public sealed class Rfc8785JsonCanonicalizerContractTests
{
    public static TheoryData<string> InvalidRawJsonInputs => new()
    {
        { "" },
        { " \t\r\n" },
        { "\uFEFF{}" },
        { "/* comment */{}" },
        { "[1,]" },
        { "{} {}" },
        { "{" },
        { "NaN" },
        { "Infinity" },
        { "\u00A0{}" },
    };

    public static TheoryData<string, JsonCanonicalizationFailureKind> SemanticViolationInputs => new()
    {
        {
            """
            {"outer":{"value":1,"\u0076alue":2}}
            """,
            JsonCanonicalizationFailureKind.DuplicateProperty
        },
        {
            """
            {"value":"\ud800"}
            """,
            JsonCanonicalizationFailureKind.InvalidUnicode
        },
        {
            """
            {"\udfff":true}
            """,
            JsonCanonicalizationFailureKind.InvalidUnicode
        },
        { "1e400", JsonCanonicalizationFailureKind.NumberNotRepresentable },
        { "-0", JsonCanonicalizationFailureKind.NegativeZero },
    };

    [Theory]
    [Trait("Size", "Small")]
    [MemberData(nameof(InvalidRawJsonInputs))]
    public void Canonicalize_ThrowsInvalidJson_ForForbiddenRawJsonSyntax (string json)
    {
        AssertRawFailure(
            Encoding.UTF8.GetBytes(json),
            JsonCanonicalizationFailureKind.InvalidJson);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Canonicalize_ThrowsInvalidUnicode_ForInvalidUtf8 ()
    {
        byte[] invalidUtf8Json = [0x22, 0xc3, 0x28, 0x22];

        AssertRawFailure(
            invalidUtf8Json,
            JsonCanonicalizationFailureKind.InvalidUnicode);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Canonicalize_ReturnsSameBytes_ForEquivalentRawJsonRepresentations ()
    {
        byte[] expected = """{"a":[true],"b":1}"""u8.ToArray();
        byte[] withWhitespaceAndDecimal = Encoding.UTF8.GetBytes(
            " \t\r\n { \r\n \"b\" \t : \t 1.0, \"a\": [true] \r\n } \t");
        byte[] reorderedWithExponent = """{"a":[true],"b":1e0}"""u8.ToArray();
        byte[] reorderedWithInteger = """{"b":1,"a":[true]}"""u8.ToArray();

        byte[] firstResult = Rfc8785JsonCanonicalizer.Canonicalize(withWhitespaceAndDecimal);
        byte[] secondResult = Rfc8785JsonCanonicalizer.Canonicalize(reorderedWithExponent);
        byte[] thirdResult = Rfc8785JsonCanonicalizer.Canonicalize(reorderedWithInteger);

        Assert.Equal(expected, firstResult);
        Assert.Equal(expected, secondResult);
        Assert.Equal(expected, thirdResult);
    }

    [Theory]
    [Trait("Size", "Small")]
    [MemberData(nameof(SemanticViolationInputs))]
    public void Canonicalize_ThrowsClassifiedFailure_ForSemanticViolation (
        string json,
        JsonCanonicalizationFailureKind expectedFailureKind)
    {
        AssertRawFailure(Encoding.UTF8.GetBytes(json), expectedFailureKind);

        using JsonDocument document = JsonDocument.Parse(
            json,
            new JsonDocumentOptions
            {
                MaxDepth = 128,
            });

        JsonCanonicalizationException exception = Assert.Throws<JsonCanonicalizationException>(
            () => Rfc8785JsonCanonicalizer.Canonicalize(document.RootElement));

        Assert.Equal(expectedFailureKind, exception.FailureKind);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Canonicalize_ThrowsNegativeZero_WhenNonzeroDecimalUnderflowsToBinary64NegativeZero ()
    {
        const string json = "-1e-400";
        AssertRawFailure(
            Encoding.UTF8.GetBytes(json),
            JsonCanonicalizationFailureKind.NegativeZero);

        using JsonDocument document = JsonDocument.Parse(json);

        JsonCanonicalizationException exception = Assert.Throws<JsonCanonicalizationException>(
            () => Rfc8785JsonCanonicalizer.Canonicalize(document.RootElement));

        Assert.Equal(JsonCanonicalizationFailureKind.NegativeZero, exception.FailureKind);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Canonicalize_ThrowsMaximumDepth_ForRawUtf8BeyondMaximumDepth ()
    {
        AssertRawFailure(
            Encoding.UTF8.GetBytes(CreateNestedArrayJson(65)),
            JsonCanonicalizationFailureKind.MaximumDepthExceeded);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Canonicalize_AcceptsMaximumDepth_ForBothEntrypoints ()
    {
        string json = CreateNestedArrayJson(64);
        byte[] expected = Encoding.UTF8.GetBytes(json);

        byte[] rawResult = Rfc8785JsonCanonicalizer.Canonicalize(expected);
        using JsonDocument document = JsonDocument.Parse(
            json,
            new JsonDocumentOptions
            {
                MaxDepth = 128,
            });
        byte[] elementResult = Rfc8785JsonCanonicalizer.Canonicalize(document.RootElement);

        Assert.Equal(expected, rawResult);
        Assert.Equal(expected, elementResult);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Size", "Medium")]
    public void Canonicalize_AcceptsDeepParsedElementWithoutUsingTheCallStack (bool useObjects)
    {
        const int depth = 10_000;
        string json = useObjects
            ? CreateNestedObjectJson(depth)
            : CreateNestedArrayJson(depth);
        using JsonDocument document = JsonDocument.Parse(
            json,
            new JsonDocumentOptions
            {
                MaxDepth = depth + 1,
            });

        byte[] result = Rfc8785JsonCanonicalizer.Canonicalize(document.RootElement);

        Assert.Equal(Encoding.UTF8.GetBytes(json), result);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Canonicalize_AcceptsParsedElementFromLenientSourceSyntax ()
    {
        using JsonDocument document = JsonDocument.Parse(
            """{"b":2,/* source comment */"a":1,}""",
            new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip,
            });

        byte[] result = Rfc8785JsonCanonicalizer.Canonicalize(document.RootElement);

        Assert.Equal("""{"a":1,"b":2}"""u8.ToArray(), result);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Canonicalize_DuplicatePropertyFailureMessageDoesNotEmbedPropertyName ()
    {
        const string propertyName = "\u001b[31m\n\u202E";
        string encodedName = JsonSerializer.Serialize(propertyName);
        byte[] json = Encoding.UTF8.GetBytes(
            $"{{{encodedName}:1,{encodedName}:2}}");

        JsonCanonicalizationException exception = Assert.Throws<JsonCanonicalizationException>(
            () => Rfc8785JsonCanonicalizer.Canonicalize(json));

        Assert.Equal(JsonCanonicalizationFailureKind.DuplicateProperty, exception.FailureKind);
        Assert.DoesNotContain(propertyName, exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain('\n', exception.Message);
        Assert.DoesNotContain('\u001b', exception.Message);
        Assert.DoesNotContain('\u202E', exception.Message);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void FailureKind_ContainsOnlyTheClosedContractSet ()
    {
        Assert.Equal(
            [
                JsonCanonicalizationFailureKind.InvalidJson,
                JsonCanonicalizationFailureKind.DuplicateProperty,
                JsonCanonicalizationFailureKind.InvalidUnicode,
                JsonCanonicalizationFailureKind.NumberNotRepresentable,
                JsonCanonicalizationFailureKind.NegativeZero,
                JsonCanonicalizationFailureKind.MaximumDepthExceeded,
            ],
            Enum.GetValues<JsonCanonicalizationFailureKind>());
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Assembly_ExportsOnlyTheCanonicalizationContractTypes ()
    {
        Assert.Equal(
            [
                typeof(JsonCanonicalizationException),
                typeof(JsonCanonicalizationFailureKind),
                typeof(Rfc8785JsonCanonicalizer),
            ],
            typeof(Rfc8785JsonCanonicalizer).Assembly
                .GetExportedTypes()
                .OrderBy(static type => type.FullName, StringComparer.Ordinal));
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Canonicalize_RoundsJsonNumberToItsBinary64Value ()
    {
        byte[] result = Rfc8785JsonCanonicalizer.Canonicalize(
            "9007199254740993"u8);

        Assert.Equal("9007199254740992"u8.ToArray(), result);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Canonicalize_ReturnsCallerOwnedArray_ForBothEntrypoints ()
    {
        byte[] input = """{"value":1}"""u8.ToArray();
        byte[] expected = input.ToArray();

        byte[] firstRawResult = Rfc8785JsonCanonicalizer.Canonicalize(input);
        byte[] secondRawResult = Rfc8785JsonCanonicalizer.Canonicalize(input);
        using JsonDocument document = JsonDocument.Parse(input);
        byte[] firstElementResult = Rfc8785JsonCanonicalizer.Canonicalize(document.RootElement);
        byte[] secondElementResult = Rfc8785JsonCanonicalizer.Canonicalize(document.RootElement);

        Assert.NotSame(firstRawResult, secondRawResult);
        Assert.NotSame(firstElementResult, secondElementResult);

        firstRawResult[0] = 0;
        firstElementResult[0] = 0;

        Assert.Equal(expected, secondRawResult);
        Assert.Equal(expected, secondElementResult);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Canonicalize_ReturnedBytesRemainValid_AfterSourceDocumentIsDisposed ()
    {
        byte[] result;
        using (JsonDocument document = JsonDocument.Parse("""{"b":1,"a":2}"""))
        {
            result = Rfc8785JsonCanonicalizer.Canonicalize(document.RootElement);
        }

        Assert.Equal("""{"a":2,"b":1}"""u8.ToArray(), result);
    }

    [Fact]
    [Trait("Size", "Small")]
    public void Canonicalize_ThrowsInvalidJson_WhenSourceDocumentIsDisposed ()
    {
        JsonElement value;
        using (JsonDocument document = JsonDocument.Parse("""{"value":1}"""))
        {
            value = document.RootElement;
        }

        JsonCanonicalizationException exception = Assert.Throws<JsonCanonicalizationException>(
            () => Rfc8785JsonCanonicalizer.Canonicalize(value));

        Assert.Equal(JsonCanonicalizationFailureKind.InvalidJson, exception.FailureKind);
    }

    private static void AssertRawFailure (
        byte[] json,
        JsonCanonicalizationFailureKind expectedFailureKind)
    {
        JsonCanonicalizationException exception = Assert.Throws<JsonCanonicalizationException>(
            () => Rfc8785JsonCanonicalizer.Canonicalize(json));

        Assert.Equal(expectedFailureKind, exception.FailureKind);
    }

    private static string CreateNestedArrayJson (int depth)
    {
        return new string('[', depth) + "0" + new string(']', depth);
    }

    private static string CreateNestedObjectJson (int depth)
    {
        return string.Concat(Enumerable.Repeat("""{"value":""", depth))
            + "0"
            + new string('}', depth);
    }
}
