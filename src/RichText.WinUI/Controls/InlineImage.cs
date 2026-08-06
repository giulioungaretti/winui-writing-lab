using System;
using System.IO;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.UI;

namespace RichTexteEditor.Controls;

/// <summary>
/// A lightweight inline image control designed for use within InlineUIContainer.
/// Renders a thumbnail with rounded corners; the alt text is exposed through
/// automation and shown as a tooltip and as a fallback when loading fails.
/// </summary>
public sealed class InlineImage : UserControl
{
    private const double DefaultMaxHeight = 160;

    private static readonly Color FallbackForeground = Color.FromArgb(255, 155, 155, 155);
    private static readonly Color FallbackBackground = Color.FromArgb(30, 120, 119, 198);

    private readonly Image _image;
    private readonly Border _border;

    /// <summary>
    /// Gets the image source path or URI as authored.
    /// </summary>
    public string SourcePath { get; }

    /// <summary>
    /// Gets the alternative text.
    /// </summary>
    public string AltText { get; }

    public InlineImage(string source, string altText)
    {
        ArgumentNullException.ThrowIfNull(source);
        SourcePath = source;
        AltText = altText ?? string.Empty;

        _image = new Image
        {
            Stretch = Stretch.Uniform,
            MaxHeight = DefaultMaxHeight
        };
        _image.ImageFailed += OnImageFailed;

        _border = new Border
        {
            CornerRadius = new CornerRadius(6),
            Child = _image
        };

        Margin = new Thickness(2, 2, 2, 2);
        Content = _border;

        var label = string.IsNullOrWhiteSpace(AltText) ? SourcePath : AltText;
        AutomationProperties.SetName(this, label);
        ToolTipService.SetToolTip(this, label);

        var uri = ResolveSource(source);
        if (uri is not null)
        {
            _image.Source = new BitmapImage(uri);
        }
        else
        {
            ShowFallback();
        }
    }

    private static Uri? ResolveSource(string source)
    {
        if (Uri.TryCreate(source, UriKind.Absolute, out var absolute))
        {
            return absolute;
        }

        // Relative path: resolve against the application base directory.
        var local = Path.Combine(AppContext.BaseDirectory, source);
        return File.Exists(local) ? new Uri(local) : null;
    }

    private void OnImageFailed(object sender, ExceptionRoutedEventArgs e) => ShowFallback();

    private void ShowFallback()
    {
        _border.Background = new SolidColorBrush(FallbackBackground);
        _border.Padding = new Thickness(8, 4, 8, 4);
        _border.Child = new TextBlock
        {
            Text = $"\uE91B {(string.IsNullOrWhiteSpace(AltText) ? SourcePath : AltText)}",
            FontSize = 13,
            Foreground = new SolidColorBrush(FallbackForeground)
        };
    }

    /// <summary>
    /// Creates an InlineImage configured for inline display.
    /// </summary>
    public static InlineImage Create(string source, string altText) => new(source, altText);
}
