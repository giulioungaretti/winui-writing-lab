using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RichTexteEditor.Core.Model;
using RichTexteEditor.Core.Parsing;

namespace RichTexteEditor.ViewModels;

/// <summary>
/// Main ViewModel for the document editor, managing tags and todos.
/// </summary>
public partial class DocumentEditorViewModel : ObservableObject
{
    private readonly DocumentParser _parser = new();
    private readonly InlineParser _inlineParser = new();

    /// <summary>
    /// Collection of unique tags found in the document.
    /// </summary>
    public ObservableCollection<TagViewModel> Tags { get; } = [];

    /// <summary>
    /// Collection of todos found in the document.
    /// </summary>
    public ObservableCollection<TodoViewModel> Todos { get; } = [];

    /// <summary>
    /// The current document text.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CharacterCount))]
    [NotifyPropertyChangedFor(nameof(WordCount))]
    [NotifyPropertyChangedFor(nameof(TagCount))]
    [NotifyPropertyChangedFor(nameof(TodoCount))]
    [NotifyPropertyChangedFor(nameof(CompletedTodoCount))]
    [NotifyPropertyChangedFor(nameof(StatsDisplay))]
    private string _documentText = string.Empty;

    /// <summary>
    /// Status message to display.
    /// </summary>
    [ObservableProperty]
    private string _statusMessage = "Ready";

    /// <summary>
    /// Character count of the document.
    /// </summary>
    public int CharacterCount => DocumentText.Length;

    /// <summary>
    /// Word count of the document.
    /// </summary>
    public int WordCount => string.IsNullOrWhiteSpace(DocumentText) 
        ? 0 
        : DocumentText.Split([' ', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries).Length;

    /// <summary>
    /// Number of unique tags in the document.
    /// </summary>
    public int TagCount => Tags.Count;

    /// <summary>
    /// Number of todos in the document.
    /// </summary>
    public int TodoCount => Todos.Count;

    /// <summary>
    /// Number of completed todos.
    /// </summary>
    public int CompletedTodoCount => Todos.Where(t => t.IsCompleted).Count();

    /// <summary>
    /// Stats display string.
    /// </summary>
    public string StatsDisplay
    {
        get
        {
            var parts = new List<string>
            {
                $"{CharacterCount} chars",
                $"{WordCount} words"
            };

            if (TagCount > 0)
            {
                var suffix = TagCount == 1 ? "" : "s";
                parts.Add($"{TagCount} tag{suffix}");
            }

            if (TodoCount > 0)
            {
                parts.Add($"{CompletedTodoCount}/{TodoCount} todos");
            }

            return string.Join(" | ", parts);
        }
    }

    /// <summary>
    /// Event raised when a tag is clicked.
    /// </summary>
    public event EventHandler<TagClickedEventArgs>? TagClicked;

    /// <summary>
    /// Event raised when a todo state changes.
    /// </summary>
    public event EventHandler<TodoStateChangedEventArgs>? TodoStateChanged;

    /// <summary>
    /// Event raised when the document should be updated in the editor.
    /// </summary>
    public event EventHandler<DocumentUpdateEventArgs>? DocumentUpdateRequested;

    /// <summary>
    /// Updates the document text and refreshes tags/todos.
    /// </summary>
    /// <param name="text">The new document text.</param>
    public void UpdateDocument(string text)
    {
        // Normalize the text (convert emojis for parsing)
        var parseText = text
            .Replace(TodoViewModel.UncheckedEmoji, "[ ]")
            .Replace(TodoViewModel.CheckedEmoji, "[x]");

        DocumentText = text;

        // Parse the document
        var document = _parser.Parse(parseText);
        
        RefreshTags(document, text);
        RefreshTodos(document, text);

        OnPropertyChanged(nameof(StatsDisplay));
    }

    /// <summary>
    /// Processes quick input text and returns formatted text to insert.
    /// </summary>
    /// <param name="input">The quick input text.</param>
    /// <returns>Formatted text with emoji todos.</returns>
    public string ProcessQuickInput(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        // Parse to check what we have
        var inlines = _inlineParser.Parse(input);
        var tagCount = inlines.Where(i => i.Type == InlineType.Tag).Count();
        var todoCount = inlines.Where(i => i.Type == InlineType.Todo).Count();

        // Convert brackets to emojis
        var result = input
            .Replace("[ ]", TodoViewModel.UncheckedEmoji)
            .Replace("[x]", TodoViewModel.CheckedEmoji, StringComparison.OrdinalIgnoreCase);

        // Update status
        if (tagCount > 0 || todoCount > 0)
        {
            var parts = new List<string>();
            if (tagCount > 0)
            {
                parts.Add($"{tagCount} tag{(tagCount == 1 ? "" : "s")}");
            }
            if (todoCount > 0)
            {
                parts.Add($"{todoCount} todo{(todoCount == 1 ? "" : "s")}");
            }
            StatusMessage = $"Added: {string.Join(", ", parts)}";
        }
        else
        {
            StatusMessage = "Added text";
        }

        return result;
    }

    /// <summary>
    /// Clears the document.
    /// </summary>
    [RelayCommand]
    private void Clear()
    {
        DocumentText = string.Empty;
        Tags.Clear();
        Todos.Clear();
        StatusMessage = "Cleared";
        DocumentUpdateRequested?.Invoke(this, new DocumentUpdateEventArgs(string.Empty));
    }

    private void RefreshTags(Document document, string originalText)
    {
        Tags.Clear();

        var coreTags = document.FindAllTags();
        var uniqueNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var tag in coreTags)
        {
            if (uniqueNames.Add(tag.Name))
            {
                // Find position in original text
                var searchText = $"#{tag.Name}";
                var position = originalText.IndexOf(searchText, StringComparison.OrdinalIgnoreCase);

                var tagVm = new TagViewModel(tag.Name, position, position + searchText.Length);
                tagVm.Clicked += OnTagClicked;
                Tags.Add(tagVm);
            }
        }

        OnPropertyChanged(nameof(TagCount));
    }

    private void RefreshTodos(Document document, string originalText)
    {
        // Unsubscribe from old viewmodels
        foreach (var todo in Todos)
        {
            todo.StateChanged -= OnTodoStateChanged;
        }

        Todos.Clear();

        var coreTodos = document.FindAllTodos();
        var currentPosition = 0;

        foreach (var todo in coreTodos)
        {
            // Find position in original text (accounting for emoji representation)
            var uncheckedPattern = $"{TodoViewModel.UncheckedEmoji} {todo.Text}";
            var checkedPattern = $"{TodoViewModel.CheckedEmoji} {todo.Text}";
            
            var position = originalText.IndexOf(
                todo.IsCompleted ? checkedPattern : uncheckedPattern, 
                currentPosition,
                StringComparison.Ordinal);

            if (position < 0)
            {
                // Try bracket style
                var bracketPattern = todo.IsCompleted ? $"[x] {todo.Text}" : $"[ ] {todo.Text}";
                position = originalText.IndexOf(bracketPattern, currentPosition, StringComparison.OrdinalIgnoreCase);
            }

            var endPosition = position >= 0 ? position + (todo.IsCompleted ? checkedPattern.Length : uncheckedPattern.Length) : 0;

            var todoVm = TodoViewModel.FromModel(todo, Math.Max(0, position), endPosition);
            todoVm.StateChanged += OnTodoStateChanged;
            Todos.Add(todoVm);

            if (position >= 0)
            {
                currentPosition = position + 1;
            }
        }

        OnPropertyChanged(nameof(TodoCount));
        OnPropertyChanged(nameof(CompletedTodoCount));
    }

    private void OnTagClicked(object? sender, TagClickedEventArgs e)
    {
        StatusMessage = $"Tag: #{e.TagName}";
        TagClicked?.Invoke(this, e);
    }

    private void OnTodoStateChanged(object? sender, TodoStateChangedEventArgs e)
    {
        StatusMessage = e.IsCompleted ? $"Completed: {e.TodoText}" : $"Reopened: {e.TodoText}";
        OnPropertyChanged(nameof(CompletedTodoCount));
        OnPropertyChanged(nameof(StatsDisplay));
        TodoStateChanged?.Invoke(this, e);
    }
}

/// <summary>
/// Event arguments for document update requests.
/// </summary>
public sealed class DocumentUpdateEventArgs : EventArgs
{
    /// <summary>
    /// The new document text.
    /// </summary>
    public string Text { get; }

    public DocumentUpdateEventArgs(string text)
    {
        Text = text;
    }
}
