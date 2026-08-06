using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Threading;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Windows.UI;
using win2dlowlatecny.Diagnostics;
using win2dlowlatecny.Models;
using win2dlowlatecny.Rendering;

namespace win2dlowlatecny;

/// <summary>
/// Low-latency inking window with infinite canvas support.
/// Target: sub-20ms input-to-display latency.
/// </summary>
public sealed partial class MainWindow : Window
{
    // Win2D rendering
    private CanvasDevice? _canvasDevice;
    private CanvasSwapChain? _swapChain;
    private bool _isSwapChainReady;

    // Ink data
    private readonly StrokeCollection _strokes;
    private InkStroke? _activeStroke;

    // Transform and rendering
    private readonly CanvasTransform _transform;
    private readonly InkRenderer _renderer;
    private readonly LatencyTracker _latencyTracker;

    // Touch gesture tracking
    private readonly Dictionary<uint, Vector2> _touchPoints = new();
    private float _lastPinchDistance;
    private Vector2 _lastPinchCenter;
    private bool _isPinching;

    // Rendering thread with event-based signaling for low latency
    private Thread? _renderThread;
    private volatile bool _isRunning;
    private readonly object _renderLock = new();
    private readonly AutoResetEvent _renderEvent = new(false);

    // Stroke settings
    private readonly Color _inkColor = Microsoft.UI.Colors.DarkBlue;
    private const float BaseStrokeThickness = 4.0f;

    // Reference to swap chain panel
    private CanvasSwapChainPanel? _swapChainPanel;

    public MainWindow()
    {
        InitializeComponent();

        _latencyTracker = new LatencyTracker();
        _transform = new CanvasTransform();
        _renderer = new InkRenderer(_latencyTracker);
        _strokes = new StrokeCollection();

        // Wire up keyboard shortcuts
        Content.KeyDown += Content_KeyDown;

        Debug.WriteLine("[MainWindow] Initialized - Target latency: <20ms");
    }

    private void SwapChainPanel_Loaded(object sender, RoutedEventArgs e)
    {
        Debug.WriteLine("[MainWindow] SwapChainPanel loaded");

        _swapChainPanel = sender as CanvasSwapChainPanel;
        if (_swapChainPanel == null)
        {
            Debug.WriteLine("[MainWindow] ERROR: SwapChainPanel is null");
            return;
        }

        // Create the canvas device
        _canvasDevice = CanvasDevice.GetSharedDevice();
        _canvasDevice.DeviceLost += CanvasDevice_DeviceLost;

        // Initialize renderer
        _renderer.Initialize(_canvasDevice);

        // Create swap chain
        CreateSwapChain();

        // Setup input handlers
        SetupInputHandlers();

        // Start render thread
        StartRenderLoop();
    }

    private void SwapChainPanel_Unloaded(object sender, RoutedEventArgs e)
    {
        Debug.WriteLine("[MainWindow] SwapChainPanel unloaded");

        StopRenderLoop();

        _renderer.Dispose();

        if (_canvasDevice != null)
        {
            _canvasDevice.DeviceLost -= CanvasDevice_DeviceLost;
            _canvasDevice.Dispose();
            _canvasDevice = null;
        }

        _swapChain?.Dispose();
        _swapChain = null;
        _isSwapChainReady = false;
        _swapChainPanel = null;
    }

    private void SwapChainPanel_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_swapChain == null || _canvasDevice == null)
            return;

        lock (_renderLock)
        {
            try
            {
                var newSize = e.NewSize;
                if (newSize.Width > 0 && newSize.Height > 0)
                {
                    _swapChain.ResizeBuffers((float)newSize.Width, (float)newSize.Height);
                    RequestRedraw();
                    Debug.WriteLine($"[MainWindow] Resized swap chain to {newSize.Width}x{newSize.Height}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MainWindow] Error resizing swap chain: {ex.Message}");
            }
        }
    }

    private void CreateSwapChain()
    {
        if (_canvasDevice == null || _swapChainPanel == null)
            return;

        lock (_renderLock)
        {
            _swapChain?.Dispose();

            float width = Math.Max((float)_swapChainPanel.ActualWidth, 1);
            float height = Math.Max((float)_swapChainPanel.ActualHeight, 1);

            // Create swap chain - using default buffer count (2) for lowest latency
            _swapChain = new CanvasSwapChain(
                _canvasDevice,
                width,
                height,
                96); // DPI

            _swapChainPanel.SwapChain = _swapChain;
            _isSwapChainReady = true;
            RequestRedraw();

            Debug.WriteLine($"[MainWindow] Created swap chain {width}x{height}");
        }
    }

    private void CanvasDevice_DeviceLost(CanvasDevice sender, object args)
    {
        Debug.WriteLine("[MainWindow] Canvas device lost, recreating...");

        lock (_renderLock)
        {
            _isSwapChainReady = false;
        }

        // Recreate device on UI thread
        DispatcherQueue.TryEnqueue(() =>
        {
            _canvasDevice?.Dispose();
            _canvasDevice = CanvasDevice.GetSharedDevice();
            _canvasDevice.DeviceLost += CanvasDevice_DeviceLost;
            _renderer.Initialize(_canvasDevice);
            CreateSwapChain();
        });
    }

    private void SetupInputHandlers()
    {
        if (_swapChainPanel == null)
            return;

        // Pointer events for pen and touch
        _swapChainPanel.PointerPressed += SwapChainPanel_PointerPressed;
        _swapChainPanel.PointerMoved += SwapChainPanel_PointerMoved;
        _swapChainPanel.PointerReleased += SwapChainPanel_PointerReleased;
        _swapChainPanel.PointerCanceled += SwapChainPanel_PointerCanceled;

        // Mouse wheel for zoom
        _swapChainPanel.PointerWheelChanged += SwapChainPanel_PointerWheelChanged;

        Debug.WriteLine("[MainWindow] Input handlers configured");
    }

    #region Input Handling

    private void SwapChainPanel_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (_swapChainPanel == null)
            return;

        var pointer = e.Pointer;
        var currentPoint = e.GetCurrentPoint(_swapChainPanel);
        long inputTimestamp = _latencyTracker.GetTimestamp();

        Debug.WriteLine($"[Input] PointerPressed - Type: {pointer.PointerDeviceType}, Id: {pointer.PointerId}");

        if (pointer.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Pen)
        {
            // Start new stroke for pen input
            StartNewStroke(currentPoint, inputTimestamp);
            _swapChainPanel.CapturePointer(pointer);
            e.Handled = true;
        }
        else if (pointer.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Touch)
        {
            // Track touch points for pan/zoom gestures
            var position = new Vector2((float)currentPoint.Position.X, (float)currentPoint.Position.Y);
            _touchPoints[pointer.PointerId] = position;

            if (_touchPoints.Count == 2)
            {
                // Start pinch gesture
                StartPinchGesture();
            }

            e.Handled = true;
        }
    }

    private void SwapChainPanel_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_swapChainPanel == null)
            return;

        var pointer = e.Pointer;
        long inputTimestamp = _latencyTracker.GetTimestamp();

        if (pointer.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Pen && _activeStroke != null)
        {
            // Get all intermediate points for accurate stroke capture
            // This captures points that may have been batched by the input system
            var points = e.GetIntermediatePoints(_swapChainPanel);

            foreach (var point in points)
            {
                if (point.IsInContact)
                {
                    AddPointToActiveStroke(point, inputTimestamp);
                }
            }

            // Immediately signal render thread for lowest latency
            RequestRedraw();
            e.Handled = true;
        }
        else if (pointer.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Touch)
        {
            var currentPoint = e.GetCurrentPoint(_swapChainPanel);
            var position = new Vector2((float)currentPoint.Position.X, (float)currentPoint.Position.Y);

            if (_touchPoints.ContainsKey(pointer.PointerId))
            {
                var previousPosition = _touchPoints[pointer.PointerId];
                _touchPoints[pointer.PointerId] = position;

                if (_touchPoints.Count == 1)
                {
                    // Single finger pan
                    var delta = position - previousPosition;
                    _transform.Pan(delta);
                    RequestRedraw();
                }
                else if (_touchPoints.Count == 2 && _isPinching)
                {
                    // Two finger pinch zoom
                    UpdatePinchGesture();
                }

                e.Handled = true;
            }
        }
    }

    private void SwapChainPanel_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (_swapChainPanel == null)
            return;

        var pointer = e.Pointer;

        Debug.WriteLine($"[Input] PointerReleased - Type: {pointer.PointerDeviceType}, Id: {pointer.PointerId}");

        if (pointer.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Pen)
        {
            CompleteActiveStroke();
            _swapChainPanel.ReleasePointerCapture(pointer);
            e.Handled = true;
        }
        else if (pointer.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Touch)
        {
            _touchPoints.Remove(pointer.PointerId);
            if (_touchPoints.Count < 2)
            {
                _isPinching = false;
            }
            e.Handled = true;
        }
    }

    private void SwapChainPanel_PointerCanceled(object sender, PointerRoutedEventArgs e)
    {
        var pointer = e.Pointer;

        Debug.WriteLine($"[Input] PointerCanceled - Type: {pointer.PointerDeviceType}");

        if (pointer.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Pen)
        {
            // Discard the active stroke on cancel
            _activeStroke = null;
            RequestRedraw();
        }

        _touchPoints.Remove(pointer.PointerId);
        _isPinching = false;
        e.Handled = true;
    }

    private void SwapChainPanel_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        if (_swapChainPanel == null)
            return;

        var currentPoint = e.GetCurrentPoint(_swapChainPanel);
        var position = new Vector2((float)currentPoint.Position.X, (float)currentPoint.Position.Y);
        int delta = currentPoint.Properties.MouseWheelDelta;

        // Zoom in/out based on wheel direction
        float scaleFactor = delta > 0 ? 1.1f : 0.9f;
        _transform.ZoomAt(position, scaleFactor);

        RequestRedraw();
        e.Handled = true;
    }

    private void Content_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        var ctrl = InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control)
            .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

        if (ctrl)
        {
            if (e.Key == Windows.System.VirtualKey.Z)
            {
                // Undo
                if (_strokes.RemoveLast())
                {
                    Debug.WriteLine("[Input] Undo - removed last stroke");
                    RequestRedraw();
                }
                e.Handled = true;
            }
            else if (e.Key == Windows.System.VirtualKey.R)
            {
                // Reset view
                _transform.Reset();
                Debug.WriteLine("[Input] Reset view");
                RequestRedraw();
                e.Handled = true;
            }
        }
    }

    #endregion

    #region Stroke Management

    private void StartNewStroke(PointerPoint currentPoint, long timestamp)
    {
        int strokeId = _strokes.GetNextStrokeId();
        _activeStroke = new InkStroke(strokeId, _inkColor, BaseStrokeThickness);

        AddPointToActiveStroke(currentPoint, timestamp);

        Debug.WriteLine($"[Stroke] Started new stroke #{strokeId}");
    }

    private void AddPointToActiveStroke(PointerPoint point, long timestamp)
    {
        if (_activeStroke == null)
            return;

        // Convert screen position to canvas position
        var screenPos = new Vector2((float)point.Position.X, (float)point.Position.Y);
        var canvasPos = _transform.ScreenToCanvas(screenPos);

        // Get pressure (default to 0.5 if not available)
        float pressure = point.Properties.Pressure;
        if (pressure <= 0)
            pressure = 0.5f;

        var inkPoint = new InkPoint(canvasPos, pressure, timestamp);
        _activeStroke.AddPoint(inkPoint);

        // Record latency for this point
        _latencyTracker.RecordLatency(timestamp);
    }

    private void CompleteActiveStroke()
    {
        if (_activeStroke == null)
            return;

        if (_activeStroke.PointCount > 0)
        {
            _activeStroke.Complete();
            _strokes.Add(_activeStroke);
            Debug.WriteLine($"[Stroke] Completed stroke #{_activeStroke.Id} with {_activeStroke.PointCount} points");

            // Log final latency statistics
            _latencyTracker.LogStatistics("StrokeComplete");
        }

        _activeStroke = null;
        RequestRedraw();
    }

    #endregion

    #region Pinch Gesture

    private void StartPinchGesture()
    {
        if (_touchPoints.Count != 2)
            return;

        var points = new Vector2[2];
        int i = 0;
        foreach (var p in _touchPoints.Values)
        {
            points[i++] = p;
            if (i >= 2) break;
        }

        _lastPinchDistance = Vector2.Distance(points[0], points[1]);
        _lastPinchCenter = (points[0] + points[1]) * 0.5f;
        _isPinching = true;

        Debug.WriteLine($"[Gesture] Started pinch - distance: {_lastPinchDistance:F2}");
    }

    private void UpdatePinchGesture()
    {
        if (_touchPoints.Count != 2 || !_isPinching)
            return;

        var points = new Vector2[2];
        int i = 0;
        foreach (var p in _touchPoints.Values)
        {
            points[i++] = p;
            if (i >= 2) break;
        }

        float currentDistance = Vector2.Distance(points[0], points[1]);
        var currentCenter = (points[0] + points[1]) * 0.5f;

        // Calculate zoom
        if (_lastPinchDistance > 0)
        {
            float scaleFactor = currentDistance / _lastPinchDistance;
            _transform.ZoomAt(_lastPinchCenter, scaleFactor);
        }

        // Calculate pan
        var panDelta = currentCenter - _lastPinchCenter;
        _transform.Pan(panDelta);

        _lastPinchDistance = currentDistance;
        _lastPinchCenter = currentCenter;

        RequestRedraw();
    }

    #endregion

    #region Render Loop

    private void StartRenderLoop()
    {
        _isRunning = true;
        _renderThread = new Thread(RenderLoop)
        {
            IsBackground = true,
            Priority = ThreadPriority.Highest,
            Name = "InkRenderThread"
        };
        _renderThread.Start();

        Debug.WriteLine("[Render] Started render thread with high priority");
    }

    private void StopRenderLoop()
    {
        _isRunning = false;
        _renderEvent.Set(); // Wake up the thread so it can exit
        _renderThread?.Join(1000);
        _renderThread = null;

        Debug.WriteLine("[Render] Stopped render thread");
    }

    private void RequestRedraw()
    {
        // Signal the render thread immediately using event
        // This is faster than polling with Thread.Sleep
        _renderEvent.Set();
    }

    private void RenderLoop()
    {
        while (_isRunning)
        {
            try
            {
                // Wait for render request with timeout
                // Using event-based signaling for lower latency than polling
                bool signaled = _renderEvent.WaitOne(16); // Max 16ms wait (60fps minimum)

                if (_isSwapChainReady && (signaled || _activeStroke != null))
                {
                    RenderFrame();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Render] Error in render loop: {ex.Message}");
                Thread.Sleep(16); // Wait a frame before retrying
            }
        }
    }

    private void RenderFrame()
    {
        lock (_renderLock)
        {
            if (_swapChain == null || !_isSwapChainReady)
                return;

            try
            {
                long frameStartTicks = _latencyTracker.GetTimestamp();

                using var ds = _swapChain.CreateDrawingSession(Microsoft.UI.Colors.White);

                float width = (float)_swapChain.Size.Width;
                float height = (float)_swapChain.Size.Height;

                _renderer.Render(ds, _transform, _strokes, _activeStroke, width, height);

                _swapChain.Present();

                // Log frame latency periodically
                double frameTimeMs = LatencyTracker.TicksToMs(_latencyTracker.GetTimestamp() - frameStartTicks);
                if (frameTimeMs > 16)
                {
                    Debug.WriteLine($"[Render] WARNING: Frame time {frameTimeMs:F2}ms exceeds 16ms target");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Render] Error rendering frame: {ex.Message}");
            }
        }
    }

    #endregion
}
