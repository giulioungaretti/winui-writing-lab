using RichTexteEditor.Core.Model;
using RichTexteEditor.Core.Parsing;
using Xunit;

namespace RichTexteEditor.Core.Tests.Parsing;

/// <summary>
/// Tests for FormattingParser - parses markdown-style formatting into formatted text runs.
/// </summary>
public class FormattingParserTests
{
    #region Basic Spans

    [Fact]
    public void WhenParsingPlainTextThenReturnsSingleUnformattedRun()
    {
        var result = FormattingParser.Parse("Hello World");

        Assert.Single(result);
        Assert.Equal("Hello World", result[0].Text);
        Assert.Equal(TextFormatting.None, result[0].Formatting);
    }

    [Fact]
    public void WhenParsingEmptyStringThenReturnsEmptyCollection()
    {
        Assert.Empty(FormattingParser.Parse(string.Empty));
    }

    [Fact]
    public void WhenParsingNullThenThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => FormattingParser.Parse(null!));
    }

    [Fact]
    public void WhenParsingBoldThenReturnsBoldRun()
    {
        var result = FormattingParser.Parse("**bold**");

        Assert.Single(result);
        Assert.Equal("bold", result[0].Text);
        Assert.Equal(TextFormatting.Bold, result[0].Formatting);
    }

    [Fact]
    public void WhenParsingItalicThenReturnsItalicRun()
    {
        var result = FormattingParser.Parse("*italic*");

        Assert.Single(result);
        Assert.Equal("italic", result[0].Text);
        Assert.Equal(TextFormatting.Italic, result[0].Formatting);
    }

    [Fact]
    public void WhenParsingStrikethroughThenReturnsStrikethroughRun()
    {
        var result = FormattingParser.Parse("~~gone~~");

        Assert.Single(result);
        Assert.Equal("gone", result[0].Text);
        Assert.Equal(TextFormatting.Strikethrough, result[0].Formatting);
    }

    [Fact]
    public void WhenParsingCodeThenReturnsCodeRun()
    {
        var result = FormattingParser.Parse("`code`");

        Assert.Single(result);
        Assert.Equal("code", result[0].Text);
        Assert.Equal(TextFormatting.Code, result[0].Formatting);
    }

    [Fact]
    public void WhenParsingTripleAsteriskThenReturnsBoldItalicRun()
    {
        var result = FormattingParser.Parse("***both***");

        Assert.Single(result);
        Assert.Equal("both", result[0].Text);
        Assert.Equal(TextFormatting.Bold | TextFormatting.Italic, result[0].Formatting);
    }

    #endregion

    #region Mixed Content

    [Fact]
    public void WhenParsingMixedTextThenSplitsIntoRuns()
    {
        var result = FormattingParser.Parse("plain **bold** and ~~struck~~ end");

        Assert.Equal(5, result.Count);
        Assert.Equal(("plain ", TextFormatting.None), (result[0].Text, result[0].Formatting));
        Assert.Equal(("bold", TextFormatting.Bold), (result[1].Text, result[1].Formatting));
        Assert.Equal((" and ", TextFormatting.None), (result[2].Text, result[2].Formatting));
        Assert.Equal(("struck", TextFormatting.Strikethrough), (result[3].Text, result[3].Formatting));
        Assert.Equal((" end", TextFormatting.None), (result[4].Text, result[4].Formatting));
    }

    [Fact]
    public void WhenParsingNestedSpansThenCombinesFormatting()
    {
        var result = FormattingParser.Parse("**bold *and italic***");

        Assert.Equal(2, result.Count);
        Assert.Equal(("bold ", TextFormatting.Bold), (result[0].Text, result[0].Formatting));
        Assert.Equal(("and italic", TextFormatting.Bold | TextFormatting.Italic), (result[1].Text, result[1].Formatting));
    }

    [Fact]
    public void WhenParsingCodeSpanThenInnerMarkersAreLiteral()
    {
        var result = FormattingParser.Parse("`**not bold**`");

        Assert.Single(result);
        Assert.Equal("**not bold**", result[0].Text);
        Assert.Equal(TextFormatting.Code, result[0].Formatting);
    }

    #endregion

    #region Invalid Markers

    [Fact]
    public void WhenMarkerIsUnclosedThenTreatedAsLiteralText()
    {
        var result = FormattingParser.Parse("**unclosed");

        Assert.Single(result);
        Assert.Equal("**unclosed", result[0].Text);
        Assert.Equal(TextFormatting.None, result[0].Formatting);
    }

    [Fact]
    public void WhenSpanIsEmptyThenTreatedAsLiteralText()
    {
        var result = FormattingParser.Parse("****");

        Assert.Single(result);
        Assert.Equal("****", result[0].Text);
        Assert.Equal(TextFormatting.None, result[0].Formatting);
    }

    [Fact]
    public void WhenSpanContentHasEdgeWhitespaceThenTreatedAsLiteralText()
    {
        var result = FormattingParser.Parse("a * b * c");

        Assert.Single(result);
        Assert.Equal("a * b * c", result[0].Text);
        Assert.Equal(TextFormatting.None, result[0].Formatting);
    }

    #endregion

    #region Round Trip With MarkdownRenderer

    [Theory]
    [InlineData("**bold**", TextFormatting.Bold)]
    [InlineData("*italic*", TextFormatting.Italic)]
    [InlineData("~~strike~~", TextFormatting.Strikethrough)]
    [InlineData("`code`", TextFormatting.Code)]
    public void WhenParsingMarkdownRendererOutputThenFormattingRoundTrips(string markdown, TextFormatting expected)
    {
        var result = FormattingParser.Parse(markdown);

        Assert.Single(result);
        Assert.Equal(expected, result[0].Formatting);
    }

    #endregion
}

/// <summary>
/// Tests for InlineParser integration with formatting spans.
/// </summary>
public class InlineParserFormattingTests
{
    private readonly InlineParser _parser = new();

    [Fact]
    public void WhenParsingFormattedTextThenReturnsFormattedRuns()
    {
        var result = _parser.Parse("**bold** and ~~struck~~");

        Assert.Equal(3, result.Count);
        var bold = Assert.IsType<TextRun>(result[0]);
        Assert.Equal(TextFormatting.Bold, bold.Formatting);
        var struck = Assert.IsType<TextRun>(result[2]);
        Assert.Equal(TextFormatting.Strikethrough, struck.Formatting);
    }

    [Fact]
    public void WhenParsingFormattingAroundTagThenBothAreParsed()
    {
        var result = _parser.Parse("**bold** #tag");

        Assert.Equal(3, result.Count);
        Assert.Equal(TextFormatting.Bold, ((TextRun)result[0]).Formatting);
        Assert.IsType<Tag>(result[2]);
    }

    [Fact]
    public void WhenParsingPlainTextThenRunStaysUnformatted()
    {
        var result = _parser.Parse("no markers here");

        var run = Assert.IsType<TextRun>(Assert.Single(result));
        Assert.Equal(TextFormatting.None, run.Formatting);
    }
}
