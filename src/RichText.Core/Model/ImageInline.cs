namespace RichTexteEditor.Core.Model;

/// <summary>
/// Represents an embedded inline image, authored as <c>![alt](source)</c>.
/// This is an immutable value type. The source may be a relative path,
/// absolute path, or URI; resolution is left to the renderer.
/// </summary>
public sealed record ImageInline : IInline, IEquatable<ImageInline>
{
    /// <summary>
    /// Gets the image source (path or URI).
    /// </summary>
    public string Source { get; }

    /// <summary>
    /// Gets the alternative text describing the image.
    /// </summary>
    public string AltText { get; }

    /// <summary>
    /// Creates a new inline image.
    /// </summary>
    /// <param name="source">The image source path or URI.</param>
    /// <param name="altText">The alternative text (may be empty).</param>
    /// <exception cref="ArgumentNullException">Thrown when source or altText is null.</exception>
    /// <exception cref="ArgumentException">Thrown when source is empty or whitespace.</exception>
    public ImageInline(string source, string altText = "")
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(altText);

        if (string.IsNullOrWhiteSpace(source))
        {
            throw new ArgumentException("Image source cannot be empty or whitespace.", nameof(source));
        }

        Source = source.Trim();
        AltText = altText;
    }

    /// <inheritdoc/>
    public InlineType Type => InlineType.Image;

    /// <inheritdoc/>
    public string ToPlainText() => $"![{AltText}]({Source})";
}
