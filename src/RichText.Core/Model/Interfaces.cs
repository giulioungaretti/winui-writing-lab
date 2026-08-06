namespace RichTexteEditor.Core.Model;

/// <summary>
/// Base interface for all inline elements within a block.
/// </summary>
public interface IInline
{
    /// <summary>
    /// Gets the type of this inline element.
    /// </summary>
    InlineType Type { get; }

    /// <summary>
    /// Converts this inline to its plain text representation.
    /// </summary>
    string ToPlainText();
}

/// <summary>
/// Base interface for all block-level elements in a document.
/// </summary>
public interface IBlock
{
    /// <summary>
    /// Gets the type of this block element.
    /// </summary>
    BlockType Type { get; }

    /// <summary>
    /// Gets whether this block is empty (has no content).
    /// </summary>
    bool IsEmpty { get; }

    /// <summary>
    /// Converts this block to its plain text representation.
    /// </summary>
    string ToPlainText();
}
