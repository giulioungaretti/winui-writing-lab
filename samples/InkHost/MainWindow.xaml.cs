using InkControl.Controls;
using InkControl.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace App1;

/// <summary>
/// Demo window showcasing the InkControl library.
/// </summary>
public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void BackgroundComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (InkCanvas is null) return;

        InkCanvas.BgType = BackgroundComboBox.SelectedIndex switch
        {
            0 => BackgroundType.Blank,
            1 => BackgroundType.Ruled,
            2 => BackgroundType.Dotted,
            _ => BackgroundType.Blank
        };
    }

    private void ClearCanvas_Click(object sender, RoutedEventArgs e)
    {
        InkCanvas?.Clear();
        StatusText.Text = "Canvas cleared";
    }

    private void ResetViewport_Click(object sender, RoutedEventArgs e)
    {
        InkCanvas?.ResetViewport();
        ZoomText.Text = "100%";
        StatusText.Text = "Viewport reset";
    }

    private void InkCanvas_ViewportChanged(object sender, ViewportChangedEventArgs e)
    {
        ZoomText.Text = $"{e.Zoom * 100:F0}%";
        StatusText.Text = $"Pan: {e.PanX:F0}, {e.PanY:F0} DIPs";
    }

    private void InkCanvas_StrokesChanged(object? sender, EventArgs e)
    {
        if (StatusText is not null)
            StatusText.Text = $"Ink: {InkCanvas.GetStrokes().Strokes.Count} strokes";
    }
}
