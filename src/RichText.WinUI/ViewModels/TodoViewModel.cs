using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RichTexteEditor.Core.Model;

namespace RichTexteEditor.ViewModels;

/// <summary>
/// ViewModel for a todo item, following MVVM Toolkit patterns.
/// </summary>
public partial class TodoViewModel : ObservableObject
{
    // Circle checkbox characters (Notion-like style)
    public const string UncheckedBox = "?";
    public const string CheckedBox = "?";
    
    // Legacy emoji support
    public const string UncheckedEmoji = "?";
    public const string CheckedEmoji = "?";

    /// <summary>
    /// The todo text/description.
    /// </summary>
    [ObservableProperty]
    private string _text = string.Empty;

    /// <summary>
    /// Whether the todo is completed.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CheckboxEmoji))]
    [NotifyPropertyChangedFor(nameof(DisplayText))]
    private bool _isCompleted;

    /// <summary>
    /// The priority of the todo.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PriorityDisplay))]
    private TodoPriority _priority = TodoPriority.Normal;

    /// <summary>
    /// Optional due date.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DueDateDisplay))]
    [NotifyPropertyChangedFor(nameof(HasDueDate))]
    [NotifyPropertyChangedFor(nameof(IsOverdue))]
    private DateTime? _dueDate;

    /// <summary>
    /// The position of this todo in the document.
    /// </summary>
    [ObservableProperty]
    private int _startPosition;

    /// <summary>
    /// The end position of this todo in the document.
    /// </summary>
    [ObservableProperty]
    private int _endPosition;

    /// <summary>
    /// The emoji to display for the checkbox state.
    /// </summary>
    public string CheckboxEmoji => IsCompleted ? CheckedEmoji : UncheckedEmoji;

    /// <summary>
    /// The full display text with checkbox.
    /// </summary>
    public string DisplayText => $"{CheckboxEmoji} {Text}";

    /// <summary>
    /// Display string for priority.
    /// </summary>
    public string PriorityDisplay => Priority switch
    {
        TodoPriority.High => "?? High",
        TodoPriority.Low => "?? Low",
        _ => ""
    };

    /// <summary>
    /// Display string for due date.
    /// </summary>
    public string DueDateDisplay => DueDate?.ToString("MMM d") ?? string.Empty;

    /// <summary>
    /// Whether the todo has a due date.
    /// </summary>
    public bool HasDueDate => DueDate.HasValue;

    /// <summary>
    /// Whether the todo is overdue.
    /// </summary>
    public bool IsOverdue => DueDate.HasValue && DueDate.Value < DateTime.Today && !IsCompleted;

    /// <summary>
    /// Event raised when the todo state changes.
    /// </summary>
    public event EventHandler<TodoStateChangedEventArgs>? StateChanged;

    /// <summary>
    /// Creates a new TodoViewModel.
    /// </summary>
    public TodoViewModel()
    {
    }

    /// <summary>
    /// Creates a new TodoViewModel with the specified text.
    /// </summary>
    /// <param name="text">The todo text.</param>
    /// <param name="isCompleted">Whether the todo is completed.</param>
    public TodoViewModel(string text, bool isCompleted = false)
    {
        Text = text;
        IsCompleted = isCompleted;
    }

    /// <summary>
    /// Creates a TodoViewModel from a Core model Todo.
    /// </summary>
    /// <param name="todo">The core Todo model.</param>
    /// <param name="startPosition">Start position in document.</param>
    /// <param name="endPosition">End position in document.</param>
    /// <returns>A new TodoViewModel.</returns>
    public static TodoViewModel FromModel(Todo todo, int startPosition = 0, int endPosition = 0)
    {
        return new TodoViewModel
        {
            Text = todo.Text,
            IsCompleted = todo.IsCompleted,
            Priority = todo.Priority,
            DueDate = todo.DueDate.HasValue ? todo.DueDate.Value.ToDateTime(TimeOnly.MinValue) : null,
            StartPosition = startPosition,
            EndPosition = endPosition
        };
    }

    /// <summary>
    /// Command to toggle the completion state.
    /// </summary>
    [RelayCommand]
    private void Toggle()
    {
        IsCompleted = !IsCompleted;
        StateChanged?.Invoke(this, new TodoStateChangedEventArgs(Text, IsCompleted, StartPosition, EndPosition));
    }

    /// <summary>
    /// Called when IsCompleted changes.
    /// </summary>
    partial void OnIsCompletedChanged(bool value)
    {
        // Notify that dependent properties have changed
        OnPropertyChanged(nameof(IsOverdue));
    }
}

/// <summary>
/// Event arguments for todo state change events.
/// </summary>
public sealed class TodoStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// The todo text.
    /// </summary>
    public string TodoText { get; }

    /// <summary>
    /// The new completion state.
    /// </summary>
    public bool IsCompleted { get; }

    /// <summary>
    /// Start position in document.
    /// </summary>
    public int StartPosition { get; }

    /// <summary>
    /// End position in document.
    /// </summary>
    public int EndPosition { get; }

    public TodoStateChangedEventArgs(string todoText, bool isCompleted, int startPosition, int endPosition)
    {
        TodoText = todoText;
        IsCompleted = isCompleted;
        StartPosition = startPosition;
        EndPosition = endPosition;
    }
}
