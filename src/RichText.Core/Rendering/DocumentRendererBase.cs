using RichTexteEditor.Core.Model;

namespace RichTexteEditor.Core.Rendering;

/// <summary>
/// Base class for document renderers using the visitor pattern.
/// </summary>
/// <typeparam name="TResult">The type of the rendered result.</typeparam>
public abstract class DocumentRendererBase<TResult> : IDocumentRenderer<TResult>
{
    /// <inheritdoc/>
    public TResult RenderDocument(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var blockResults = new List<TResult>();

        foreach (var block in document.Blocks)
        {
            blockResults.Add(RenderBlock(block));
        }

        return VisitDocument(document, blockResults);
    }

    /// <inheritdoc/>
    public TResult RenderBlock(IBlock block)
    {
        ArgumentNullException.ThrowIfNull(block);

        return block switch
        {
            Paragraph paragraph => RenderParagraph(paragraph),
            _ => throw new NotSupportedException($"Block type {block.Type} is not supported.")
        };
    }

    /// <inheritdoc/>
    public TResult RenderInline(IInline inline)
    {
        ArgumentNullException.ThrowIfNull(inline);

        return inline switch
        {
            TextRun textRun => VisitTextRun(textRun),
            Tag tag => VisitTag(tag),
            Todo todo => VisitTodo(todo),
            ImageInline image => VisitImage(image),
            _ => throw new NotSupportedException($"Inline type {inline.Type} is not supported.")
        };
    }

    private TResult RenderParagraph(Paragraph paragraph)
    {
        var inlineResults = new List<TResult>();

        foreach (var inline in paragraph.Inlines)
        {
            inlineResults.Add(RenderInline(inline));
        }

        return VisitParagraph(paragraph, inlineResults);
    }

    /// <summary>
    /// Visits a text run element.
    /// </summary>
    /// <param name="textRun">The text run to visit.</param>
    /// <returns>The rendered result.</returns>
    protected abstract TResult VisitTextRun(TextRun textRun);

    /// <summary>
    /// Visits a tag element.
    /// </summary>
    /// <param name="tag">The tag to visit.</param>
    /// <returns>The rendered result.</returns>
    protected abstract TResult VisitTag(Tag tag);

    /// <summary>
    /// Visits a todo element.
    /// </summary>
    /// <param name="todo">The todo to visit.</param>
    /// <returns>The rendered result.</returns>
    protected abstract TResult VisitTodo(Todo todo);

    /// <summary>
    /// Visits an inline image element. Defaults to rendering the image's
    /// plain-text form via <see cref="VisitTextRun"/> so existing renderers
    /// keep working without overriding.
    /// </summary>
    /// <param name="image">The image to visit.</param>
    /// <returns>The rendered result.</returns>
    protected virtual TResult VisitImage(ImageInline image) => VisitTextRun(new TextRun(image.ToPlainText()));

    /// <summary>
    /// Visits a paragraph block.
    /// </summary>
    /// <param name="paragraph">The paragraph to visit.</param>
    /// <param name="inlineResults">The rendered results of the paragraph's inlines.</param>
    /// <returns>The rendered result.</returns>
    protected abstract TResult VisitParagraph(Paragraph paragraph, IReadOnlyList<TResult> inlineResults);

    /// <summary>
    /// Visits a document.
    /// </summary>
    /// <param name="document">The document to visit.</param>
    /// <param name="blockResults">The rendered results of the document's blocks.</param>
    /// <returns>The rendered result.</returns>
    protected abstract TResult VisitDocument(Document document, IReadOnlyList<TResult> blockResults);
}
