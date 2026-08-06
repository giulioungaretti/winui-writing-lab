using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using RichTexteEditor.Core.Parsing;
using Windows.System;

namespace RichTexteEditor;

/// <summary>
/// Main application window containing the rich text editor with inline component rendering.
/// Uses a block-based approach with RichTextBlock + InlineUIContainer for true inline components.
/// </summary>
public sealed partial class MainWindow : Window
{
    private readonly DocumentParser _parser;
    private readonly InlineParser _inlineParser;

    public MainWindow()
    {
        InitializeComponent();

        _parser = new DocumentParser();
        _inlineParser = new InlineParser();

        StatusText.Text = "Ready";
        UpdateStats();
    }

    private void DocumentEditor_ContentChanged(object? sender, string content)
    {
        UpdateStats();
    }

    private void DocumentEditor_TagClicked(object? sender, string tag)
    {
        StatusText.Text = $"Tag clicked: #{tag}";
    }

    private void DocumentEditor_TodoToggled(object? sender, (string Text, bool IsCompleted) args)
    {
        var status = args.IsCompleted ? "completed" : "uncompleted";
        StatusText.Text = $"Todo {status}: {args.Text}";
        UpdateStats();
    }

    private void InsertTag_Click(object sender, RoutedEventArgs e)
    {
        // Add a new block with a tag prefix
        QuickInput.Text = "#";
        QuickInput.Focus(FocusState.Programmatic);
        QuickInput.SelectionStart = QuickInput.Text.Length;
        StatusText.Text = "Type tag name in quick input";
    }

    private void InsertTodo_Click(object sender, RoutedEventArgs e)
    {
        // Add a new block with a todo prefix
        QuickInput.Text = "[ ] ";
        QuickInput.Focus(FocusState.Programmatic);
        QuickInput.SelectionStart = QuickInput.Text.Length;
        StatusText.Text = "Type todo text in quick input";
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        DocumentEditor.Clear();
        StatusText.Text = "Cleared";
        UpdateStats();
    }

    private void QuickInput_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
        {
            ProcessQuickInput();
            e.Handled = true;
        }
    }

    private void QuickAdd_Click(object sender, RoutedEventArgs e)
    {
        ProcessQuickInput();
    }

    private void ProcessQuickInput()
    {
        var input = QuickInput.Text?.Trim();
        if (string.IsNullOrEmpty(input)) return;

        // Get current content and append
        var currentContent = DocumentEditor.GetDocumentText();
        var newContent = string.IsNullOrEmpty(currentContent)
            ? input
            : currentContent + "\n" + input;

        DocumentEditor.SetDocumentText(newContent);

        // Clear input
        QuickInput.Text = string.Empty;

        // Count what was added
        var inlines = _inlineParser.Parse(input);
        var tagCount = inlines.Count(i => i.Type == Core.Model.InlineType.Tag);
        var todoCount = inlines.Count(i => i.Type == Core.Model.InlineType.Todo);

        if (tagCount > 0 || todoCount > 0)
        {
            var parts = new System.Collections.Generic.List<string>();
            if (tagCount > 0) parts.Add($"{tagCount} tag{(tagCount == 1 ? "" : "s")}");
            if (todoCount > 0) parts.Add($"{todoCount} todo{(todoCount == 1 ? "" : "s")}");
            StatusText.Text = $"Added: {string.Join(", ", parts)}";
        }
        else
        {
            StatusText.Text = "Added text";
        }

        UpdateStats();
        DocumentEditor.FocusLastBlock();
    }

    private void UpdateStats()
    {
        var text = DocumentEditor.GetDocumentText();

        if (string.IsNullOrEmpty(text))
        {
            StatsText.Text = "";
            TodoSummaryText.Text = "? 0/0";
            TagSummaryText.Text = "#0 tags";
            return;
        }

        var document = _parser.Parse(text);
        var tags = document.FindAllTags();
        var todos = document.FindAllTodos();

        var uniqueTagCount = tags.Select(t => t.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count();
        var todoCount = todos.Count;
        var completedCount = todos.Count(t => t.IsCompleted);

        var charCount = text.Length;
        var wordCount = text.Split(new[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;

        // Update summary in title bar
        TodoSummaryText.Text = $"? {completedCount}/{todoCount}";
        TagSummaryText.Text = $"#{uniqueTagCount} tag{(uniqueTagCount == 1 ? "" : "s")}";

        // Update status bar stats
        var parts = new System.Collections.Generic.List<string>
        {
            $"{charCount} chars",
            $"{wordCount} words"
        };

        StatsText.Text = string.Join(" | ", parts);
    }
}
