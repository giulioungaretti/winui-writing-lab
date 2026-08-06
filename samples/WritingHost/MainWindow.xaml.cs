using InkControl.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WritingHost;

/// <summary>
/// Combined host: block-based rich text editor (formatting + inline todos/tags) on the left,
/// low-latency D2D ink canvas on the right.
/// </summary>
public sealed partial class MainWindow : Window
{
    private const string DemoDocument =
        "This editor supports **bold**, *italic*, and ~~strikethrough~~ text.\n" +
        "Inline elements render as real controls: #demo #winui\n" +
        "[ ] An open todo item\n" +
        "[x] A completed todo item\n" +
        "Mix them freely: **important** [ ] review the ink latency notes #research\n" +
        "Images embed inline too: ![lab sketch](Assets/demo-sketch.png) right in the text flow.";

    public MainWindow()
    {
        InitializeComponent();
    }

    // ---- Editor ----

    private void DocumentEditor_ContentChanged(object? sender, string content)
    {
        StatusText.Text = $"Document: {content.Length} chars";
    }

    private void DocumentEditor_TagClicked(object? sender, string tag)
    {
        StatusText.Text = $"Tag clicked: #{tag}";
    }

    private void DocumentEditor_TodoToggled(object? sender, (string Text, bool IsCompleted) args)
    {
        StatusText.Text = $"Todo {(args.IsCompleted ? "completed" : "reopened")}: {args.Text}";
    }

    private void InsertTodo_Click(object sender, RoutedEventArgs e)
    {
        AppendLine("[ ] New todo");
    }

    private void InsertTag_Click(object sender, RoutedEventArgs e)
    {
        AppendLine("#tag");
    }

    private void LoadDemo_Click(object sender, RoutedEventArgs e)
    {
        DocumentEditor.SetDocumentText(DemoDocument);
        StatusText.Text = "Demo document loaded";
    }

    private void ClearDocument_Click(object sender, RoutedEventArgs e)
    {
        DocumentEditor.Clear();
        StatusText.Text = "Document cleared";
    }

    private void AppendLine(string line)
    {
        var text = DocumentEditor.GetDocumentText();
        DocumentEditor.SetDocumentText(string.IsNullOrWhiteSpace(text) ? line : $"{text}\n{line}");
        DocumentEditor.FocusLastBlock();
        StatusText.Text = $"Document: {DocumentEditor.GetDocumentText().Length} chars";
    }

    // ---- Ink ----

    private void BackgroundComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (InkSurface is null) return;

        InkSurface.BgType = BackgroundComboBox.SelectedIndex switch
        {
            1 => BackgroundType.Ruled,
            2 => BackgroundType.Dotted,
            _ => BackgroundType.Blank
        };
    }

    private void ResetViewport_Click(object sender, RoutedEventArgs e)
    {
        InkSurface.ResetViewport();
        ZoomText.Text = "100%";
    }

    private void ClearInk_Click(object sender, RoutedEventArgs e)
    {
        InkSurface.Clear();
        StatusText.Text = "Ink cleared";
    }

    private void InkSurface_ViewportChanged(object sender, InkControl.Controls.ViewportChangedEventArgs e)
    {
        ZoomText.Text = $"{e.Zoom * 100:F0}%";
    }
}
