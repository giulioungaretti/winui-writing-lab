using System.Collections.Immutable;

namespace RichTexteEditor.Core.Model;

/// <summary>
/// Represents a paragraph block containing inline elements.
/// This is an immutable type.
/// </summary>
public sealed class Paragraph : IBlock, IEquatable<Paragraph>
{
    private readonly ImmutableArray<IInline> _inlines;

    /// <summary>
    /// Gets the inline elements in this paragraph.
    /// </summary>
    public IReadOnlyList<IInline> Inlines => _inlines;

    /// <summary>
    /// Creates an empty paragraph.
    /// </summary>
    public Paragraph() : this(ImmutableArray<IInline>.Empty)
    {
    }

    /// <summary>
    /// Creates a paragraph with the specified inlines.
    /// </summary>
    /// <param name="inlines">The inline elements.</param>
    public Paragraph(IEnumerable<IInline> inlines)
    {
        _inlines = inlines?.ToImmutableArray() ?? ImmutableArray<IInline>.Empty;
    }

    private Paragraph(ImmutableArray<IInline> inlines)
    {
        _inlines = inlines;
    }

    /// <inheritdoc/>
    public BlockType Type => BlockType.Paragraph;

    /// <inheritdoc/>
    public bool IsEmpty => _inlines.IsEmpty;

    /// <inheritdoc/>
    public string ToPlainText()
    {
        return string.Concat(_inlines.Select(i => i.ToPlainText()));
    }

    /// <summary>
    /// Creates a new paragraph with the inline appended.
    /// </summary>
    /// <param name="inline">The inline to append.</param>
    /// <returns>A new Paragraph with the inline appended.</returns>
    public Paragraph AppendInline(IInline inline)
    {
        ArgumentNullException.ThrowIfNull(inline);
        return new Paragraph(_inlines.Add(inline));
    }

    /// <summary>
    /// Creates a new paragraph with the inline inserted at the specified index.
    /// </summary>
    /// <param name="index">The index to insert at.</param>
    /// <param name="inline">The inline to insert.</param>
    /// <returns>A new Paragraph with the inline inserted.</returns>
    public Paragraph InsertInlineAt(int index, IInline inline)
    {
        ArgumentNullException.ThrowIfNull(inline);
        return new Paragraph(_inlines.Insert(index, inline));
    }

    /// <summary>
    /// Creates a new paragraph with the inline at the specified index removed.
    /// </summary>
    /// <param name="index">The index of the inline to remove.</param>
    /// <returns>A new Paragraph with the inline removed.</returns>
    public Paragraph RemoveInlineAt(int index)
    {
        return new Paragraph(_inlines.RemoveAt(index));
    }

    /// <summary>
    /// Creates a new paragraph with the inline at the specified index replaced.
    /// </summary>
    /// <param name="index">The index of the inline to replace.</param>
    /// <param name="inline">The replacement inline.</param>
    /// <returns>A new Paragraph with the inline replaced.</returns>
    public Paragraph ReplaceInlineAt(int index, IInline inline)
    {
        ArgumentNullException.ThrowIfNull(inline);
        return new Paragraph(_inlines.SetItem(index, inline));
    }

    /// <summary>
    /// Finds all tags in this paragraph.
    /// </summary>
    /// <returns>A list of all tags.</returns>
    public IReadOnlyList<Tag> FindTags()
    {
        return _inlines.OfType<Tag>().ToList();
    }

    /// <summary>
    /// Finds all todos in this paragraph.
    /// </summary>
    /// <returns>A list of all todos.</returns>
    public IReadOnlyList<Todo> FindTodos()
    {
        return _inlines.OfType<Todo>().ToList();
    }

    /// <inheritdoc/>
    public bool Equals(Paragraph? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (_inlines.Length != other._inlines.Length) return false;

        for (int i = 0; i < _inlines.Length; i++)
        {
            if (!Equals(_inlines[i], other._inlines[i])) return false;
        }

        return true;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => Equals(obj as Paragraph);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var inline in _inlines)
        {
            hash.Add(inline);
        }
        return hash.ToHashCode();
    }
}
