using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using RichTexteEditor.ViewModels;
using Windows.UI;

namespace RichTexteEditor.Controls;

/// <summary>
/// A chip-style control for displaying tags with Fluent Design styling.
/// </summary>
public sealed class TagChip : Button
{
    /// <summary>
    /// The tag view model.
    /// </summary>
    public static new readonly DependencyProperty TagProperty =
        DependencyProperty.Register(
            nameof(Tag),
            typeof(TagViewModel),
            typeof(TagChip),
            new PropertyMetadata(null, OnTagChanged));

    /// <summary>
    /// Gets or sets the tag view model.
    /// </summary>
    public new TagViewModel? Tag
    {
        get => (TagViewModel?)GetValue(TagProperty);
        set => SetValue(TagProperty, value);
    }

    public TagChip()
    {
        DefaultStyleKey = typeof(TagChip);
        ApplyChipStyle();
        Click += OnChipClick;
    }

    private static void OnTagChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TagChip chip)
        {
            chip.UpdateContent();
        }
    }

    private void UpdateContent()
    {
        if (Tag != null)
        {
            Content = Tag.DisplayText;
        }
    }

    private void ApplyChipStyle()
    {
        // Fluent Design tag chip styling
        Background = new SolidColorBrush(Color.FromArgb(40, 0, 120, 212));
        Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 120, 212));
        BorderThickness = new Thickness(0);
        Padding = new Thickness(12, 4, 12, 4);
        CornerRadius = new CornerRadius(12);
        FontSize = 12;
        MinHeight = 0;
        MinWidth = 0;
    }

    private void OnChipClick(object sender, RoutedEventArgs e)
    {
        Tag?.ClickCommand.Execute(null);
    }
}
