# Low-Latency Win2D Inking Implementation - Session Notes
**Date:** 2025-01-28
**Project:** win2dlowlatecny

## Objective
Implement a low-latency (sub-20ms) Win2D-based inking solution with pen-only input on an infinite canvas with pan and zoom support.

## Key Technical Decisions

### 1. Rendering Approach - CanvasSwapChainPanel
- **Why:** Swap chains are not tied to XAML's refresh timer, enabling lower latency
- **Alternative considered:** CanvasControl - rejected because it's tied to XAML refresh (~16ms minimum)
- **Package:** `Microsoft.Graphics.Win2D` version 1.3.2

### 2. Dedicated Render Thread with Event-Based Signaling
```csharp
private readonly AutoResetEvent _renderEvent = new(false);

private void RequestRedraw()
{
    _renderEvent.Set(); // Immediate signal, no polling delay
}

private void RenderLoop()
{
    while (_isRunning)
    {
        bool signaled = _renderEvent.WaitOne(16); // 60fps minimum
        if (_isSwapChainReady && (signaled || _activeStroke != null))
        {
            RenderFrame();
        }
    }
}
```
- **Why:** `AutoResetEvent` provides immediate wake-up vs `Thread.Sleep(1)` which has ~1-15ms resolution
- **Thread Priority:** `ThreadPriority.Highest` for render thread

### 3. Input Handling - GetIntermediatePoints
```csharp
var points = e.GetIntermediatePoints(_swapChainPanel);
foreach (var point in points)
{
    if (point.IsInContact)
    {
        AddPointToActiveStroke(point, inputTimestamp);
    }
}
```
- **Why:** Captures all pen samples that may have been batched by the input system
- **Result:** Smoother strokes, no lost points

### 4. Pen-Only Filtering
```csharp
if (pointer.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Pen)
{
    // Drawing logic
}
else if (pointer.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Touch)
{
    // Pan/zoom gestures
}
```

## Architecture

### File Structure
```
win2dlowlatecny/
??? Models/
?   ??? InkPoint.cs      # Struct with position, pressure, timestamp
?   ??? InkStroke.cs     # Stroke with bounding box for culling
?   ??? StrokeCollection.cs  # Thread-safe collection
??? Rendering/
?   ??? CanvasTransform.cs   # Pan/zoom matrix management
?   ??? InkRenderer.cs       # Win2D drawing logic
??? Diagnostics/
?   ??? LatencyTracker.cs    # Real-time latency measurement
??? MainWindow.xaml          # CanvasSwapChainPanel
??? MainWindow.xaml.cs       # Input + render loop
```

### Key Classes

#### InkPoint (struct)
- `Vector2 Position` - Canvas coordinates
- `float Pressure` - 0.0 to 1.0
- `long TimestampTicks` - Stopwatch ticks for latency tracking

#### InkStroke
- Pre-allocated `List<InkPoint>(256)` for performance
- Bounding box for viewport culling
- Pressure-based thickness calculation

#### CanvasTransform
- Matrix3x2 for pan/zoom
- `ScreenToCanvas()` / `CanvasToScreen()` coordinate conversion
- `ZoomAt(screenCenter, scaleFactor)` - zoom around cursor

#### LatencyTracker
- Uses `Stopwatch` for high-resolution timing
- Rolling average of last 100 samples
- Formatted output for overlay display

## WinUI 3 / Win2D Gotchas Discovered

### 1. Namespace Conflicts
```csharp
// WRONG - ambiguous
if (pointer.PointerDeviceType == PointerDeviceType.Pen)

// CORRECT - fully qualified
if (pointer.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Pen)
```

### 2. Colors Namespace
```csharp
// WRONG in WinUI 3
Colors.White

// CORRECT
Microsoft.UI.Colors.White
```

### 3. XAML x:Name Not Generating Fields
- Store reference in code-behind: `_swapChainPanel = sender as CanvasSwapChainPanel;`

### 4. Build Platform Requirement
```bash
# WRONG - AnyCPU not supported
dotnet build

# CORRECT
dotnet build -p:Platform=x64
```

### 5. SwapChain Size Properties Return Double
```csharp
float width = (float)_swapChain.Size.Width;
float height = (float)_swapChain.Size.Height;
```

## Performance Optimizations Applied

1. **Pre-allocated collections** - `List<InkPoint>(256)` per stroke
2. **Viewport culling** - Only render visible strokes using bounding boxes
3. **Minimal lock contention** - Quick lock scope in render frame
4. **Event-based signaling** - No polling delays
5. **High-priority render thread**
6. **Immediate redraw on input** - `RequestRedraw()` after each point added

## Latency Measurement Strategy

```csharp
// Capture timestamp at input event
long inputTimestamp = _latencyTracker.GetTimestamp();

// Record when point is processed
_latencyTracker.RecordLatency(inputTimestamp);

// Display in overlay
string stats = _latencyTracker.GetStatisticsString();
// Output: "Latency: 8.23ms (avg: 7.45ms, min: 5.12ms, max: 12.34ms)"
```

## Features Implemented

- ? Pen-only inking with pressure sensitivity
- ? Infinite canvas with pan (single touch)
- ? Zoom (pinch or mouse wheel)
- ? Real-time latency overlay
- ? Viewport culling for completed strokes
- ? Undo (Ctrl+Z)
- ? Reset view (Ctrl+R)
- ? Grid visualization with adaptive density
- ? Device lost recovery

## Useful Documentation Links

- Win2D without built-in controls: https://learn.microsoft.com/en-us/windows/apps/develop/win2d/using-win2d-without-built-in-controls
- CanvasSwapChainPanel: Lower latency than CanvasControl
- GetIntermediatePoints: Captures batched input samples
- Custom ink rendering: https://learn.microsoft.com/en-us/windows/apps/develop/input/pen-and-stylus-interactions#custom-ink-rendering

## Future Improvements to Consider

1. **Bezier smoothing** - Use CanvasPathBuilder for smoother curves
2. **Ink prediction** - Extrapolate next point for perceived lower latency
3. **Stroke caching** - Rasterize completed strokes to texture
4. **Multi-layer support** - Separate canvases for different content types
5. **Eraser tool** - Hit testing against stroke bounds
