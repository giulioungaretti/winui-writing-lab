using Microsoft.UI.Xaml;

namespace WritingHost;

/// <summary>
/// Combined writing lab host: rich text editing with inline elements next to low-latency inking.
/// </summary>
public partial class App : Application
{
    private Window? _window;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }
}
