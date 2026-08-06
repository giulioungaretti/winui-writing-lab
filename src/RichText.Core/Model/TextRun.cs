namespace RichTexteEditor.Core.Model;

/// <summary>
/// Represents a run of text with optional formatting.
/// This is an immutable value type.
/// </summary>
public sealed record TextRun : IInline, IEquatable<TextRun>
{
    /// <summary>
    /// Gets the text content.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Gets the formatting applied to this text run.
    /// </summary>
    public TextFormatting Formatting { get; }

    /// <summary>
    /// Creates a new text run with the specified text and no formatting.
    /// </summary>
    /// <param name="text">The text content.</param>
    public TextRun(string text) : this(text, TextFormatting.None)
    {
    }

    /// <summary>
    /// Creates a new text run with the specified text and formatting.
    /// </summary>
    /// <param name="text">The text content.</param>
    /// <param name="formatting">The formatting to apply.</param>
    public TextRun(string text, TextFormatting formatting)
    {
        ArgumentNullException.ThrowIfNull(text);
        Text = text;
        Formatting = formatting;
    }

    /// <inheritdoc/>
    public InlineType Type => InlineType.Text;

    /// <inheritdoc/>
    public string ToPlainText() => Text;

    /// <summary>
    /// Creates a new text run with different formatting, preserving the text.
    /// </summary>
    /// <param name="formatting">The new formatting to apply.</param>
    /// <returns>A new TextRun with the specified formatting.</returns>
    public TextRun WithFormatting(TextFormatting formatting) => new(Text, formatting);

    /// <summary>
    /// Creates a new text run with different text, preserving the formatting.
    /// </summary>
    /// <param name="text">The new text content.</param>
    /// <returns>A new TextRun with the specified text.</returns>
    public TextRun WithText(string text) => new(text, Formatting);
}
