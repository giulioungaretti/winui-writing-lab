# InkControl Library - Implementation Plan

> Historical plan for the retired Direct2D backend. The library now uses native Windows App SDK
> 2.4.1-experimental inking; see the repository README for the current architecture and limitations.

## Overview

Separate the `D2DDrawingSurfaceControl` into an independent WinUI class library (`InkControl`) that can be embedded in any WinUI application. The library will expose:

1. **Core Drawing Surface** - The main `D2DDrawingSurfaceControl` as an embeddable control
2. **Configurable Background** - Background settings exposed as properties or via a pre-made toolbar
3. **Ink Toolbar** - A default ink toolbar control, or APIs for building custom toolbars

## Architecture Principles

- **Decoupled from App Services**: No dependency on `App.Services` (DI container) - accept dependencies via constructor or properties
- **No Static Singletons in Public API**: The `DeviceManager.Instance` pattern should be internal; consumers shouldn't manage device lifecycle
- **Configurable Logging**: Accept `ILogger<T>` or `ILoggerFactory` optionally
- **Self-Contained Models**: Move or reference necessary models from `inkapp.Core` or define local equivalents

---

## Phase 1: Project Setup & Dependencies

### Step 1.1: Configure `InkControl.csproj`

Update the project file with proper WinUI library setup and dependencies:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0-windows10.0.19041.0</TargetFramework>
    <TargetPlatformMinVersion>10.0.17763.0</TargetPlatformMinVersion>
    <RootNamespace>InkControl</RootNamespace>
    <RuntimeIdentifiers>win-x86;win-x64;win-arm64</RuntimeIdentifiers>
    <UseWinUI>true</UseWinUI>
    <WinUISDKReferences>false</WinUISDKReferences>
    <Nullable>enable</Nullable>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Windows.SDK.BuildTools" Version="10.0.26100.7175" />
    <PackageReference Include="Microsoft.WindowsAppSDK" Version="1.8.251106002" />
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="8.0.0" />
    <PackageReference Include="Vortice.Direct2D1" Version="3.8.1" />
    <PackageReference Include="Vortice.Direct3D11" Version="3.8.1" />
    <PackageReference Include="Vortice.DXGI" Version="3.8.1" />
  </ItemGroup>
</Project>
```

### Step 1.2: Define Folder Structure

```
InkControl/
??? Controls/
?   ??? InkCanvas.xaml                    # Main drawing surface control
?   ??? InkCanvas.xaml.cs
?   ??? InkToolbar.xaml                   # Default pre-made toolbar
?   ??? InkToolbar.xaml.cs
?   ??? BackgroundToolbar.xaml            # Default background settings toolbar
?   ??? BackgroundToolbar.xaml.cs
??? Models/
?   ??? ToolMode.cs                       # Pen, Eraser, Pan enum
?   ??? BackgroundType.cs                 # Blank, Ruled, Dotted enum
?   ??? BackgroundSettings.cs             # Background configuration record
?   ??? InkSettings.cs                    # Pen color, thickness settings
?   ??? ViewportState.cs                  # Pan/zoom state (or reference inkapp.Core)
?   ??? Stroke.cs                         # (or reference inkapp.Core)
?   ??? StrokeCollection.cs               # (or reference inkapp.Core)
??? Rendering/
?   ??? DeviceManager.cs                  # D3D/D2D device lifecycle (internal)
?   ??? SwapChainManager.cs               # Swap chain management (internal)
?   ??? D2DRenderer.cs                    # Ink rendering (internal)
?   ??? BackgroundRenderer.cs             # Background patterns (internal)
??? Input/
?   ??? IViewportProvider.cs              # Interface for coordinate transforms
?   ??? InkInputProcessor.cs              # Platform-agnostic input processing
?   ??? InkInputHandler.cs                # WinUI pointer event handling
??? Themes/
?   ??? Generic.xaml                      # Default control styles
??? InkControl.csproj
```

---

## Phase 2: Models (Platform-Independent)

### Step 2.1: Create `Models/ToolMode.cs`

```csharp
namespace InkControl.Models;

/// <summary>
/// Available drawing tool modes.
/// </summary>
public enum ToolMode
{
    /// <summary>Pen drawing mode.</summary>
    Pen,
    /// <summary>Eraser mode (stroke eraser).</summary>
    Eraser,
    /// <summary>Pan/scroll mode.</summary>
    Pan
}
```

### Step 2.2: Create `Models/BackgroundType.cs`

```csharp
namespace InkControl.Models;

/// <summary>
/// Background pattern types for the canvas.
/// </summary>
public enum BackgroundType
{
    /// <summary>Blank/white background.</summary>
    Blank = 0,
    /// <summary>Horizontal ruled lines.</summary>
    Ruled = 1,
    /// <summary>Dotted grid pattern.</summary>
    Dotted = 2
}
```

### Step 2.3: Create `Models/BackgroundSettings.cs`

```csharp
namespace InkControl.Models;

/// <summary>
/// Configuration for canvas background appearance.
/// </summary>
public sealed record BackgroundSettings
{
    /// <summary>Pattern type (blank, ruled, dotted).</summary>
    public BackgroundType Type { get; init; } = BackgroundType.Blank;
    
    /// <summary>Spacing between lines or dots in pixels.</summary>
    public double Spacing { get; init; } = 24.0;
    
    /// <summary>Pattern color as ARGB integer.</summary>
    public int PatternColorArgb { get; init; } = unchecked((int)0x20000000); // subtle gray
    
    /// <summary>Background fill color as ARGB integer.</summary>
    public int BackgroundColorArgb { get; init; } = unchecked((int)0xFFFFFFFF); // white
    
    /// <summary>Default background settings (blank white).</summary>
    public static BackgroundSettings Default => new();
}
```

### Step 2.4: Create `Models/InkSettings.cs`

```csharp
namespace InkControl.Models;

/// <summary>
/// Configuration for ink/pen appearance.
/// </summary>
public sealed record InkSettings
{
    /// <summary>Pen color as Windows.UI.Color.</summary>
    public Windows.UI.Color PenColor { get; init; } = Microsoft.UI.Colors.Black;
    
    /// <summary>Pen stroke thickness in pixels.</summary>
    public double PenThickness { get; init; } = 2.0;
    
    /// <summary>Eraser radius in pixels.</summary>
    public double EraserRadius { get; init; } = 10.0;
    
    /// <summary>Default ink settings.</summary>
    public static InkSettings Default => new();
}
```

### Step 2.5: Decision - Reference `inkapp.Core` or Copy Models

**Option A (Recommended)**: Add reference to `inkapp.Core` for:
- `Stroke`, `StrokePoint`, `StrokeColor`, `StrokeCollection`
- `ViewportState`
- `IViewportProvider`, `IInkInputProcessor`, `InkInputProcessor`

**Option B**: Copy models into InkControl (more self-contained, but code duplication)

For this plan, we'll use **Option A** (reference `inkapp.Core`) to avoid duplication.

---

## Phase 3: Rendering Infrastructure (Internal)

### Step 3.1: Move/Copy `DeviceManager.cs`

- Copy from `desktop\Rendering\DeviceManager.cs`
- Change namespace to `InkControl.Rendering`
- Mark as `internal`
- Keep the singleton pattern (internal to library)

### Step 3.2: Move/Copy `SwapChainManager.cs`

- Copy from `desktop\Rendering\SwapChainManager.cs`
- Change namespace to `InkControl.Rendering`
- Mark as `internal`

### Step 3.3: Move/Copy `D2DInkRenderer.cs`

- Copy from `desktop\Rendering\D2DInkRenderer.cs`
- Change namespace to `InkControl.Rendering`
- Mark as `internal`
- **Remove dependency on `SettingsViewModel`** - accept settings via parameters/properties instead

### Step 3.4: Create `D2DBackgroundRenderer.cs`

- Create D2D-based background renderer (adapted from existing `BackgroundRenderer.cs`)
- Use Direct2D for drawing patterns (more efficient than XAML shapes)
- Mark as `internal`

---

## Phase 4: Input Handling

### Step 4.1: Copy `InkInputHandler.cs`

- Copy from `desktop\Input\InkInputHandler.cs`
- Change namespace to `InkControl.Input`
- **Remove dependency on `App.Services`** - accept `ILogger` via constructor (optional)
- Use `InkControl.Models.ToolMode` instead of `inkapp.Models.ToolMode`

### Step 4.2: Copy `ViewportStateAdapter.cs`

- Copy from `desktop\Input\ViewportStateAdapter.cs`  
- Change namespace to `InkControl.Input`

---

## Phase 5: Main Control - `InkCanvas`

### Step 5.1: Create `Controls/InkCanvas.xaml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<UserControl
    x:Class="InkControl.Controls.InkCanvas"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Grid x:Name="RootGrid" Background="Transparent">
        <!-- SwapChainPanel will be created dynamically -->
    </Grid>
</UserControl>
```

### Step 5.2: Create `Controls/InkCanvas.xaml.cs`

Refactor `D2DDrawingSurfaceControl` with these changes:

```csharp
namespace InkControl.Controls;

/// <summary>
/// High-performance ink drawing surface using Direct2D.
/// </summary>
public sealed partial class InkCanvas : UserControl, IDisposable
{
    // Constructor accepts optional ILoggerFactory (not required)
    public InkCanvas() : this(null) { }
    
    public InkCanvas(ILoggerFactory? loggerFactory)
    {
        _logger = loggerFactory?.CreateLogger<InkCanvas>();
        // ... initialization
    }
    
    #region Dependency Properties
    
    // Existing properties:
    // - PenColor, PenThickness, ToolMode, EraserRadius
    // - BgType, BgSpacing, BgColorArgb
    // - PanX, PanY, Zoom
    
    // NEW: BackgroundSettings as a single property (alternative to individual props)
    public static readonly DependencyProperty BackgroundSettingsProperty = ...;
    
    // NEW: InkSettings as a single property (alternative to individual props)
    public static readonly DependencyProperty InkSettingsProperty = ...;
    
    #endregion
    
    #region Events
    
    /// <summary>Raised when strokes change (added/removed).</summary>
    public event EventHandler? StrokesChanged;
    
    /// <summary>Raised when viewport (pan/zoom) changes.</summary>
    public event EventHandler<ViewportChangedEventArgs>? ViewportChanged;
    
    /// <summary>Raised when a stroke is completed.</summary>
    public event EventHandler<StrokeCompletedEventArgs>? StrokeCompleted;
    
    #endregion
    
    #region Public Methods
    
    /// <summary>Gets the current stroke collection.</summary>
    public StrokeCollection GetStrokes();
    
    /// <summary>Sets the stroke collection.</summary>
    public void SetStrokes(StrokeCollection strokes);
    
    /// <summary>Clears all strokes.</summary>
    public void Clear();
    
    /// <summary>Exports strokes to byte array for persistence.</summary>
    public Task<byte[]> ExportAsync();
    
    /// <summary>Imports strokes from byte array.</summary>
    public Task ImportAsync(byte[] data);
    
    /// <summary>Resets viewport to default (no pan, 100% zoom).</summary>
    public void ResetViewport();
    
    /// <summary>Zooms to fit all content.</summary>
    public void ZoomToFit();
    
    #endregion
}
```

### Step 5.3: Event Args Classes

```csharp
namespace InkControl.Controls;

public sealed class ViewportChangedEventArgs : EventArgs
{
    public float PanX { get; init; }
    public float PanY { get; init; }
    public float Zoom { get; init; }
}

public sealed class StrokeCompletedEventArgs : EventArgs
{
    public required Stroke Stroke { get; init; }
}
```

---

## Phase 6: Pre-Made Toolbar Controls

### Step 6.1: Create `Controls/InkToolbar.xaml`

Default toolbar with pen settings:

```xml
<UserControl x:Class="InkControl.Controls.InkToolbar">
    <StackPanel Orientation="Horizontal" Spacing="4">
        <!-- Tool Selection -->
        <RadioButtons x:Name="ToolSelector">
            <RadioButton Content="??" Tag="Pen" />
            <RadioButton Content="??" Tag="Eraser" />
            <RadioButton Content="?" Tag="Pan" />
        </RadioButtons>
        
        <!-- Pen Color Button -->
        <Button x:Name="ColorButton">
            <Border Width="24" Height="24" CornerRadius="12" 
                    Background="{x:Bind PenColor, Mode=OneWay, Converter=...}" />
            <Button.Flyout>
                <Flyout>
                    <ColorPicker x:Name="ColorPicker" />
                </Flyout>
            </Button.Flyout>
        </Button>
        
        <!-- Thickness Slider -->
        <Slider x:Name="ThicknessSlider" 
                Minimum="1" Maximum="20" Value="{x:Bind PenThickness, Mode=TwoWay}" />
    </StackPanel>
</UserControl>
```

### Step 6.2: Create `Controls/InkToolbar.xaml.cs`

```csharp
namespace InkControl.Controls;

/// <summary>
/// Pre-made toolbar for ink settings. Bind to an InkCanvas via TargetCanvas property.
/// </summary>
public sealed partial class InkToolbar : UserControl
{
    /// <summary>The target InkCanvas this toolbar controls.</summary>
    public static readonly DependencyProperty TargetCanvasProperty = ...;
    
    public InkCanvas? TargetCanvas
    {
        get => (InkCanvas?)GetValue(TargetCanvasProperty);
        set => SetValue(TargetCanvasProperty, value);
    }
    
    // Toolbar syncs its state with TargetCanvas properties
}
```

### Step 6.3: Create `Controls/BackgroundToolbar.xaml`

```xml
<UserControl x:Class="InkControl.Controls.BackgroundToolbar">
    <StackPanel Orientation="Horizontal" Spacing="4">
        <!-- Background Type -->
        <ComboBox x:Name="BackgroundTypeSelector">
            <ComboBoxItem Content="Blank" Tag="Blank" />
            <ComboBoxItem Content="Ruled" Tag="Ruled" />
            <ComboBoxItem Content="Dotted" Tag="Dotted" />
        </ComboBox>
        
        <!-- Spacing Slider -->
        <Slider x:Name="SpacingSlider" 
                Minimum="8" Maximum="48" Value="{x:Bind Spacing, Mode=TwoWay}" />
        
        <!-- Pattern Color -->
        <Button x:Name="PatternColorButton">
            <Border Width="24" Height="24" Background="..." />
        </Button>
    </StackPanel>
</UserControl>
```

### Step 6.4: Create `Controls/BackgroundToolbar.xaml.cs`

```csharp
namespace InkControl.Controls;

/// <summary>
/// Pre-made toolbar for background settings. Bind to an InkCanvas via TargetCanvas property.
/// </summary>
public sealed partial class BackgroundToolbar : UserControl
{
    public static readonly DependencyProperty TargetCanvasProperty = ...;
    
    public InkCanvas? TargetCanvas { get; set; }
}
```

---

## Phase 7: Integration with Host Application

### Step 7.1: Consumer Usage - Basic

```xml
<Page xmlns:ink="using:InkControl.Controls">
    <Grid>
        <ink:InkCanvas x:Name="Canvas" 
                       PenColor="Blue" 
                       PenThickness="3" />
    </Grid>
</Page>
```

### Step 7.2: Consumer Usage - With Toolbars

```xml
<Page xmlns:ink="using:InkControl.Controls">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="*" />
        </Grid.RowDefinitions>
        
        <StackPanel Grid.Row="0" Orientation="Horizontal">
            <ink:InkToolbar TargetCanvas="{x:Bind Canvas}" />
            <ink:BackgroundToolbar TargetCanvas="{x:Bind Canvas}" />
        </StackPanel>
        
        <ink:InkCanvas x:Name="Canvas" Grid.Row="1" />
    </Grid>
</Page>
```

### Step 7.3: Consumer Usage - Custom Toolbar

```xml
<Page>
    <Grid>
        <!-- Custom toolbar binding directly to InkCanvas properties -->
        <StackPanel>
            <ColorPicker Color="{x:Bind Canvas.PenColor, Mode=TwoWay}" />
            <Slider Value="{x:Bind Canvas.PenThickness, Mode=TwoWay}" />
        </StackPanel>
        
        <ink:InkCanvas x:Name="Canvas" />
    </Grid>
</Page>
```

---

## Phase 8: Update Host Application (`inkapp`)

### Step 8.1: Add Project Reference

```xml
<!-- desktop\inkapp.csproj -->
<ItemGroup>
    <ProjectReference Include="..\InkControl\InkControl.csproj" />
</ItemGroup>
```

### Step 8.2: Update `NotebookPage.xaml`

Replace `controls:D2DDrawingSurfaceControl` with `ink:InkCanvas`:

```xml
<Page xmlns:ink="using:InkControl.Controls">
    <!-- ... -->
    <ink:InkCanvas x:Name="DrawingSurface"
                   PenColor="{x:Bind ViewModel.PenColor, Mode=TwoWay}"
                   PenThickness="{x:Bind ViewModel.PenThickness, Mode=TwoWay}"
                   ToolMode="{x:Bind ViewModel.CurrentTool, Mode=TwoWay}"
                   BgType="{x:Bind ViewModel.CurrentPage.BackgroundType, Mode=OneWay}"
                   StrokesChanged="DrawingSurface_StrokesChanged" />
</Page>
```

### Step 8.3: Remove Duplicated Code from `desktop`

Delete these files from `desktop` project after migration:
- `desktop\Controls\D2DDrawingSurfaceControl.xaml(.cs)`
- `desktop\Rendering\DeviceManager.cs`
- `desktop\Rendering\SwapChainManager.cs`
- `desktop\Rendering\D2DInkRenderer.cs`
- `desktop\Rendering\BackgroundRenderer.cs`
- `desktop\Input\InkInputHandler.cs`
- `desktop\Input\ViewportStateAdapter.cs`
- `desktop\Models\ToolMode.cs`

---

## Phase 9: Testing & Validation

### Step 9.1: Create Test Project

```
InkControl.Tests/
??? Controls/
?   ??? InkCanvasTests.cs
??? Models/
?   ??? BackgroundSettingsTests.cs
??? InkControl.Tests.csproj
```

### Step 9.2: Test Cases

1. **Initialization**: InkCanvas creates without errors
2. **Property Binding**: PenColor, PenThickness update rendering
3. **Background Settings**: Different background types render correctly
4. **Stroke Management**: GetStrokes/SetStrokes round-trip correctly
5. **Export/Import**: Serialization works correctly
6. **Toolbar Binding**: InkToolbar syncs with target InkCanvas

---

## Implementation Order (Recommended Sequence)

1. ? Create this plan document
2. Update `InkControl.csproj` with dependencies
3. Create `Models/` folder with enums and settings records
4. Copy rendering infrastructure to `Rendering/`
5. Copy input handling to `Input/`
6. Create `InkCanvas` control
7. Create `InkToolbar` control  
8. Create `BackgroundToolbar` control
9. Add `Themes/Generic.xaml` for styles
10. Update `inkapp` to use `InkControl` library
11. Remove duplicated code from `inkapp`
12. Add tests

---

## API Surface Summary

### Public Types

| Type | Description |
|------|-------------|
| `InkControl.Controls.InkCanvas` | Main drawing surface control |
| `InkControl.Controls.InkToolbar` | Pre-made ink settings toolbar |
| `InkControl.Controls.BackgroundToolbar` | Pre-made background settings toolbar |
| `InkControl.Models.ToolMode` | Pen/Eraser/Pan enum |
| `InkControl.Models.BackgroundType` | Blank/Ruled/Dotted enum |
| `InkControl.Models.BackgroundSettings` | Background configuration |
| `InkControl.Models.InkSettings` | Ink configuration |

### InkCanvas Dependency Properties

| Property | Type | Description |
|----------|------|-------------|
| `PenColor` | `Windows.UI.Color` | Current pen color |
| `PenThickness` | `double` | Pen stroke thickness |
| `ToolMode` | `ToolMode` | Current tool mode |
| `EraserRadius` | `double` | Eraser touch radius |
| `BgType` | `BackgroundType` | Background pattern type |
| `BgSpacing` | `double` | Pattern spacing |
| `BgColorArgb` | `int` | Pattern color |
| `PanX`, `PanY` | `double` | Viewport pan offset |
| `Zoom` | `double` | Viewport zoom level |

### InkCanvas Events

| Event | Description |
|-------|-------------|
| `StrokesChanged` | Strokes added or removed |
| `StrokeCompleted` | Single stroke finished |
| `ViewportChanged` | Pan or zoom changed |

---

## Notes & Considerations

1. **Threading**: All D2D rendering must happen on UI thread
2. **Device Loss**: Handle D3D device lost/restored scenarios
3. **Memory**: Large stroke collections should be virtualized
4. **Accessibility**: Consider narrator support for toolbar controls
5. **Localization**: Toolbar strings should be localizable
6. **HiDPI**: Ensure proper DPI handling for retina displays
