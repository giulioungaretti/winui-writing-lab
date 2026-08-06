using RichTexteEditor.Core.Model;
using Xunit;

namespace RichTexteEditor.Core.Tests.Model;

/// <summary>
/// Tests for Document - the root container of all content.
/// </summary>
public class DocumentTests
{
    [Fact]
    public void WhenCreatedEmptyThenHasNoBlocks()
    {
        var document = new Document();

        Assert.Empty(document.Blocks);
    }

    [Fact]
    public void WhenCreatedWithBlocksThenContainsBlocks()
    {
        var blocks = new IBlock[] { new Paragraph() };

        var document = new Document(blocks);

        Assert.Single(document.Blocks);
    }

    [Fact]
    public void WhenAppendBlockCalledThenReturnsNewDocumentWithBlock()
    {
        var original = new Document();

        var modified = original.AppendBlock(new Paragraph());

        Assert.Empty(original.Blocks);
        Assert.Single(modified.Blocks);
    }

    [Fact]
    public void WhenInsertBlockAtIndexThenBlockInsertedAtCorrectPosition()
    {
        var document = new Document(new IBlock[]
        {
            new Paragraph(new IInline[] { new TextRun("First") }),
            new Paragraph(new IInline[] { new TextRun("Third") })
        });

        var modified = document.InsertBlockAt(1, 
            new Paragraph(new IInline[] { new TextRun("Second") }));

        Assert.Equal(3, modified.Blocks.Count);
        Assert.Equal("Second", modified.Blocks[1].ToPlainText());
    }

    [Fact]
    public void WhenRemoveBlockAtIndexThenBlockRemoved()
    {
        var document = new Document(new IBlock[]
        {
            new Paragraph(new IInline[] { new TextRun("Keep") }),
            new Paragraph(new IInline[] { new TextRun("Remove") }),
            new Paragraph(new IInline[] { new TextRun("Keep") })
        });

        var modified = document.RemoveBlockAt(1);

        Assert.Equal(2, modified.Blocks.Count);
    }

    [Fact]
    public void WhenReplaceBlockAtIndexThenBlockReplaced()
    {
        var document = new Document(new IBlock[]
        {
            new Paragraph(new IInline[] { new TextRun("Original") })
        });

        var modified = document.ReplaceBlockAt(0, 
            new Paragraph(new IInline[] { new TextRun("Replaced") }));

        Assert.Equal("Replaced", modified.Blocks[0].ToPlainText());
    }

    [Fact]
    public void WhenPlainTextRequestedThenConcatenatesAllBlocksWithNewlines()
    {
        var document = new Document(new IBlock[]
        {
            new Paragraph(new IInline[] { new TextRun("First line") }),
            new Paragraph(new IInline[] { new TextRun("Second line") })
        });

        var plainText = document.ToPlainText();

        Assert.Equal("First line\nSecond line", plainText);
    }

    [Fact]
    public void WhenFindAllTagsCalledThenReturnsTagsFromAllBlocks()
    {
        var document = new Document(new IBlock[]
        {
            new Paragraph(new IInline[] { new Tag("first") }),
            new Paragraph(new IInline[] { new Tag("second"), new Tag("third") })
        });

        var tags = document.FindAllTags();

        Assert.Equal(3, tags.Count);
    }

    [Fact]
    public void WhenFindAllTodosCalledThenReturnsTodosFromAllBlocks()
    {
        var document = new Document(new IBlock[]
        {
            new Paragraph(new IInline[] { new Todo("task one") }),
            new Paragraph(new IInline[] { new Todo("task two"), new Todo("task three") })
        });

        var todos = document.FindAllTodos();

        Assert.Equal(3, todos.Count);
    }

    [Fact]
    public void WhenGetIncompleteTodosCalledThenReturnsOnlyIncompleteTodos()
    {
        var document = new Document(new IBlock[]
        {
            new Paragraph(new IInline[] 
            { 
                new Todo("incomplete", isCompleted: false),
                new Todo("complete", isCompleted: true)
            })
        });

        var incompleteTodos = document.GetIncompleteTodos();

        Assert.Single(incompleteTodos);
        Assert.Equal("incomplete", incompleteTodos[0].Text);
    }

    [Fact]
    public void WhenIsEmptyCheckedOnEmptyDocumentThenReturnsTrue()
    {
        var document = new Document();

        Assert.True(document.IsEmpty);
    }

    [Fact]
    public void WhenIsEmptyCheckedOnDocumentWithEmptyParagraphThenReturnsFalse()
    {
        var document = new Document(new IBlock[] { new Paragraph() });

        Assert.False(document.IsEmpty);
    }

    [Fact]
    public void WhenBlockCountRequestedThenReturnsCorrectCount()
    {
        var document = new Document(new IBlock[]
        {
            new Paragraph(),
            new Paragraph(),
            new Paragraph()
        });

        Assert.Equal(3, document.BlockCount);
    }
}
