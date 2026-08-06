using RichTexteEditor.Core.Model;
using RichTexteEditor.Core.Parsing;
using RichTexteEditor.Core.Rendering;
using Xunit;

namespace RichTexteEditor.Core.Tests.Parsing;

/// <summary>
/// Tests for inline image parsing and rendering (![alt](source)).
/// </summary>
public class ImageInlineTests
{
    private readonly InlineParser _parser = new();

    #region Model

    [Fact]
    public void WhenCreatingImageThenPropertiesAreSet()
    {
        var image = new ImageInline("Assets/pic.png", "a picture");

        Assert.Equal("Assets/pic.png", image.Source);
        Assert.Equal("a picture", image.AltText);
        Assert.Equal(InlineType.Image, image.Type);
    }

    [Fact]
    public void WhenSourceIsEmptyThenThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new ImageInline("   ", "alt"));
    }

    [Fact]
    public void WhenConvertingToPlainTextThenUsesMarkdownForm()
    {
        var image = new ImageInline("pic.png", "alt");

        Assert.Equal("![alt](pic.png)", image.ToPlainText());
    }

    #endregion

    #region Parsing

    [Fact]
    public void WhenParsingImageThenReturnsImageInline()
    {
        var result = _parser.Parse("![lab sketch](Assets/demo-sketch.png)");

        var image = Assert.IsType<ImageInline>(Assert.Single(result));
        Assert.Equal("Assets/demo-sketch.png", image.Source);
        Assert.Equal("lab sketch", image.AltText);
    }

    [Fact]
    public void WhenParsingImageWithEmptyAltThenAltIsEmpty()
    {
        var result = _parser.Parse("![](pic.png)");

        var image = Assert.IsType<ImageInline>(Assert.Single(result));
        Assert.Equal(string.Empty, image.AltText);
    }

    [Fact]
    public void WhenParsingImageWithinTextThenSplitsCorrectly()
    {
        var result = _parser.Parse("see ![alt](pic.png) here");

        Assert.Equal(3, result.Count);
        Assert.Equal("see ", ((TextRun)result[0]).Text);
        Assert.IsType<ImageInline>(result[1]);
        Assert.Equal(" here", ((TextRun)result[2]).Text);
    }

    [Fact]
    public void WhenParsingImageNextToTagThenBothAreParsed()
    {
        var result = _parser.Parse("![alt](pic.png) #tag");

        Assert.IsType<ImageInline>(result[0]);
        Assert.Contains(result, i => i is Tag);
    }

    [Fact]
    public void WhenImageSyntaxIsIncompleteThenTreatedAsText()
    {
        var result = _parser.Parse("![alt](unclosed");

        Assert.All(result, i => Assert.IsType<TextRun>(i));
    }

    #endregion

    #region Rendering

    [Fact]
    public void WhenRenderingImageToMarkdownThenRoundTrips()
    {
        var doc = new Document().AppendBlock(new Paragraph([new ImageInline("pic.png", "alt")]));

        var markdown = new MarkdownRenderer().RenderDocument(doc);

        Assert.Equal("![alt](pic.png)", markdown);
    }

    [Fact]
    public void WhenRenderingImageToHtmlThenEmitsImgTag()
    {
        var doc = new Document().AppendBlock(new Paragraph([new ImageInline("pic.png", "a <b> alt")]));

        var html = new HtmlRenderer().RenderDocument(doc);

        Assert.Contains("<img src=\"pic.png\" alt=\"a &lt;b&gt; alt\">", html);
    }

    [Fact]
    public void WhenRenderingImageToPlainTextThenUsesAltText()
    {
        var doc = new Document().AppendBlock(new Paragraph([new ImageInline("pic.png", "the alt")]));

        var plain = new PlainTextRenderer().RenderDocument(doc);

        Assert.Equal("the alt", plain);
    }

    #endregion
}
