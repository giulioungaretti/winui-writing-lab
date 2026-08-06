using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace RichTexteEditor.Controls;

/// <summary>
/// A lightweight inline todo checkbox control designed for use within InlineUIContainer.
/// Renders as a Notion-like clickable checkbox.
/// </summary>
public sealed class InlineTodoCheckbox : UserControl
{
    // Notion-like colors
    private static readonly Color UncheckedColor = Color.FromArgb(255, 180, 180, 180);  // Light gray
    private static readonly Color CheckedColor = Color.FromArgb(255, 120, 119, 198);    // Purple when done
    private static readonly Color TextNormalColor = Color.FromArgb(255, 55, 53, 47);     // Normal text (dark)
    private static readonly Color TextCompletedColor = Color.FromArgb(255, 155, 155, 155);  // Faded when done

    public static readonly DependencyProperty IsCompletedProperty =
        DependencyProperty.Register(
            nameof(IsCompleted),
            typeof(bool),
            typeof(InlineTodoCheckbox),
            new PropertyMetadata(false, OnIsCompletedChanged));

    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(InlineTodoCheckbox),
            new PropertyMetadata(string.Empty, OnTextChanged));

    /// <summary>
    /// Gets or sets whether the todo is completed.
    /// </summary>
    public bool IsCompleted
    {
        get => (bool)GetValue(IsCompletedProperty);
        set => SetValue(IsCompletedProperty, value);
    }

    /// <summary>
    /// Gets or sets the todo text.
    /// </summary>
    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>
    /// Raised when the checkbox is toggled.
    /// </summary>
    public event EventHandler<bool>? Toggled;

    private readonly CheckBox _checkbox;
    private readonly TextBlock _textTextBlock;
    private readonly StackPanel _panel;

    public InlineTodoCheckbox()
    {
        _panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            VerticalAlignment = VerticalAlignment.Center
        };

        // Use actual CheckBox for better accessibility and native rendering.
        // Constrain to the 20x20 glyph so the control doesn't inflate the
        // text line height (default CheckBox reserves 32px vertically).
        _checkbox = new CheckBox
        {
            MinWidth = 0,
            MinHeight = 0,
            Width = 20,
            Height = 20,
            Padding = new Thickness(0),
            VerticalAlignment = VerticalAlignment.Center
        };
        _checkbox.Checked += OnCheckboxToggled;
        _checkbox.Unchecked += OnCheckboxToggled;

        // Todo text
        _textTextBlock = new TextBlock
        {
            FontSize = 14,
            Foreground = new SolidColorBrush(TextNormalColor),
            VerticalAlignment = VerticalAlignment.Center
        };

        _panel.Children.Add(_checkbox);
        _panel.Children.Add(_textTextBlock);

        Margin = new Thickness(2, 0, 2, 0);
        VerticalAlignment = VerticalAlignment.Center;
        Content = _panel;

        UpdateVisualState();
    }

    public InlineTodoCheckbox(string text, bool isCompleted = false) : this()
    {
        Text = text;
        IsCompleted = isCompleted;
    }

    private static void OnIsCompletedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is InlineTodoCheckbox checkbox)
        {
            checkbox._checkbox.IsChecked = (bool)e.NewValue;
            checkbox.UpdateVisualState();
        }
    }

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is InlineTodoCheckbox checkbox && e.NewValue is string text)
        {
            checkbox._textTextBlock.Text = text;
        }
    }

    private void UpdateVisualState()
    {
        if (IsCompleted)
        {
            // Completed: faded text with strikethrough
            _textTextBlock.Foreground = new SolidColorBrush(TextCompletedColor);
            _textTextBlock.TextDecorations = Windows.UI.Text.TextDecorations.Strikethrough;
        }
        else
        {
            // Incomplete: normal dark text
            _textTextBlock.Foreground = new SolidColorBrush(TextNormalColor);
            _textTextBlock.TextDecorations = Windows.UI.Text.TextDecorations.None;
        }
    }

    private void OnCheckboxToggled(object sender, RoutedEventArgs e)
    {
        var newValue = _checkbox.IsChecked ?? false;
        if (newValue != IsCompleted)
        {
            IsCompleted = newValue;
            Toggled?.Invoke(this, newValue);
        }
    }

    /// <summary>
    /// Creates an InlineTodoCheckbox configured for inline display.
    /// </summary>
    public static InlineTodoCheckbox Create(string text, bool isCompleted = false)
    {
        return new InlineTodoCheckbox(text, isCompleted);
    }
}
