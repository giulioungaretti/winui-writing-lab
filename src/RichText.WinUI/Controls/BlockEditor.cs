using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using RichTexteEditor.Services;
using Microsoft.UI;
using Windows.UI;

namespace RichTexteEditor.Controls;

/// <summary>
/// A block editor control that provides an editable text experience with
/// inline component rendering (tags, todos) using RichTextBlock preview.
/// 
/// Approach: Uses a TextBox for editing and a RichTextBlock overlay for rendering.
/// When focused, shows the TextBox for raw editing.
/// When not focused, shows the RichTextBlock with inline components.
/// </summary>
public sealed class BlockEditor : UserControl
{
    private readonly Grid _container;
    private readonly TextBox _editBox;
    private readonly RichTextBlock _previewBlock;
    private readonly InlineRenderer _renderer;
    
    private bool _isEditing;

    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(BlockEditor),
            new PropertyMetadata(string.Empty, OnTextChanged));

    public static readonly DependencyProperty PlaceholderTextProperty =
        DependencyProperty.Register(
            nameof(PlaceholderText),
            typeof(string),
            typeof(BlockEditor),
            new PropertyMetadata("Type here..."));

    /// <summary>
    /// Gets or sets the text content of the block.
    /// </summary>
    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>
    /// Gets or sets the placeholder text shown when empty.
    /// </summary>
    public string PlaceholderText
    {
        get => (string)GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    /// <summary>
    /// Raised when the text content changes.
    /// </summary>
    public event EventHandler<string>? TextChanged;

    /// <summary>
    /// Raised when a tag is clicked in preview mode.
    /// </summary>
    public event EventHandler<string>? TagClicked;

    /// <summary>
    /// Raised when a todo is toggled in preview mode.
    /// </summary>
    public event EventHandler<(string Text, bool IsCompleted)>? TodoToggled;

    /// <summary>
    /// Raised when Enter is pressed (for creating new blocks).
    /// </summary>
    public event EventHandler? EnterPressed;

    public BlockEditor()
    {
        _renderer = new InlineRenderer();
        _renderer.TagClicked += (s, tag) => TagClicked?.Invoke(this, tag);
        _renderer.TodoToggled += (s, args) => TodoToggled?.Invoke(this, args);

        _container = new Grid
        {
            Background = new SolidColorBrush(Colors.Transparent)
        };

        // Edit TextBox - for raw text editing
        _editBox = new TextBox
        {
            AcceptsReturn = false,  // Single block per editor
            TextWrapping = TextWrapping.Wrap,
            BorderThickness = new Thickness(0),
            Background = new SolidColorBrush(Colors.Transparent),
            Padding = new Thickness(8, 6, 8, 6),
            FontSize = 14,
            Visibility = Visibility.Collapsed
        };
        _editBox.TextChanged += OnEditBoxTextChanged;
        _editBox.GotFocus += OnEditBoxGotFocus;
        _editBox.LostFocus += OnEditBoxLostFocus;
        _editBox.KeyDown += OnEditBoxKeyDown;

        // Preview RichTextBlock - shows rendered inline components
        _previewBlock = new RichTextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Padding = new Thickness(8, 6, 8, 6),
            FontSize = 14,
            IsTextSelectionEnabled = false,  // Tap to edit instead
            Foreground = new SolidColorBrush(Color.FromArgb(255, 55, 53, 47))
        };

        _container.Children.Add(_previewBlock);
        _container.Children.Add(_editBox);

        // Tap preview to start editing
        _previewBlock.Tapped += OnPreviewTapped;

        Content = _container;

        // Initial state: show preview
        UpdatePreview();
    }

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is BlockEditor editor)
        {
            editor.UpdatePreview();
            editor._editBox.Text = e.NewValue as string ?? string.Empty;
        }
    }

    private void OnEditBoxTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isEditing)
        {
            Text = _editBox.Text;
            TextChanged?.Invoke(this, _editBox.Text);
        }
    }

    private void OnEditBoxGotFocus(object sender, RoutedEventArgs e)
    {
        _isEditing = true;
        _editBox.Visibility = Visibility.Visible;
        _previewBlock.Visibility = Visibility.Collapsed;
    }

    private void OnEditBoxLostFocus(object sender, RoutedEventArgs e)
    {
        _isEditing = false;
        _editBox.Visibility = Visibility.Collapsed;
        _previewBlock.Visibility = Visibility.Visible;
        UpdatePreview();
    }

    private void OnEditBoxKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            // Don't insert newline, raise event for creating new block
            EnterPressed?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
        }
        else if (e.Key == Windows.System.VirtualKey.Escape)
        {
            // Blur to exit editing
            _container.Focus(FocusState.Programmatic);
            e.Handled = true;
        }
    }

    private void OnPreviewTapped(object sender, TappedRoutedEventArgs e)
    {
        // Start editing
        _editBox.Focus(FocusState.Programmatic);
        e.Handled = true;
    }

    private void UpdatePreview()
    {
        var text = Text ?? string.Empty;
        
        if (string.IsNullOrEmpty(text))
        {
            // Show placeholder
            _previewBlock.Blocks.Clear();
            var placeholderParagraph = new Microsoft.UI.Xaml.Documents.Paragraph();
            placeholderParagraph.Inlines.Add(new Microsoft.UI.Xaml.Documents.Run
            {
                Text = PlaceholderText,
                Foreground = new SolidColorBrush(Color.FromArgb(128, 55, 53, 47))
            });
            _previewBlock.Blocks.Add(placeholderParagraph);
        }
        else
        {
            _renderer.RenderToRichTextBlock(text, _previewBlock);
        }
    }

    /// <summary>
    /// Focuses the editor for text input.
    /// </summary>
    public void FocusEditor()
    {
        _editBox.Focus(FocusState.Programmatic);
    }

    /// <summary>
    /// Moves cursor to end of text.
    /// </summary>
    public void MoveCursorToEnd()
    {
        _editBox.SelectionStart = _editBox.Text.Length;
    }
}
