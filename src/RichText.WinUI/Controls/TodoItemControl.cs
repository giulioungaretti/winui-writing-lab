using System.Collections.Generic;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using RichTexteEditor.ViewModels;
using Windows.UI;

namespace RichTexteEditor.Controls;

/// <summary>
/// A control for displaying and interacting with todo items.
/// </summary>
public sealed class TodoItemControl : Button
{
    // Colors
    private static readonly Color PendingColor = Color.FromArgb(255, 107, 107, 107);
    private static readonly Color CompletedColor = Color.FromArgb(255, 16, 124, 16);
    private static readonly Color OverdueColor = Color.FromArgb(255, 196, 43, 28);

    /// <summary>
    /// The todo view model.
    /// </summary>
    public static readonly DependencyProperty TodoProperty =
        DependencyProperty.Register(
            nameof(Todo),
            typeof(TodoViewModel),
            typeof(TodoItemControl),
            new PropertyMetadata(null, OnTodoChanged));

    /// <summary>
    /// Gets or sets the todo view model.
    /// </summary>
    public TodoViewModel? Todo
    {
        get => (TodoViewModel?)GetValue(TodoProperty);
        set => SetValue(TodoProperty, value);
    }

    private TextBlock? _checkboxText;
    private TextBlock? _todoText;
    private TextBlock? _metaText;

    public TodoItemControl()
    {
        DefaultStyleKey = typeof(TodoItemControl);
        ApplyControlStyle();
        Click += OnControlClick;
    }

    private static void OnTodoChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TodoItemControl control)
        {
            // Unsubscribe from old
            if (e.OldValue is TodoViewModel oldVm)
            {
                oldVm.PropertyChanged -= control.OnTodoPropertyChanged;
            }

            // Subscribe to new
            if (e.NewValue is TodoViewModel newVm)
            {
                newVm.PropertyChanged += control.OnTodoPropertyChanged;
            }

            control.UpdateContent();
        }
    }

    private void OnTodoPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // Update UI when ViewModel properties change
        if (e.PropertyName is nameof(TodoViewModel.IsCompleted) or 
            nameof(TodoViewModel.Text) or 
            nameof(TodoViewModel.CheckboxEmoji))
        {
            UpdateContent();
        }
    }

    private void UpdateContent()
    {
        if (Todo == null)
        {
            Content = null;
            return;
        }

        // Create content panel
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };

        // Checkbox emoji
        _checkboxText = new TextBlock
        {
            Text = Todo.CheckboxEmoji,
            FontSize = 16,
            VerticalAlignment = VerticalAlignment.Center
        };
        panel.Children.Add(_checkboxText);

        // Todo text
        _todoText = new TextBlock
        {
            Text = Todo.Text,
            VerticalAlignment = VerticalAlignment.Center,
            TextDecorations = Todo.IsCompleted 
                ? Windows.UI.Text.TextDecorations.Strikethrough 
                : Windows.UI.Text.TextDecorations.None
        };
        panel.Children.Add(_todoText);

        // Meta info (priority, due date)
        var metaParts = new List<string>();
        if (!string.IsNullOrEmpty(Todo.PriorityDisplay))
        {
            metaParts.Add(Todo.PriorityDisplay);
        }
        if (Todo.HasDueDate)
        {
            metaParts.Add(Todo.DueDateDisplay);
        }

        if (metaParts.Count > 0)
        {
            _metaText = new TextBlock
            {
                Text = string.Join(" · ", metaParts),
                FontSize = 11,
                Opacity = 0.7,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 0, 0)
            };
            panel.Children.Add(_metaText);
        }

        Content = panel;
        UpdateColors();
    }

    private void UpdateColors()
    {
        if (Todo == null) return;

        Color textColor;
        if (Todo.IsOverdue)
        {
            textColor = OverdueColor;
        }
        else if (Todo.IsCompleted)
        {
            textColor = CompletedColor;
        }
        else
        {
            textColor = PendingColor;
        }

        if (_todoText != null)
        {
            _todoText.Foreground = new SolidColorBrush(textColor);
        }
    }

    private void ApplyControlStyle()
    {
        Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
        BorderThickness = new Thickness(0);
        Padding = new Thickness(4, 2, 4, 2);
        HorizontalAlignment = HorizontalAlignment.Stretch;
        HorizontalContentAlignment = HorizontalAlignment.Left;
        MinHeight = 0;
        MinWidth = 0;
    }

    private void OnControlClick(object sender, RoutedEventArgs e)
    {
        Todo?.ToggleCommand.Execute(null);
        UpdateContent();
    }
}
