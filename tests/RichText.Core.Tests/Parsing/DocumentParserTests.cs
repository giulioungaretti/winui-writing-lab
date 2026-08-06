using RichTexteEditor.Core.Model;
using RichTexteEditor.Core.Parsing;
using Xunit;

namespace RichTexteEditor.Core.Tests.Parsing;

/// <summary>
/// Tests for DocumentParser - parses multi-line text into a document.
/// </summary>
public class DocumentParserTests
{
    private readonly DocumentParser _parser = new();

    [Fact]
    public void WhenParsingSingleLineThenReturnsSingleParagraph()
    {
        var result = _parser.Parse("Hello World");

        Assert.Single(result.Blocks);
        Assert.IsType<Paragraph>(result.Blocks[0]);
    }

    [Fact]
    public void WhenParsingMultipleLinesThenReturnsMultipleParagraphs()
    {
        var result = _parser.Parse("First line\nSecond line\nThird line");

        Assert.Equal(3, result.Blocks.Count);
    }

    [Fact]
    public void WhenParsingEmptyLinesThenCreatesEmptyParagraphs()
    {
        var result = _parser.Parse("First\n\nThird");

        Assert.Equal(3, result.Blocks.Count);
        Assert.True(result.Blocks[1].IsEmpty);
    }

    [Fact]
    public void WhenParsingNullThenThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _parser.Parse(null!));
    }

    [Fact]
    public void WhenParsingEmptyStringThenReturnsEmptyDocument()
    {
        var result = _parser.Parse(string.Empty);

        Assert.True(result.IsEmpty);
    }

    [Fact]
    public void WhenParsingWithWindowsLineEndingsThenParsedCorrectly()
    {
        var result = _parser.Parse("First\r\nSecond");

        Assert.Equal(2, result.Blocks.Count);
    }

    [Fact]
    public void WhenParsingLineWithTagThenParagraphContainsTag()
    {
        var result = _parser.Parse("Hello #world");

        var paragraph = (Paragraph)result.Blocks[0];
        var tags = paragraph.FindTags();

        Assert.Single(tags);
        Assert.Equal("world", tags[0].Name);
    }

    [Fact]
    public void WhenParsingLineWithTodoThenParagraphContainsTodo()
    {
        var result = _parser.Parse("[ ] Buy groceries");

        var paragraph = (Paragraph)result.Blocks[0];
        var todos = paragraph.FindTodos();

        Assert.Single(todos);
        Assert.Equal("Buy groceries", todos[0].Text);
    }

    [Fact]
    public void WhenParsingComplexDocumentThenAllElementsParsed()
    {
        var input = """
            # Project Notes
            
            Working on #project today.
            
            Tasks:
            [ ] Complete documentation
            [x] Review code
            [ ] Deploy to staging
            
            Remember to check #urgent items.
            """;

        var result = _parser.Parse(input);

        var allTags = result.FindAllTags();
        var allTodos = result.FindAllTodos();

        Assert.Equal(2, allTags.Count);
        Assert.Equal(3, allTodos.Count);
        Assert.Equal(2, allTodos.Count(t => !t.IsCompleted));
        Assert.Single(allTodos.Where(t => t.IsCompleted));
    }

    [Fact]
    public void WhenRoundTrippingDocumentThenContentPreserved()
    {
        var input = "Hello #world and [ ] task";
        
        var document = _parser.Parse(input);
        var output = document.ToPlainText();

        Assert.Equal(input, output);
    }
}
