using System.Text;
using RichTexteEditor.Core.Model;

namespace RichTexteEditor.Core.Rendering;

/// <summary>
/// Renders a document to plain text format.
/// </summary>
public class PlainTextRenderer : DocumentRendererBase<string>
{
    /// <inheritdoc/>
    protected override string VisitTextRun(TextRun textRun)
    {
        return textRun.Text;
    }

    /// <inheritdoc/>
    protected override string VisitTag(Tag tag)
    {
        return tag.ToPlainText();
    }

    /// <inheritdoc/>
    protected override string VisitTodo(Todo todo)
    {
        return todo.ToPlainText();
    }

    /// <inheritdoc/>
    protected override string VisitImage(ImageInline image)
    {
        return image.AltText;
    }

    /// <inheritdoc/>
    protected override string VisitParagraph(Paragraph paragraph, IReadOnlyList<string> inlineResults)
    {
        return string.Concat(inlineResults);
    }

    /// <inheritdoc/>
    protected override string VisitDocument(Document document, IReadOnlyList<string> blockResults)
    {
        return string.Join(Environment.NewLine, blockResults);
    }
}

/// <summary>
/// Renders a document to Markdown format.
/// </summary>
public class MarkdownRenderer : DocumentRendererBase<string>
{
    /// <inheritdoc/>
    protected override string VisitTextRun(TextRun textRun)
    {
        var text = textRun.Text;

        if (textRun.Formatting.HasFlag(TextFormatting.Bold))
        {
            text = $"**{text}**";
        }

        if (textRun.Formatting.HasFlag(TextFormatting.Italic))
        {
            text = $"*{text}*";
        }

        if (textRun.Formatting.HasFlag(TextFormatting.Strikethrough))
        {
            text = $"~~{text}~~";
        }

        if (textRun.Formatting.HasFlag(TextFormatting.Code))
        {
            text = $"`{text}`";
        }

        return text;
    }

    /// <inheritdoc/>
    protected override string VisitTag(Tag tag)
    {
        return tag.ToPlainText();
    }

    /// <inheritdoc/>
    protected override string VisitTodo(Todo todo)
    {
        var checkbox = todo.IsCompleted ? "[x]" : "[ ]";
        return $"- {checkbox} {todo.Text}";
    }

    /// <inheritdoc/>
    protected override string VisitImage(ImageInline image)
    {
        return image.ToPlainText();
    }

    /// <inheritdoc/>
    protected override string VisitParagraph(Paragraph paragraph, IReadOnlyList<string> inlineResults)
    {
        return string.Concat(inlineResults);
    }

    /// <inheritdoc/>
    protected override string VisitDocument(Document document, IReadOnlyList<string> blockResults)
    {
        return string.Join(Environment.NewLine + Environment.NewLine, blockResults);
    }
}

/// <summary>
/// Renders a document to HTML format.
/// </summary>
public class HtmlRenderer : DocumentRendererBase<string>
{
    /// <inheritdoc/>
    protected override string VisitTextRun(TextRun textRun)
    {
        var text = System.Net.WebUtility.HtmlEncode(textRun.Text);

        if (textRun.Formatting.HasFlag(TextFormatting.Bold))
        {
            text = $"<strong>{text}</strong>";
        }

        if (textRun.Formatting.HasFlag(TextFormatting.Italic))
        {
            text = $"<em>{text}</em>";
        }

        if (textRun.Formatting.HasFlag(TextFormatting.Underline))
        {
            text = $"<u>{text}</u>";
        }

        if (textRun.Formatting.HasFlag(TextFormatting.Strikethrough))
        {
            text = $"<s>{text}</s>";
        }

        if (textRun.Formatting.HasFlag(TextFormatting.Code))
        {
            text = $"<code>{text}</code>";
        }

        return text;
    }

    /// <inheritdoc/>
    protected override string VisitTag(Tag tag)
    {
        var escapedName = System.Net.WebUtility.HtmlEncode(tag.Name);
        return $"<span class=\"tag\" data-tag=\"{escapedName}\">#{escapedName}</span>";
    }

    /// <inheritdoc/>
    protected override string VisitTodo(Todo todo)
    {
        var checkedAttr = todo.IsCompleted ? " checked" : "";
        var completedClass = todo.IsCompleted ? " completed" : "";
        var escapedText = System.Net.WebUtility.HtmlEncode(todo.Text);

        return $"<span class=\"todo{completedClass}\"><input type=\"checkbox\"{checkedAttr}><span class=\"todo-text\">{escapedText}</span></span>";
    }

    /// <inheritdoc/>
    protected override string VisitImage(ImageInline image)
    {
        var escapedSource = System.Net.WebUtility.HtmlEncode(image.Source);
        var escapedAlt = System.Net.WebUtility.HtmlEncode(image.AltText);
        return $"<img src=\"{escapedSource}\" alt=\"{escapedAlt}\">";
    }

    /// <inheritdoc/>
    protected override string VisitParagraph(Paragraph paragraph, IReadOnlyList<string> inlineResults)
    {
        var content = string.Concat(inlineResults);
        return $"<p>{content}</p>";
    }

    /// <inheritdoc/>
    protected override string VisitDocument(Document document, IReadOnlyList<string> blockResults)
    {
        var content = string.Join(Environment.NewLine, blockResults);
        return $"<div class=\"document\">{Environment.NewLine}{content}{Environment.NewLine}</div>";
    }
}
