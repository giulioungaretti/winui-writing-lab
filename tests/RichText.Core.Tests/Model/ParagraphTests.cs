using RichTexteEditor.Core.Model;
using Xunit;

namespace RichTexteEditor.Core.Tests.Model;

/// <summary>
/// Tests for Paragraph block element - contains inline elements.
/// </summary>
public class ParagraphTests
{
    [Fact]
    public void WhenCreatedEmptyThenHasNoInlines()
    {
        var paragraph = new Paragraph();

        Assert.Empty(paragraph.Inlines);
    }

    [Fact]
    public void WhenCreatedWithInlinesThenContainsInlines()
    {
        var inlines = new IInline[] { new TextRun("Hello") };

        var paragraph = new Paragraph(inlines);

        Assert.Single(paragraph.Inlines);
        Assert.Equal("Hello", ((TextRun)paragraph.Inlines[0]).Text);
    }

    [Fact]
    public void WhenAppendInlineCalledThenReturnsNewParagraphWithInline()
    {
        var original = new Paragraph();

        var modified = original.AppendInline(new TextRun("Hello"));

        Assert.Empty(original.Inlines);
        Assert.Single(modified.Inlines);
    }

    [Fact]
    public void WhenAppendMultipleInlinesThenAllAreAdded()
    {
        var paragraph = new Paragraph()
            .AppendInline(new TextRun("Hello "))
            .AppendInline(new Tag("world"))
            .AppendInline(new TextRun(" and "))
            .AppendInline(new Todo("remember this"));

        Assert.Equal(4, paragraph.Inlines.Count);
        Assert.IsType<TextRun>(paragraph.Inlines[0]);
        Assert.IsType<Tag>(paragraph.Inlines[1]);
        Assert.IsType<TextRun>(paragraph.Inlines[2]);
        Assert.IsType<Todo>(paragraph.Inlines[3]);
    }

    [Fact]
    public void WhenInsertInlineAtIndexThenInlineInsertedAtCorrectPosition()
    {
        var paragraph = new Paragraph(new IInline[]
        {
            new TextRun("Start "),
            new TextRun("End")
        });

        var modified = paragraph.InsertInlineAt(1, new Tag("middle"));

        Assert.Equal(3, modified.Inlines.Count);
        Assert.IsType<Tag>(modified.Inlines[1]);
    }

    [Fact]
    public void WhenRemoveInlineAtIndexThenInlineRemoved()
    {
        var paragraph = new Paragraph(new IInline[]
        {
            new TextRun("Start "),
            new Tag("remove"),
            new TextRun("End")
        });

        var modified = paragraph.RemoveInlineAt(1);

        Assert.Equal(2, modified.Inlines.Count);
        Assert.IsType<TextRun>(modified.Inlines[0]);
        Assert.IsType<TextRun>(modified.Inlines[1]);
    }

    [Fact]
    public void WhenReplaceInlineAtIndexThenInlineReplaced()
    {
        var paragraph = new Paragraph(new IInline[]
        {
            new TextRun("Hello"),
            new Tag("old")
        });

        var modified = paragraph.ReplaceInlineAt(1, new Tag("new"));

        Assert.Equal("new", ((Tag)modified.Inlines[1]).Name);
    }

    [Fact]
    public void WhenBlockTypeRequestedThenReturnsParagraph()
    {
        var paragraph = new Paragraph();

        Assert.Equal(BlockType.Paragraph, paragraph.Type);
    }

    [Fact]
    public void WhenPlainTextRequestedThenConcatenatesAllInlines()
    {
        var paragraph = new Paragraph(new IInline[]
        {
            new TextRun("Hello "),
            new Tag("world"),
            new TextRun(" and "),
            new Todo("remember this")
        });

        Assert.Equal("Hello #world and [ ] remember this", paragraph.ToPlainText());
    }

    [Fact]
    public void WhenIsEmptyCheckedOnEmptyParagraphThenReturnsTrue()
    {
        var paragraph = new Paragraph();

        Assert.True(paragraph.IsEmpty);
    }

    [Fact]
    public void WhenIsEmptyCheckedOnNonEmptyParagraphThenReturnsFalse()
    {
        var paragraph = new Paragraph(new IInline[] { new TextRun("Hello") });

        Assert.False(paragraph.IsEmpty);
    }

    [Fact]
    public void WhenFindTagsCalledThenReturnsAllTagsInParagraph()
    {
        var paragraph = new Paragraph(new IInline[]
        {
            new TextRun("Hello "),
            new Tag("first"),
            new TextRun(" and "),
            new Tag("second")
        });

        var tags = paragraph.FindTags();

        Assert.Equal(2, tags.Count);
        Assert.Contains(tags, t => t.Name == "first");
        Assert.Contains(tags, t => t.Name == "second");
    }

    [Fact]
    public void WhenFindTodosCalledThenReturnsAllTodosInParagraph()
    {
        var paragraph = new Paragraph(new IInline[]
        {
            new Todo("task one"),
            new TextRun(" and "),
            new Todo("task two", isCompleted: true)
        });

        var todos = paragraph.FindTodos();

        Assert.Equal(2, todos.Count);
        Assert.Contains(todos, t => t.Text == "task one" && !t.IsCompleted);
        Assert.Contains(todos, t => t.Text == "task two" && t.IsCompleted);
    }
}
