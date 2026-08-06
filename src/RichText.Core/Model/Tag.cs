using System.Text.RegularExpressions;

namespace RichTexteEditor.Core.Model;

/// <summary>
/// Represents a hashtag reference in the document.
/// This is an immutable value type.
/// </summary>
public sealed partial record Tag : IInline, IEquatable<Tag>
{
    private static readonly Regex ValidTagPattern = TagNameRegex();

    /// <summary>
    /// Gets the tag name (without the # prefix).
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Creates a new tag with the specified name.
    /// </summary>
    /// <param name="name">The tag name (with or without # prefix).</param>
    /// <exception cref="ArgumentNullException">Thrown when name is null.</exception>
    /// <exception cref="ArgumentException">Thrown when name is invalid.</exception>
    public Tag(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        // Strip leading # if present
        var normalizedName = name.StartsWith('#') ? name[1..] : name;

        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            throw new ArgumentException("Tag name cannot be empty or whitespace.", nameof(name));
        }

        if (!ValidTagPattern.IsMatch(normalizedName))
        {
            throw new ArgumentException(
                "Tag name must start with a letter and contain only letters, numbers, hyphens, or underscores.",
                nameof(name));
        }

        Name = normalizedName;
    }

    /// <inheritdoc/>
    public InlineType Type => InlineType.Tag;

    /// <inheritdoc/>
    public string ToPlainText() => $"#{Name}";

    /// <inheritdoc/>
    public override string ToString() => ToPlainText();

    /// <summary>
    /// Checks if this tag matches another tag (case-insensitive comparison).
    /// </summary>
    /// <param name="other">The other tag to compare with.</param>
    /// <returns>True if the tags match (case-insensitive).</returns>
    public bool Matches(Tag other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if this tag matches a tag name (case-insensitive comparison).
    /// </summary>
    /// <param name="tagName">The tag name to compare with (with or without # prefix).</param>
    /// <returns>True if the tags match (case-insensitive).</returns>
    public bool Matches(string tagName)
    {
        var normalizedName = tagName.StartsWith('#') ? tagName[1..] : tagName;
        return string.Equals(Name, normalizedName, StringComparison.OrdinalIgnoreCase);
    }

    [GeneratedRegex(@"^[a-zA-Z][a-zA-Z0-9_-]*$", RegexOptions.Compiled)]
    private static partial Regex TagNameRegex();
}
