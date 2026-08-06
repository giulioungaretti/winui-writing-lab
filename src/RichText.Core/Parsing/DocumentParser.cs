using RichTexteEditor.Core.Model;

namespace RichTexteEditor.Core.Parsing;

/// <summary>
/// Parses multi-line text into a document structure.
/// </summary>
public class DocumentParser
{
    private readonly InlineParser _inlineParser;

    /// <summary>
    /// Creates a new document parser with default inline parser.
    /// </summary>
    public DocumentParser() : this(new InlineParser())
    {
    }

    /// <summary>
    /// Creates a new document parser with the specified inline parser.
    /// </summary>
    /// <param name="inlineParser">The inline parser to use.</param>
    public DocumentParser(InlineParser inlineParser)
    {
        _inlineParser = inlineParser ?? throw new ArgumentNullException(nameof(inlineParser));
    }

    /// <summary>
    /// Parses multi-line text into a document.
    /// </summary>
    /// <param name="text">The text to parse.</param>
    /// <returns>A document containing the parsed content.</returns>
    /// <exception cref="ArgumentNullException">Thrown when text is null.</exception>
    public Document Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (string.IsNullOrEmpty(text))
        {
            return new Document();
        }

        // Split by line endings (handle both \r\n and \n)
        var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

        var blocks = new List<IBlock>();

        foreach (var line in lines)
        {
            var inlines = _inlineParser.Parse(line);
            blocks.Add(new Paragraph(inlines));
        }

        return new Document(blocks);
    }
}
