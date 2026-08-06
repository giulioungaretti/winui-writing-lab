using RichTexteEditor.Core.Model;
using RichTexteEditor.Core.Parsing;
using RichTexteEditor.Core.Rendering;
using Xunit;

namespace RichTexteEditor.Core.Tests.Integration;

/// <summary>
/// Integration tests for the complete document workflow.
/// </summary>
public class DocumentWorkflowTests
{
    private readonly DocumentParser _parser = new();
    private readonly PlainTextRenderer _plainTextRenderer = new();
    private readonly HtmlRenderer _htmlRenderer = new();
    private readonly MarkdownRenderer _markdownRenderer = new();

    [Fact]
    public void WhenParsingAndRenderingPlainTextThenRoundTripSucceeds()
    {
        var input = "Hello #world and [ ] task";

        var document = _parser.Parse(input);
        var output = _plainTextRenderer.RenderDocument(document);

        Assert.Equal(input, output);
    }

    [Fact]
    public void WhenParsingComplexDocumentThenAllElementsAccessible()
    {
        var input = """
            Meeting notes #meeting
            
            [ ] Review the slides
            [x] Send invitations
            [ ] Book conference room
            
            Don't forget #urgent items!
            """;

        var document = _parser.Parse(input);

        var tags = document.FindAllTags();
        var todos = document.FindAllTodos();
        var incompleteTodos = document.GetIncompleteTodos();

        Assert.Equal(2, tags.Count);
        Assert.Equal(3, todos.Count);
        Assert.Equal(2, incompleteTodos.Count);
        Assert.Single(document.GetCompletedTodos());
    }

    [Fact]
    public void WhenModifyingDocumentThenOriginalUnchanged()
    {
        var original = _parser.Parse("Hello #world");

        var modified = original.AppendBlock(new Paragraph(new IInline[]
        {
            new TextRun("New paragraph with "),
            new Tag("newtag")
        }));

        Assert.Single(original.Blocks);
        Assert.Equal(2, modified.Blocks.Count);
        Assert.Single(original.FindAllTags());
        Assert.Equal(2, modified.FindAllTags().Count);
    }

    [Fact]
    public void WhenTogglingTodoThenDocumentUpdatedCorrectly()
    {
        var document = _parser.Parse("[ ] Complete task");

        var paragraph = (Paragraph)document.Blocks[0];
        var todo = paragraph.FindTodos()[0];
        var toggledTodo = todo.ToggleCompleted();

        var updatedParagraph = paragraph.ReplaceInlineAt(0, toggledTodo);
        var updatedDocument = document.ReplaceBlockAt(0, updatedParagraph);

        Assert.False(todo.IsCompleted);
        Assert.True(toggledTodo.IsCompleted);
        Assert.True(updatedDocument.FindAllTodos()[0].IsCompleted);
    }

    [Fact]
    public void WhenRenderingToHtmlThenTagsHaveCorrectMarkup()
    {
        var document = _parser.Parse("Check #project status");

        var html = _htmlRenderer.RenderDocument(document);

        Assert.Contains("class=\"tag\"", html);
        Assert.Contains("data-tag=\"project\"", html);
        Assert.Contains("#project</span>", html);
    }

    [Fact]
    public void WhenRenderingToHtmlThenTodosHaveCheckboxes()
    {
        var document = _parser.Parse("[ ] Incomplete task");

        var html = _htmlRenderer.RenderDocument(document);

        Assert.Contains("<input type=\"checkbox\">", html);
        Assert.Contains("class=\"todo\"", html);
        Assert.DoesNotContain("checked", html);
    }

    [Fact]
    public void WhenRenderingCompletedTodoToHtmlThenCheckboxIsChecked()
    {
        var document = _parser.Parse("[x] Complete task");

        var html = _htmlRenderer.RenderDocument(document);

        Assert.Contains("checked", html);
        Assert.Contains("class=\"todo completed\"", html);
    }

    [Fact]
    public void WhenRenderingToMarkdownThenFormattingApplied()
    {
        var document = new Document(new IBlock[]
        {
            new Paragraph(new IInline[]
            {
                new TextRun("Important", TextFormatting.Bold),
                new TextRun(" and "),
                new TextRun("emphasized", TextFormatting.Italic)
            })
        });

        var markdown = _markdownRenderer.RenderDocument(document);

        Assert.Contains("**Important**", markdown);
        Assert.Contains("*emphasized*", markdown);
    }

    [Fact]
    public void WhenBuildingDocumentProgrammaticallyThenRendersCorrectly()
    {
        var document = new Document()
            .AppendBlock(new Paragraph(new IInline[]
            {
                new TextRun("Project: "),
                new Tag("my-project")
            }))
            .AppendBlock(new Paragraph())
            .AppendBlock(new Paragraph(new IInline[]
            {
                new Todo("First task"),
                new TextRun(" "),
                new Todo("Second task", isCompleted: true)
            }));

        var plainText = _plainTextRenderer.RenderDocument(document);

        Assert.Contains("Project: #my-project", plainText);
        Assert.Contains("[ ] First task", plainText);
        Assert.Contains("[x] Second task", plainText);
    }

    [Fact]
    public void WhenFilteringByTagThenCorrectBlocksReturned()
    {
        var document = _parser.Parse("""
            Line with #tag1
            Line without tags
            Line with #tag2 and #tag1
            Another #tag2 line
            """);

        var blocksWithTag1 = document.Blocks
            .OfType<Paragraph>()
            .Where(p => p.FindTags().Any(t => t.Matches("tag1")))
            .ToList();

        Assert.Equal(2, blocksWithTag1.Count);
    }
}
