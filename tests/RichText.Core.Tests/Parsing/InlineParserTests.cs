using RichTexteEditor.Core.Model;
using RichTexteEditor.Core.Parsing;
using Xunit;

namespace RichTexteEditor.Core.Tests.Parsing;

/// <summary>
/// Tests for InlineParser - parses text into inline elements.
/// </summary>
public class InlineParserTests
{
    private readonly InlineParser _parser = new();

    #region Plain Text Parsing

    [Fact]
    public void WhenParsingPlainTextThenReturnsSingleTextRun()
    {
        var result = _parser.Parse("Hello World");

        Assert.Single(result);
        Assert.IsType<TextRun>(result[0]);
        Assert.Equal("Hello World", ((TextRun)result[0]).Text);
    }

    [Fact]
    public void WhenParsingEmptyStringThenReturnsEmptyCollection()
    {
        var result = _parser.Parse(string.Empty);

        Assert.Empty(result);
    }

    [Fact]
    public void WhenParsingNullThenThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _parser.Parse(null!));
    }

    [Fact]
    public void WhenParsingWhitespaceOnlyThenReturnsSingleTextRun()
    {
        var result = _parser.Parse("   ");

        Assert.Single(result);
        Assert.Equal("   ", ((TextRun)result[0]).Text);
    }

    #endregion

    #region Tag Parsing

    [Fact]
    public void WhenParsingTextWithTagThenReturnsTagInline()
    {
        var result = _parser.Parse("#project");

        Assert.Single(result);
        Assert.IsType<Tag>(result[0]);
        Assert.Equal("project", ((Tag)result[0]).Name);
    }

    [Fact]
    public void WhenParsingTextWithTagInMiddleThenReturnsThreeInlines()
    {
        var result = _parser.Parse("Hello #world goodbye");

        Assert.Equal(3, result.Count);
        Assert.IsType<TextRun>(result[0]);
        Assert.IsType<Tag>(result[1]);
        Assert.IsType<TextRun>(result[2]);
        Assert.Equal("Hello ", ((TextRun)result[0]).Text);
        Assert.Equal("world", ((Tag)result[1]).Name);
        Assert.Equal(" goodbye", ((TextRun)result[2]).Text);
    }

    [Fact]
    public void WhenParsingMultipleTagsThenReturnsAllTags()
    {
        var result = _parser.Parse("#first #second #third");

        var tags = result.OfType<Tag>().ToList();
        Assert.Equal(3, tags.Count);
        Assert.Equal("first", tags[0].Name);
        Assert.Equal("second", tags[1].Name);
        Assert.Equal("third", tags[2].Name);
    }

    [Fact]
    public void WhenParsingTagWithHyphenThenIncludesHyphen()
    {
        var result = _parser.Parse("#high-priority");

        Assert.Single(result);
        Assert.Equal("high-priority", ((Tag)result[0]).Name);
    }

    [Fact]
    public void WhenParsingTagWithUnderscoreThenIncludesUnderscore()
    {
        var result = _parser.Parse("#meeting_notes");

        Assert.Single(result);
        Assert.Equal("meeting_notes", ((Tag)result[0]).Name);
    }

    [Fact]
    public void WhenParsingTagWithNumbersThenIncludesNumbers()
    {
        var result = _parser.Parse("#project123");

        Assert.Single(result);
        Assert.Equal("project123", ((Tag)result[0]).Name);
    }

    [Fact]
    public void WhenParsingHashFollowedByNumberThenTreatedAsPlainText()
    {
        var result = _parser.Parse("#123 is not a tag");

        Assert.Single(result);
        Assert.IsType<TextRun>(result[0]);
    }

    [Fact]
    public void WhenParsingHashFollowedBySpaceThenTreatedAsPlainText()
    {
        var result = _parser.Parse("# not a tag");

        Assert.Single(result);
        Assert.IsType<TextRun>(result[0]);
    }

    [Fact]
    public void WhenParsingHashAtEndWithoutNameThenTreatedAsPlainText()
    {
        var result = _parser.Parse("Just a #");

        Assert.Single(result);
        Assert.IsType<TextRun>(result[0]);
    }

    [Fact]
    public void WhenParsingTagAtEndOfLineThenParsedCorrectly()
    {
        var result = _parser.Parse("Check out #project");

        Assert.Equal(2, result.Count);
        Assert.IsType<TextRun>(result[0]);
        Assert.IsType<Tag>(result[1]);
        Assert.Equal("project", ((Tag)result[1]).Name);
    }

    [Fact]
    public void WhenParsingTagFollowedByPunctuationThenTagEndsBeforePunctuation()
    {
        var result = _parser.Parse("#project, and more");

        Assert.Equal(2, result.Count);
        Assert.Equal("project", ((Tag)result[0]).Name);
        Assert.Equal(", and more", ((TextRun)result[1]).Text);
    }

    #endregion

    #region Todo Parsing

    [Fact]
    public void WhenParsingIncompleteTodoThenReturnsTodoInline()
    {
        var result = _parser.Parse("[ ] Buy groceries");

        Assert.Single(result);
        Assert.IsType<Todo>(result[0]);
        Assert.Equal("Buy groceries", ((Todo)result[0]).Text);
        Assert.False(((Todo)result[0]).IsCompleted);
    }

    [Fact]
    public void WhenParsingCompleteTodoThenReturnsTodoWithCompletedTrue()
    {
        var result = _parser.Parse("[x] Buy groceries");

        Assert.Single(result);
        Assert.IsType<Todo>(result[0]);
        Assert.True(((Todo)result[0]).IsCompleted);
    }

    [Fact]
    public void WhenParsingCompleteTodoWithUppercaseXThenParsedCorrectly()
    {
        var result = _parser.Parse("[X] Buy groceries");

        Assert.Single(result);
        Assert.True(((Todo)result[0]).IsCompleted);
    }

    [Fact]
    public void WhenParsingInlineTodoSyntaxThenReturnsTodoInline()
    {
        var result = _parser.Parse("Remember to @todo(call John) tomorrow");

        Assert.Equal(3, result.Count);
        Assert.IsType<TextRun>(result[0]);
        Assert.IsType<Todo>(result[1]);
        Assert.IsType<TextRun>(result[2]);
        Assert.Equal("call John", ((Todo)result[1]).Text);
        Assert.False(((Todo)result[1]).IsCompleted);
    }

    [Fact]
    public void WhenParsingInlineDoneSyntaxThenReturnsCompletedTodo()
    {
        var result = _parser.Parse("I already @done(called John)");

        Assert.Equal(2, result.Count);
        Assert.IsType<Todo>(result[1]);
        Assert.True(((Todo)result[1]).IsCompleted);
    }

    [Fact]
    public void WhenParsingBracketsThatAreNotTodoThenTreatedAsPlainText()
    {
        var result = _parser.Parse("[not a todo]");

        Assert.Single(result);
        Assert.IsType<TextRun>(result[0]);
    }

    [Fact]
    public void WhenParsingEmptyTodoBracketsThenTreatedAsPlainText()
    {
        var result = _parser.Parse("[ ] ");

        // Empty todo text should be treated as plain text
        Assert.Single(result);
        Assert.IsType<TextRun>(result[0]);
    }

    #endregion

    #region Mixed Content Parsing

    [Fact]
    public void WhenParsingMixedContentThenAllElementsParsedCorrectly()
    {
        var result = _parser.Parse("Working on #project with [ ] task pending");

        Assert.Equal(4, result.Count);
        Assert.IsType<TextRun>(result[0]);
        Assert.IsType<Tag>(result[1]);
        Assert.IsType<TextRun>(result[2]);
        Assert.IsType<Todo>(result[3]);
    }

    [Fact]
    public void WhenParsingMultipleTodosAndTagsThenAllParsedCorrectly()
    {
        var result = _parser.Parse("[ ] First task #urgent [x] Second task #done");

        var todos = result.OfType<Todo>().ToList();
        var tags = result.OfType<Tag>().ToList();

        Assert.Equal(2, todos.Count);
        Assert.Equal(2, tags.Count);
        Assert.False(todos[0].IsCompleted);
        Assert.True(todos[1].IsCompleted);
    }

    [Fact]
    public void WhenParsingAdjacentTagsThenBothParsedSeparately()
    {
        var result = _parser.Parse("#tag1#tag2");

        // This should be parsed as tag, then another tag (or text depending on implementation)
        var tags = result.OfType<Tag>().ToList();
        Assert.Equal(2, tags.Count);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void WhenParsingEscapedHashThenTreatedAsPlainText()
    {
        var result = _parser.Parse(@"\#notAtag");

        Assert.Single(result);
        Assert.IsType<TextRun>(result[0]);
        Assert.Equal("#notAtag", ((TextRun)result[0]).Text);
    }

    [Fact]
    public void WhenParsingUnicodeTextThenHandledCorrectly()
    {
        var result = _parser.Parse("Hello ?? #tag");

        Assert.Equal(2, result.Count);
        Assert.Equal("Hello ?? ", ((TextRun)result[0]).Text);
        Assert.Equal("tag", ((Tag)result[1]).Name);
    }

    [Fact]
    public void WhenParsingNewlinesThenPreservedInTextRuns()
    {
        var result = _parser.Parse("Line one\nLine two");

        Assert.Single(result);
        Assert.Contains("\n", ((TextRun)result[0]).Text);
    }

    #endregion
}
