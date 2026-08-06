namespace RichTexteEditor.Core.Model;

/// <summary>
/// Represents a todo/task item in the document.
/// This is an immutable value type.
/// </summary>
public sealed record Todo : IInline, IEquatable<Todo>
{
    /// <summary>
    /// Gets the todo text/description.
    /// </summary>
    public string Text { get; init; }

    /// <summary>
    /// Gets whether the todo is completed.
    /// </summary>
    public bool IsCompleted { get; init; }

    /// <summary>
    /// Gets the priority of the todo.
    /// </summary>
    public TodoPriority Priority { get; init; }

    /// <summary>
    /// Gets the optional due date for the todo.
    /// </summary>
    public DateOnly? DueDate { get; init; }

    /// <summary>
    /// Creates a new todo with the specified text.
    /// </summary>
    /// <param name="text">The todo text/description.</param>
    /// <param name="isCompleted">Whether the todo is completed.</param>
    /// <param name="priority">The priority level.</param>
    /// <param name="dueDate">Optional due date.</param>
    /// <exception cref="ArgumentNullException">Thrown when text is null.</exception>
    /// <exception cref="ArgumentException">Thrown when text is empty or whitespace.</exception>
    public Todo(
        string text,
        bool isCompleted = false,
        TodoPriority priority = TodoPriority.Normal,
        DateOnly? dueDate = null)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Todo text cannot be empty or whitespace.", nameof(text));
        }

        Text = text;
        IsCompleted = isCompleted;
        Priority = priority;
        DueDate = dueDate;
    }

    /// <inheritdoc/>
    public InlineType Type => InlineType.Todo;

    /// <inheritdoc/>
    public string ToPlainText()
    {
        var checkbox = IsCompleted ? "[x]" : "[ ]";
        return $"{checkbox} {Text}";
    }

    /// <summary>
    /// Creates a new todo with the completion state toggled.
    /// </summary>
    /// <returns>A new Todo with toggled completion state.</returns>
    public Todo ToggleCompleted() => this with { IsCompleted = !IsCompleted };

    /// <summary>
    /// Creates a new todo with different text, preserving other properties.
    /// </summary>
    /// <param name="text">The new text.</param>
    /// <returns>A new Todo with the specified text.</returns>
    public Todo WithText(string text) => this with { Text = text };

    /// <summary>
    /// Creates a new todo with different priority.
    /// </summary>
    /// <param name="priority">The new priority.</param>
    /// <returns>A new Todo with the specified priority.</returns>
    public Todo WithPriority(TodoPriority priority) => this with { Priority = priority };

    /// <summary>
    /// Creates a new todo with a different due date.
    /// </summary>
    /// <param name="dueDate">The new due date (or null to remove).</param>
    /// <returns>A new Todo with the specified due date.</returns>
    public Todo WithDueDate(DateOnly? dueDate) => this with { DueDate = dueDate };
}
