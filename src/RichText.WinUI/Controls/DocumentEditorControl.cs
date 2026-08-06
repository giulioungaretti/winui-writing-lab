using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace RichTexteEditor.Controls;

/// <summary>
/// A document editor control that manages multiple BlockEditor instances.
/// Provides a Notion-like editing experience with inline components.
/// </summary>
public sealed class DocumentEditorControl : UserControl
{
    private readonly ScrollViewer _scrollViewer;
    private readonly StackPanel _blocksPanel;
    private readonly ObservableCollection<BlockEditor> _blocks;

    public static readonly DependencyProperty PlaceholderTextProperty =
        DependencyProperty.Register(
            nameof(PlaceholderText),
            typeof(string),
            typeof(DocumentEditorControl),
            new PropertyMetadata("Start typing...\n\nUse #tag to create inline tags\nUse [ ] to create todos"));

    /// <summary>
    /// Gets or sets the placeholder text for empty documents.
    /// </summary>
    public string PlaceholderText
    {
        get => (string)GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    /// <summary>
    /// Raised when any text content changes.
    /// </summary>
    public event EventHandler<string>? ContentChanged;

    /// <summary>
    /// Raised when a tag is clicked.
    /// </summary>
    public event EventHandler<string>? TagClicked;

    /// <summary>
    /// Raised when a todo is toggled.
    /// </summary>
    public event EventHandler<(string Text, bool IsCompleted)>? TodoToggled;

    public DocumentEditorControl()
    {
        _blocks = new ObservableCollection<BlockEditor>();

        _blocksPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Padding = new Thickness(16),
            Spacing = 2
        };

        _scrollViewer = new ScrollViewer
        {
            VerticalScrollMode = ScrollMode.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollMode = ScrollMode.Disabled,
            Content = _blocksPanel
        };

        Content = _scrollViewer;

        // Start with one empty block
        AddNewBlock(0);
    }

    /// <summary>
    /// Gets the full document text (all blocks combined with newlines).
    /// </summary>
    public string GetDocumentText()
    {
        return string.Join("\n", _blocks.Select(b => b.Text));
    }

    /// <summary>
    /// Sets the document content from text (splits into blocks by newlines).
    /// </summary>
    public void SetDocumentText(string text)
    {
        ClearAllBlocks();

        if (string.IsNullOrEmpty(text))
        {
            AddNewBlock(0);
            return;
        }

        var lines = text.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var block = CreateBlock(lines[i]);
            _blocks.Add(block);
            _blocksPanel.Children.Add(block);
        }

        // Ensure at least one block
        if (_blocks.Count == 0)
        {
            AddNewBlock(0);
        }
    }

    /// <summary>
    /// Clears all content.
    /// </summary>
    public void Clear()
    {
        ClearAllBlocks();
        AddNewBlock(0);
    }

    private void ClearAllBlocks()
    {
        foreach (var block in _blocks)
        {
            UnwireBlockEvents(block);
        }
        _blocks.Clear();
        _blocksPanel.Children.Clear();
    }

    private BlockEditor CreateBlock(string text = "")
    {
        var block = new BlockEditor
        {
            Text = text,
            PlaceholderText = _blocks.Count == 0 ? PlaceholderText : "Type / for commands..."
        };

        WireBlockEvents(block);
        return block;
    }

    private void WireBlockEvents(BlockEditor block)
    {
        block.TextChanged += OnBlockTextChanged;
        block.EnterPressed += OnBlockEnterPressed;
        block.TagClicked += OnBlockTagClicked;
        block.TodoToggled += OnBlockTodoToggled;
    }

    private void UnwireBlockEvents(BlockEditor block)
    {
        block.TextChanged -= OnBlockTextChanged;
        block.EnterPressed -= OnBlockEnterPressed;
        block.TagClicked -= OnBlockTagClicked;
        block.TodoToggled -= OnBlockTodoToggled;
    }

    private void OnBlockTextChanged(object? sender, string text)
    {
        ContentChanged?.Invoke(this, GetDocumentText());
    }

    private void OnBlockEnterPressed(object? sender, EventArgs e)
    {
        if (sender is BlockEditor currentBlock)
        {
            var currentIndex = _blocks.IndexOf(currentBlock);
            if (currentIndex >= 0)
            {
                var newBlock = AddNewBlock(currentIndex + 1);
                newBlock.FocusEditor();
            }
        }
    }

    private void OnBlockTagClicked(object? sender, string tag)
    {
        TagClicked?.Invoke(this, tag);
    }

    private void OnBlockTodoToggled(object? sender, (string Text, bool IsCompleted) args)
    {
        TodoToggled?.Invoke(this, args);
        ContentChanged?.Invoke(this, GetDocumentText());
    }

    private BlockEditor AddNewBlock(int index)
    {
        var block = CreateBlock();
        
        if (index >= _blocks.Count)
        {
            _blocks.Add(block);
            _blocksPanel.Children.Add(block);
        }
        else
        {
            _blocks.Insert(index, block);
            _blocksPanel.Children.Insert(index, block);
        }

        // Update placeholder for first block
        if (_blocks.Count > 0 && _blocks[0] != null)
        {
            _blocks[0].PlaceholderText = PlaceholderText;
        }

        return block;
    }

    /// <summary>
    /// Removes empty blocks (except the first one).
    /// </summary>
    public void TrimEmptyBlocks()
    {
        for (var i = _blocks.Count - 1; i > 0; i--)
        {
            if (string.IsNullOrEmpty(_blocks[i].Text))
            {
                UnwireBlockEvents(_blocks[i]);
                _blocksPanel.Children.RemoveAt(i);
                _blocks.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Focuses the first block for editing.
    /// </summary>
    public void FocusFirstBlock()
    {
        if (_blocks.Count > 0)
        {
            _blocks[0].FocusEditor();
        }
    }

    /// <summary>
    /// Focuses the last block for editing.
    /// </summary>
    public void FocusLastBlock()
    {
        if (_blocks.Count > 0)
        {
            var lastBlock = _blocks[^1];
            lastBlock.FocusEditor();
            lastBlock.MoveCursorToEnd();
        }
    }
}
