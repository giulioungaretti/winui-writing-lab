using Vortice.Direct2D1;
using Vortice.DXGI;

namespace InkControl.Rendering;

/// <summary>
/// Manages a DXGI swap chain for composition with XAML.
/// Uses Vortice.Windows for DirectX interop.
/// </summary>
internal sealed class SwapChainManager : IDisposable
{
    private readonly DeviceManager _deviceManager;
    private IDXGISwapChain1? _swapChain;
    private ID2D1DeviceContext? _d2dContext;
    private ID2D1Bitmap1? _targetBitmap;
    private uint _width;
    private uint _height;
    private float _dpiX = 96f;
    private float _dpiY = 96f;
    private bool _isDisposed;

    public SwapChainManager(DeviceManager deviceManager, uint width, uint height)
    {
        ArgumentNullException.ThrowIfNull(deviceManager);

        _deviceManager = deviceManager;
        _width = Math.Max(1, width);
        _height = Math.Max(1, height);

        CreateSwapChain();
        CreateRenderTarget();
    }

    /// <summary>
    /// Gets the native swap chain pointer for XAML composition.
    /// </summary>
    public nint SwapChain => _swapChain?.NativePointer ?? 0;

    /// <summary>
    /// Gets the D2D device context for rendering.
    /// </summary>
    public ID2D1DeviceContext? D2DContext => _d2dContext;

    /// <summary>
    /// Gets the current width of the swap chain.
    /// </summary>
    public uint Width => _width;

    /// <summary>
    /// Gets the current height of the swap chain.
    /// </summary>
    public uint Height => _height;

    /// <summary>
    /// Sets the DPI for the render target.
    /// </summary>
    public void SetDpi(float dpiX, float dpiY)
    {
        _dpiX = dpiX;
        _dpiY = dpiY;
    }

    private void CreateSwapChain()
    {
        var desc = new SwapChainDescription1
        {
            Width = _width,
            Height = _height,
            Format = Format.B8G8R8A8_UNorm,
            Stereo = false,
            SampleDescription = new SampleDescription(1, 0),
            BufferUsage = Usage.RenderTargetOutput,
            BufferCount = 2,
            Scaling = Scaling.Stretch,
            SwapEffect = SwapEffect.FlipSequential,
            AlphaMode = AlphaMode.Premultiplied,
            Flags = SwapChainFlags.None
        };

        _swapChain = _deviceManager.DXGIFactory!.CreateSwapChainForComposition(
            _deviceManager.D3DDevice!,
            desc);
    }

    private void CreateRenderTarget()
    {
        // Create D2D device context
        _d2dContext = _deviceManager.CreateDeviceContext();

        // Get back buffer surface
        using var surface = _swapChain!.GetBuffer<IDXGISurface>(0);

        // Create bitmap from surface
        var bitmapProps = new BitmapProperties1
        {
            PixelFormat = new Vortice.DCommon.PixelFormat(Format.B8G8R8A8_UNorm, Vortice.DCommon.AlphaMode.Premultiplied),
            DpiX = _dpiX,
            DpiY = _dpiY,
            BitmapOptions = BitmapOptions.Target | BitmapOptions.CannotDraw
        };

        _targetBitmap = _d2dContext.CreateBitmapFromDxgiSurface(surface, bitmapProps);
        _d2dContext.Target = _targetBitmap;
    }

    /// <summary>
    /// Resizes the swap chain to the specified dimensions.
    /// </summary>
    public void Resize(uint width, uint height)
    {
        ThrowIfDisposed();

        width = Math.Max(1, width);
        height = Math.Max(1, height);

        if (width == _width && height == _height) return;

        _width = width;
        _height = height;

        // Release old resources
        _d2dContext!.Target = null;
        _targetBitmap?.Dispose();
        _targetBitmap = null;

        // Resize swap chain
        _swapChain!.ResizeBuffers(2, _width, _height, Format.B8G8R8A8_UNorm, SwapChainFlags.None);

        // Recreate render target
        using var surface = _swapChain.GetBuffer<IDXGISurface>(0);

        var bitmapProps = new BitmapProperties1
        {
            PixelFormat = new Vortice.DCommon.PixelFormat(Format.B8G8R8A8_UNorm, Vortice.DCommon.AlphaMode.Premultiplied),
            DpiX = _dpiX,
            DpiY = _dpiY,
            BitmapOptions = BitmapOptions.Target | BitmapOptions.CannotDraw
        };

        _targetBitmap = _d2dContext.CreateBitmapFromDxgiSurface(surface, bitmapProps);
        _d2dContext.Target = _targetBitmap;
    }

    /// <summary>
    /// Begins a drawing operation.
    /// </summary>
    public void BeginDraw()
    {
        ThrowIfDisposed();
        _d2dContext!.BeginDraw();
    }

    /// <summary>
    /// Ends the current drawing operation.
    /// </summary>
    public void EndDraw()
    {
        ThrowIfDisposed();
        _d2dContext!.EndDraw();
    }

    /// <summary>
    /// Presents the rendered content to the screen.
    /// </summary>
    /// <param name="syncInterval">VSync interval (0 = immediate, 1 = 60Hz sync).</param>
    public void Present(uint syncInterval = 1)
    {
        ThrowIfDisposed();
        _swapChain!.Present(syncInterval, PresentFlags.None);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        _d2dContext?.Target = null;
        _targetBitmap?.Dispose();
        _d2dContext?.Dispose();
        _swapChain?.Dispose();
    }
}
