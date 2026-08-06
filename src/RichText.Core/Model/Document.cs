using System.Collections.Immutable;

namespace RichTexteEditor.Core.Model;

/// <summary>
/// Represents a document containing blocks of content.
/// This is an immutable type.
/// </summary>
public sealed class Document : IEquatable<Document>
{
    private readonly ImmutableArray<IBlock> _blocks;

    /// <summary>
    /// Gets the blocks in this document.
    /// </summary>
    public IReadOnlyList<IBlock> Blocks => _blocks;

    /// <summary>
    /// Gets the number of blocks in the document.
    /// </summary>
    public int BlockCount => _blocks.Length;

    /// <summary>
    /// Gets whether the document is empty (has no blocks).
    /// </summary>
    public bool IsEmpty => _blocks.IsEmpty;

    /// <summary>
    /// Creates an empty document.
    /// </summary>
    public Document() : this(ImmutableArray<IBlock>.Empty)
    {
    }

    /// <summary>
    /// Creates a document with the specified blocks.
    /// </summary>
    /// <param name="blocks">The blocks to include.</param>
    public Document(IEnumerable<IBlock> blocks)
    {
        _blocks = blocks?.ToImmutableArray() ?? ImmutableArray<IBlock>.Empty;
    }

    private Document(ImmutableArray<IBlock> blocks)
    {
        _blocks = blocks;
    }

    /// <summary>
    /// Converts the document to plain text with blocks separated by newlines.
    /// </summary>
    /// <returns>The plain text representation.</returns>
    public string ToPlainText()
    {
        return string.Join("\n", _blocks.Select(b => b.ToPlainText()));
    }

    /// <summary>
    /// Creates a new document with the block appended.
    /// </summary>
    /// <param name="block">The block to append.</param>
    /// <returns>A new Document with the block appended.</returns>
    public Document AppendBlock(IBlock block)
    {
        ArgumentNullException.ThrowIfNull(block);
        return new Document(_blocks.Add(block));
    }

    /// <summary>
    /// Creates a new document with the block inserted at the specified index.
    /// </summary>
    /// <param name="index">The index to insert at.</param>
    /// <param name="block">The block to insert.</param>
    /// <returns>A new Document with the block inserted.</returns>
    public Document InsertBlockAt(int index, IBlock block)
    {
        ArgumentNullException.ThrowIfNull(block);
        return new Document(_blocks.Insert(index, block));
    }

    /// <summary>
    /// Creates a new document with the block at the specified index removed.
    /// </summary>
    /// <param name="index">The index of the block to remove.</param>
    /// <returns>A new Document with the block removed.</returns>
    public Document RemoveBlockAt(int index)
    {
        return new Document(_blocks.RemoveAt(index));
    }

    /// <summary>
    /// Creates a new document with the block at the specified index replaced.
    /// </summary>
    /// <param name="index">The index of the block to replace.</param>
    /// <param name="block">The replacement block.</param>
    /// <returns>A new Document with the block replaced.</returns>
    public Document ReplaceBlockAt(int index, IBlock block)
    {
        ArgumentNullException.ThrowIfNull(block);
        return new Document(_blocks.SetItem(index, block));
    }

    /// <summary>
    /// Finds all tags in the document across all blocks.
    /// </summary>
    /// <returns>A list of all tags.</returns>
    public IReadOnlyList<Tag> FindAllTags()
    {
        return _blocks
            .OfType<Paragraph>()
            .SelectMany(p => p.FindTags())
            .ToList();
    }

    /// <summary>
    /// Finds all todos in the document across all blocks.
    /// </summary>
    /// <returns>A list of all todos.</returns>
    public IReadOnlyList<Todo> FindAllTodos()
    {
        return _blocks
            .OfType<Paragraph>()
            .SelectMany(p => p.FindTodos())
            .ToList();
    }

    /// <summary>
    /// Gets all incomplete todos in the document.
    /// </summary>
    /// <returns>A list of incomplete todos.</returns>
    public IReadOnlyList<Todo> GetIncompleteTodos()
    {
        return FindAllTodos().Where(t => !t.IsCompleted).ToList();
    }

    /// <summary>
    /// Gets all completed todos in the document.
    /// </summary>
    /// <returns>A list of completed todos.</returns>
    public IReadOnlyList<Todo> GetCompletedTodos()
    {
        return FindAllTodos().Where(t => t.IsCompleted).ToList();
    }

    /// <inheritdoc/>
    public bool Equals(Document? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (_blocks.Length != other._blocks.Length) return false;

        for (int i = 0; i < _blocks.Length; i++)
        {
            if (!Equals(_blocks[i], other._blocks[i])) return false;
        }

        return true;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => Equals(obj as Document);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var block in _blocks)
        {
            hash.Add(block);
        }
        return hash.ToHashCode();
    }
}
