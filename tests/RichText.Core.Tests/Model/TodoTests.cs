using RichTexteEditor.Core.Model;
using Xunit;

namespace RichTexteEditor.Core.Tests.Model;

/// <summary>
/// Tests for Todo inline element - checkable task items.
/// </summary>
public class TodoTests
{
    [Fact]
    public void WhenCreatedWithTextThenTextIsSet()
    {
        var todo = new Todo("Buy groceries");

        Assert.Equal("Buy groceries", todo.Text);
    }

    [Fact]
    public void WhenCreatedWithNullTextThenThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new Todo(null!));
    }

    [Fact]
    public void WhenCreatedWithEmptyTextThenThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new Todo(string.Empty));
    }

    [Fact]
    public void WhenCreatedWithWhitespaceTextThenThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new Todo("   "));
    }

    [Fact]
    public void WhenCreatedWithoutCompletedThenDefaultsToFalse()
    {
        var todo = new Todo("Buy groceries");

        Assert.False(todo.IsCompleted);
    }

    [Fact]
    public void WhenCreatedWithCompletedTrueThenIsCompleted()
    {
        var todo = new Todo("Buy groceries", isCompleted: true);

        Assert.True(todo.IsCompleted);
    }

    [Fact]
    public void WhenToggleCompletedCalledThenReturnsNewInstanceWithToggledState()
    {
        var original = new Todo("Buy groceries", isCompleted: false);

        var toggled = original.ToggleCompleted();

        Assert.False(original.IsCompleted);
        Assert.True(toggled.IsCompleted);
        Assert.Equal("Buy groceries", toggled.Text);
    }

    [Fact]
    public void WhenToggleCompletedCalledTwiceThenReturnsToOriginalState()
    {
        var original = new Todo("Buy groceries", isCompleted: false);

        var toggled = original.ToggleCompleted().ToggleCompleted();

        Assert.False(toggled.IsCompleted);
    }

    [Fact]
    public void WhenWithTextCalledThenReturnsNewInstanceWithNewText()
    {
        var original = new Todo("Buy groceries", isCompleted: true);

        var modified = original.WithText("Buy milk");

        Assert.Equal("Buy groceries", original.Text);
        Assert.Equal("Buy milk", modified.Text);
        Assert.True(modified.IsCompleted);
    }

    [Fact]
    public void WhenTwoTodosHaveSameContentThenAreEqual()
    {
        var todo1 = new Todo("Buy groceries", isCompleted: true);
        var todo2 = new Todo("Buy groceries", isCompleted: true);

        Assert.Equal(todo1, todo2);
    }

    [Fact]
    public void WhenTwoTodosHaveDifferentTextThenAreNotEqual()
    {
        var todo1 = new Todo("Buy groceries");
        var todo2 = new Todo("Buy milk");

        Assert.NotEqual(todo1, todo2);
    }

    [Fact]
    public void WhenTwoTodosHaveDifferentCompletedStateThenAreNotEqual()
    {
        var todo1 = new Todo("Buy groceries", isCompleted: false);
        var todo2 = new Todo("Buy groceries", isCompleted: true);

        Assert.NotEqual(todo1, todo2);
    }

    [Fact]
    public void WhenInlineTypeRequestedThenReturnsTodo()
    {
        var todo = new Todo("Buy groceries");

        Assert.Equal(InlineType.Todo, todo.Type);
    }

    [Fact]
    public void WhenPlainTextRequestedForIncompleteThenReturnsUncheckedFormat()
    {
        var todo = new Todo("Buy groceries", isCompleted: false);

        Assert.Equal("[ ] Buy groceries", todo.ToPlainText());
    }

    [Fact]
    public void WhenPlainTextRequestedForCompleteThenReturnsCheckedFormat()
    {
        var todo = new Todo("Buy groceries", isCompleted: true);

        Assert.Equal("[x] Buy groceries", todo.ToPlainText());
    }

    [Fact]
    public void WhenCreatedWithPriorityThenPriorityIsSet()
    {
        var todo = new Todo("Urgent task", isCompleted: false, priority: TodoPriority.High);

        Assert.Equal(TodoPriority.High, todo.Priority);
    }

    [Fact]
    public void WhenCreatedWithoutPriorityThenDefaultsToNormal()
    {
        var todo = new Todo("Normal task");

        Assert.Equal(TodoPriority.Normal, todo.Priority);
    }

    [Fact]
    public void WhenCreatedWithDueDateThenDueDateIsSet()
    {
        var dueDate = new DateOnly(2024, 12, 31);
        var todo = new Todo("Year end task", isCompleted: false, dueDate: dueDate);

        Assert.Equal(dueDate, todo.DueDate);
    }

    [Fact]
    public void WhenCreatedWithoutDueDateThenDueDateIsNull()
    {
        var todo = new Todo("No deadline task");

        Assert.Null(todo.DueDate);
    }
}
