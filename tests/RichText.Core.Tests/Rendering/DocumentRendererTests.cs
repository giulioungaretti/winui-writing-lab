using RichTexteEditor.Core.Model;
using RichTexteEditor.Core.Rendering;
using Xunit;

namespace RichTexteEditor.Core.Tests.Rendering;

/// <summary>
/// Tests for the rendering visitor pattern.
/// </summary>
public class DocumentRendererTests
{
    [Fact]
    public void WhenRenderingTextRunThenVisitorIsCalled()
    {
        var renderer = new TestDocumentRenderer();
        var textRun = new TextRun("Hello");

        renderer.RenderInline(textRun);

        Assert.Single(renderer.VisitedElements);
        Assert.Equal("TextRun:Hello", renderer.VisitedElements[0]);
    }

    [Fact]
    public void WhenRenderingTagThenVisitorIsCalled()
    {
        var renderer = new TestDocumentRenderer();
        var tag = new Tag("project");

        renderer.RenderInline(tag);

        Assert.Single(renderer.VisitedElements);
        Assert.Equal("Tag:project", renderer.VisitedElements[0]);
    }

    [Fact]
    public void WhenRenderingTodoThenVisitorIsCalled()
    {
        var renderer = new TestDocumentRenderer();
        var todo = new Todo("task", isCompleted: true);

        renderer.RenderInline(todo);

        Assert.Single(renderer.VisitedElements);
        Assert.Equal("Todo:task:completed", renderer.VisitedElements[0]);
    }

    [Fact]
    public void WhenRenderingParagraphThenAllInlinesRendered()
    {
        var renderer = new TestDocumentRenderer();
        var paragraph = new Paragraph(new IInline[]
        {
            new TextRun("Hello "),
            new Tag("world")
        });

        renderer.RenderBlock(paragraph);

        Assert.Equal(3, renderer.VisitedElements.Count); // 2 inlines + Paragraph
        Assert.Contains(renderer.VisitedElements, e => e.StartsWith("Paragraph:"));
    }

    [Fact]
    public void WhenRenderingDocumentThenAllBlocksRendered()
    {
        var renderer = new TestDocumentRenderer();
        var document = new Document(new IBlock[]
        {
            new Paragraph(new IInline[] { new TextRun("First") }),
            new Paragraph(new IInline[] { new TextRun("Second") })
        });

        renderer.RenderDocument(document);

        Assert.Contains(renderer.VisitedElements, e => e.StartsWith("Document:"));
        Assert.Equal(2, renderer.VisitedElements.Count(e => e.StartsWith("Paragraph:")));
    }

    [Fact]
    public void WhenRenderingEmptyDocumentThenDocumentVisited()
    {
        var renderer = new TestDocumentRenderer();
        var document = new Document();

        renderer.RenderDocument(document);

        Assert.Single(renderer.VisitedElements);
        Assert.Equal("Document:0", renderer.VisitedElements[0]);
    }

    /// <summary>
    /// Test implementation of the document renderer for verification.
    /// </summary>
    private class TestDocumentRenderer : DocumentRendererBase<string>
    {
        public List<string> VisitedElements { get; } = new();

        protected override string VisitTextRun(TextRun textRun)
        {
            var result = $"TextRun:{textRun.Text}";
            VisitedElements.Add(result);
            return result;
        }

        protected override string VisitTag(Tag tag)
        {
            var result = $"Tag:{tag.Name}";
            VisitedElements.Add(result);
            return result;
        }

        protected override string VisitTodo(Todo todo)
        {
            var status = todo.IsCompleted ? "completed" : "pending";
            var result = $"Todo:{todo.Text}:{status}";
            VisitedElements.Add(result);
            return result;
        }

        protected override string VisitParagraph(Paragraph paragraph, IReadOnlyList<string> inlineResults)
        {
            var result = $"Paragraph:{inlineResults.Count}";
            VisitedElements.Add(result);
            return result;
        }

        protected override string VisitDocument(Document document, IReadOnlyList<string> blockResults)
        {
            var result = $"Document:{blockResults.Count}";
            VisitedElements.Add(result);
            return result;
        }
    }
}
