namespace RichTexteEditor.Core.Model;

/// <summary>
/// Defines the type of inline element.
/// </summary>
public enum InlineType
{
    /// <summary>
    /// Plain or formatted text.
    /// </summary>
    Text,

    /// <summary>
    /// A hashtag reference.
    /// </summary>
    Tag,

    /// <summary>
    /// A todo/task item.
    /// </summary>
    Todo,

    /// <summary>
    /// A hyperlink.
    /// </summary>
    Link,

    /// <summary>
    /// An embedded image.
    /// </summary>
    Image
}

/// <summary>
/// Defines the type of block element.
/// </summary>
public enum BlockType
{
    /// <summary>
    /// A paragraph containing inline elements.
    /// </summary>
    Paragraph,

    /// <summary>
    /// A heading with level.
    /// </summary>
    Heading,

    /// <summary>
    /// A list item.
    /// </summary>
    ListItem
}

/// <summary>
/// Priority levels for todo items.
/// </summary>
public enum TodoPriority
{
    /// <summary>
    /// Low priority.
    /// </summary>
    Low,

    /// <summary>
    /// Normal/default priority.
    /// </summary>
    Normal,

    /// <summary>
    /// High priority.
    /// </summary>
    High,

    /// <summary>
    /// Urgent/critical priority.
    /// </summary>
    Urgent
}
