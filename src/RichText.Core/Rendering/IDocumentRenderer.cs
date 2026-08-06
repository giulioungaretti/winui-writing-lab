using RichTexteEditor.Core.Model;

namespace RichTexteEditor.Core.Rendering;

/// <summary>
/// Interface for rendering document elements to a target representation.
/// </summary>
/// <typeparam name="TResult">The type of the rendered result.</typeparam>
public interface IDocumentRenderer<TResult>
{
    /// <summary>
    /// Renders a complete document.
    /// </summary>
    /// <param name="document">The document to render.</param>
    /// <returns>The rendered result.</returns>
    TResult RenderDocument(Document document);

    /// <summary>
    /// Renders a block element.
    /// </summary>
    /// <param name="block">The block to render.</param>
    /// <returns>The rendered result.</returns>
    TResult RenderBlock(IBlock block);

    /// <summary>
    /// Renders an inline element.
    /// </summary>
    /// <param name="inline">The inline to render.</param>
    /// <returns>The rendered result.</returns>
    TResult RenderInline(IInline inline);
}
