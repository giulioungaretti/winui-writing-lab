using RichTexteEditor.Core.Model;
using Xunit;

namespace RichTexteEditor.Core.Tests.Model;

/// <summary>
/// Tests for TextRun inline element - formatted text segments.
/// </summary>
public class TextRunTests
{
    [Fact]
    public void WhenCreatedWithTextThenTextIsSet()
    {
        var textRun = new TextRun("Hello World");

        Assert.Equal("Hello World", textRun.Text);
    }

    [Fact]
    public void WhenCreatedWithoutFormattingThenFormattingIsNone()
    {
        var textRun = new TextRun("Hello");

        Assert.Equal(TextFormatting.None, textRun.Formatting);
    }

    [Fact]
    public void WhenCreatedWithFormattingThenFormattingIsSet()
    {
        var textRun = new TextRun("Hello", TextFormatting.Bold);

        Assert.Equal(TextFormatting.Bold, textRun.Formatting);
    }

    [Fact]
    public void WhenCreatedWithNullTextThenThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new TextRun(null!));
    }

    [Fact]
    public void WhenCreatedWithEmptyTextThenAllowed()
    {
        var textRun = new TextRun(string.Empty);

        Assert.Equal(string.Empty, textRun.Text);
    }

    [Fact]
    public void WhenTwoTextRunsHaveSameContentThenAreEqual()
    {
        var run1 = new TextRun("Hello", TextFormatting.Bold);
        var run2 = new TextRun("Hello", TextFormatting.Bold);

        Assert.Equal(run1, run2);
    }

    [Fact]
    public void WhenTwoTextRunsHaveDifferentTextThenAreNotEqual()
    {
        var run1 = new TextRun("Hello", TextFormatting.Bold);
        var run2 = new TextRun("World", TextFormatting.Bold);

        Assert.NotEqual(run1, run2);
    }

    [Fact]
    public void WhenTwoTextRunsHaveDifferentFormattingThenAreNotEqual()
    {
        var run1 = new TextRun("Hello", TextFormatting.Bold);
        var run2 = new TextRun("Hello", TextFormatting.Italic);

        Assert.NotEqual(run1, run2);
    }

    [Fact]
    public void WhenWithFormattingCalledThenReturnsNewInstanceWithNewFormatting()
    {
        var original = new TextRun("Hello", TextFormatting.Bold);

        var modified = original.WithFormatting(TextFormatting.Italic);

        Assert.Equal(TextFormatting.Bold, original.Formatting);
        Assert.Equal(TextFormatting.Italic, modified.Formatting);
        Assert.Equal("Hello", modified.Text);
    }

    [Fact]
    public void WhenInlineTypeRequestedThenReturnsText()
    {
        var textRun = new TextRun("Hello");

        Assert.Equal(InlineType.Text, textRun.Type);
    }

    [Fact]
    public void WhenPlainTextRequestedThenReturnsText()
    {
        var textRun = new TextRun("Hello World");

        Assert.Equal("Hello World", textRun.ToPlainText());
    }
}
