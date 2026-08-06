namespace RichTexteEditor.Core.Model;

/// <summary>
/// Formatting options that can be applied to text.
/// </summary>
[Flags]
public enum TextFormatting
{
    /// <summary>
    /// No formatting applied.
    /// </summary>
    None = 0,

    /// <summary>
    /// Bold text.
    /// </summary>
    Bold = 1,

    /// <summary>
    /// Italic text.
    /// </summary>
    Italic = 2,

    /// <summary>
    /// Underlined text.
    /// </summary>
    Underline = 4,

    /// <summary>
    /// Strikethrough text.
    /// </summary>
    Strikethrough = 8,

    /// <summary>
    /// Monospace/code text.
    /// </summary>
    Code = 16
}
