using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace RichTexteEditor.Controls;

/// <summary>
/// A lightweight inline tag chip control designed for use within InlineUIContainer.
/// Renders as a Notion-like pill/badge with purple styling.
/// </summary>
public sealed class InlineTagChip : UserControl
{
    private static readonly Color TagForeground = Color.FromArgb(255, 120, 119, 198);  // Soft purple text
    private static readonly Color TagBackground = Color.FromArgb(40, 120, 119, 198);   // Very subtle purple bg

    public static readonly DependencyProperty TagNameProperty =
        DependencyProperty.Register(
            nameof(TagName),
            typeof(string),
            typeof(InlineTagChip),
            new PropertyMetadata(string.Empty, OnTagNameChanged));

    /// <summary>
    /// Gets or sets the tag name (without the # prefix).
    /// </summary>
    public string TagName
    {
        get => (string)GetValue(TagNameProperty);
        set => SetValue(TagNameProperty, value);
    }

    private readonly TextBlock _textBlock;
    private readonly Border _border;

    public InlineTagChip()
    {
        _textBlock = new TextBlock
        {
            Foreground = new SolidColorBrush(TagForeground),
            FontSize = 13,
            VerticalAlignment = VerticalAlignment.Center
        };

        // Pill/badge styling using Border
        _border = new Border
        {
            Background = new SolidColorBrush(TagBackground),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(6, 2, 6, 2),
            Child = _textBlock
        };

        Margin = new Thickness(2, 0, 2, 0);
        VerticalAlignment = VerticalAlignment.Center;
        Content = _border;
    }

    public InlineTagChip(string tagName) : this()
    {
        TagName = tagName;
    }

    private static void OnTagNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is InlineTagChip chip && e.NewValue is string name)
        {
            chip._textBlock.Text = $"#{name}";
        }
    }

    /// <summary>
    /// Creates an InlineTagChip configured for inline display.
    /// </summary>
    public static InlineTagChip Create(string tagName)
    {
        return new InlineTagChip(tagName);
    }
}
