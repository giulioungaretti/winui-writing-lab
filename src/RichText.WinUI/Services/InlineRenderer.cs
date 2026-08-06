using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using RichTexteEditor.Controls;
using RichTexteEditor.Core.Model;
using RichTexteEditor.Core.Parsing;
using Windows.UI;

// Alias to resolve ambiguity
using XamlParagraph = Microsoft.UI.Xaml.Documents.Paragraph;

namespace RichTexteEditor.Services;

/// <summary>
/// Renders parsed inline elements to RichTextBlock using InlineUIContainer for custom components.
/// This enables Notion-like inline rendering with interactive tag chips and todo checkboxes.
/// </summary>
public sealed class InlineRenderer
{
    private readonly InlineParser _parser = new();

    // Default text colors
    private static readonly Color DefaultTextColor = Color.FromArgb(255, 55, 53, 47);

    /// <summary>
    /// Raised when a tag chip is clicked.
    /// </summary>
    public event EventHandler<string>? TagClicked;

    /// <summary>
    /// Raised when a todo checkbox is toggled.
    /// </summary>
    public event EventHandler<(string Text, bool IsCompleted)>? TodoToggled;

    /// <summary>
    /// Parses text and renders it to the specified RichTextBlock with inline components.
    /// </summary>
    public void RenderToRichTextBlock(string text, RichTextBlock richTextBlock)
    {
        ArgumentNullException.ThrowIfNull(richTextBlock);

        richTextBlock.Blocks.Clear();

        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        // Process line by line to maintain paragraph structure
        var lines = text.Split('\n');
        
        foreach (var line in lines)
        {
            var paragraph = CreateParagraph(line);
            richTextBlock.Blocks.Add(paragraph);
        }
    }

    /// <summary>
    /// Creates a XAML Paragraph with parsed inline elements.
    /// </summary>
    public XamlParagraph CreateParagraph(string text)
    {
        var paragraph = new XamlParagraph();

        if (string.IsNullOrEmpty(text))
        {
            // Empty line - add a zero-width space to maintain line height
            paragraph.Inlines.Add(new Run { Text = "\u200B" });
            return paragraph;
        }

        var inlines = _parser.Parse(text);

        foreach (var inline in inlines)
        {
            var xamlInline = ConvertToXamlInline(inline);
            paragraph.Inlines.Add(xamlInline);
        }

        return paragraph;
    }

    /// <summary>
    /// Converts a parsed inline element to a XAML Inline.
    /// </summary>
    private Inline ConvertToXamlInline(IInline inline)
    {
        return inline.Type switch
        {
            InlineType.Text => CreateTextRun((TextRun)inline),
            InlineType.Tag => CreateTagInline((Tag)inline),
            InlineType.Todo => CreateTodoInline((Todo)inline),
            InlineType.Image => CreateImageInline((ImageInline)inline),
            _ => new Run { Text = inline.ToPlainText() }
        };
    }

    /// <summary>
    /// Creates a Run for plain text with optional formatting.
    /// </summary>
    private static Run CreateTextRun(TextRun textRun)
    {
        var run = new Run
        {
            Text = textRun.Text,
            Foreground = new SolidColorBrush(DefaultTextColor)
        };

        // Apply formatting if present
        if (textRun.Formatting.HasFlag(TextFormatting.Bold))
        {
            run.FontWeight = Microsoft.UI.Text.FontWeights.Bold;
        }

        if (textRun.Formatting.HasFlag(TextFormatting.Italic))
        {
            run.FontStyle = Windows.UI.Text.FontStyle.Italic;
        }

        if (textRun.Formatting.HasFlag(TextFormatting.Underline))
        {
            run.TextDecorations = Windows.UI.Text.TextDecorations.Underline;
        }

        if (textRun.Formatting.HasFlag(TextFormatting.Strikethrough))
        {
            run.TextDecorations = Windows.UI.Text.TextDecorations.Strikethrough;
        }

        if (textRun.Formatting.HasFlag(TextFormatting.Code))
        {
            run.FontFamily = new FontFamily("Consolas");
        }

        return run;
    }

    /// <summary>
    /// Creates an InlineUIContainer with a TagChip for tag elements.
    /// </summary>
    private InlineUIContainer CreateTagInline(Tag tag)
    {
        var tagChip = InlineTagChip.Create(tag.Name);
        
        // Wire up click handling
        tagChip.Tapped += (s, e) =>
        {
            TagClicked?.Invoke(this, tag.Name);
            e.Handled = true;
        };

        return CreateBaselineAlignedContainer(tagChip);
    }

    /// <summary>
    /// Creates an InlineUIContainer with a TodoCheckbox for todo elements.
    /// </summary>
    private InlineUIContainer CreateTodoInline(Todo todo)
    {
        var todoCheckbox = InlineTodoCheckbox.Create(todo.Text, todo.IsCompleted);
        
        // Wire up toggle handling
        todoCheckbox.Toggled += (s, isCompleted) =>
        {
            TodoToggled?.Invoke(this, (todo.Text, isCompleted));
        };

        return CreateBaselineAlignedContainer(todoCheckbox);
    }

    /// <summary>
    /// Creates an InlineUIContainer with an InlineImage for image elements.
    /// </summary>
    private static InlineUIContainer CreateImageInline(ImageInline image)
    {
        // Images intentionally keep the default bottom-on-baseline placement:
        // a tall thumbnail should sit on the text baseline, not be shifted below it.
        return new InlineUIContainer
        {
            Child = InlineImage.Create(image.Source, image.AltText)
        };
    }

    /// <summary>
    /// Wraps a small text-height control in an InlineUIContainer, translated down
    /// so its visual text centerline matches the surrounding run baseline.
    /// InlineUIContainer aligns the child's bottom edge with the text baseline,
    /// which makes chips/checkboxes ride high by the font descent (~4px at 14px).
    /// </summary>
    private static InlineUIContainer CreateBaselineAlignedContainer(FrameworkElement child)
    {
        child.RenderTransform = new TranslateTransform { Y = BaselineDescentOffset };
        return new InlineUIContainer { Child = child };
    }

    // Descent of the default 14px text style; keeps inline controls vertically
    // centered on the visible line instead of floating above the baseline.
    private const double BaselineDescentOffset = 4;
}
