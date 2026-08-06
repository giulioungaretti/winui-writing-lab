using Vortice.Direct2D1;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace InkControl.Rendering;

/// <summary>
/// Manages Direct3D 11 and Direct2D device creation and lifecycle.
/// Uses Vortice.Windows for proper managed DirectX bindings.
/// </summary>
internal sealed class DeviceManager : IDisposable
{
    private static readonly object s_lock = new();
    private static DeviceManager? s_instance;

    private ID3D11Device? _d3dDevice;
    private ID3D11DeviceContext? _d3dContext;
    private IDXGIDevice? _dxgiDevice;
    private IDXGIAdapter? _dxgiAdapter;
    private IDXGIFactory2? _dxgiFactory;
    private ID2D1Factory1? _d2dFactory;
    private ID2D1Device? _d2dDevice;
    private ID2D1StrokeStyle? _roundStrokeStyle;
    private bool _isDisposed;

    /// <summary>
    /// Gets the singleton instance of the DeviceManager.
    /// </summary>
    public static DeviceManager Instance
    {
        get
        {
            if (s_instance is null)
            {
                lock (s_lock)
                {
                    s_instance ??= new DeviceManager();
                }
            }
            return s_instance;
        }
    }

    /// <summary>
    /// Gets the Direct3D 11 device.
    /// </summary>
    public ID3D11Device? D3DDevice => _d3dDevice;

    /// <summary>
    /// Gets the Direct2D factory.
    /// </summary>
    public ID2D1Factory1? D2DFactory => _d2dFactory;

    /// <summary>
    /// Gets the Direct2D device.
    /// </summary>
    public ID2D1Device? D2DDevice => _d2dDevice;

    /// <summary>
    /// Gets the DXGI factory for swap chain creation.
    /// </summary>
    public IDXGIFactory2? DXGIFactory => _dxgiFactory;

    /// <summary>
    /// Gets the round stroke style for smooth ink rendering.
    /// </summary>
    public ID2D1StrokeStyle? RoundStrokeStyle => _roundStrokeStyle;

    /// <summary>
    /// Gets whether the D3D/D2D devices are valid and ready for rendering.
    /// </summary>
    public bool IsDeviceValid => _d3dDevice is not null && _d2dDevice is not null;

    /// <summary>
    /// Raised when the D3D device is lost and needs recreation.
    /// </summary>
    public event EventHandler? DeviceLost;

    /// <summary>
    /// Raised when the D3D device has been restored after a device lost event.
    /// </summary>
    public event EventHandler? DeviceRestored;

    private DeviceManager()
    {
        CreateDeviceResources();
    }

    private void CreateDeviceResources()
    {
        // Create D3D11 device with BGRA support for D2D interop
        var result = D3D11.D3D11CreateDevice(
            adapter: null,
            DriverType.Hardware,
            DeviceCreationFlags.BgraSupport,
            null,
            out _d3dDevice,
            out _d3dContext);

        if (result.Failure)
        {
            // Fallback to WARP software renderer
            result = D3D11.D3D11CreateDevice(
                adapter: null,
                DriverType.Warp,
                DeviceCreationFlags.BgraSupport,
                null,
                out _d3dDevice,
                out _d3dContext);
            result.CheckError();
        }

        // Get DXGI device from D3D device
        _dxgiDevice = _d3dDevice!.QueryInterface<IDXGIDevice>();

        // Get adapter from DXGI device
        _dxgiAdapter = _dxgiDevice.GetAdapter();

        // Get factory from adapter
        _dxgiFactory = _dxgiAdapter.GetParent<IDXGIFactory2>();

        // Create D2D factory
        _d2dFactory = D2D1.D2D1CreateFactory<ID2D1Factory1>(FactoryType.SingleThreaded);

        // Create D2D device from DXGI device
        _d2dDevice = _d2dFactory.CreateDevice(_dxgiDevice);

        // Create round stroke style for smooth ink rendering
        var strokeProps = new StrokeStyleProperties
        {
            StartCap = CapStyle.Round,
            EndCap = CapStyle.Round,
            DashCap = CapStyle.Round,
            LineJoin = LineJoin.Round,
            MiterLimit = 10f,
            DashStyle = DashStyle.Solid,
            DashOffset = 0f
        };

        _roundStrokeStyle = _d2dFactory.CreateStrokeStyle(strokeProps);
    }

    /// <summary>
    /// Creates a new Direct2D device context for rendering.
    /// </summary>
    /// <returns>A new device context that must be disposed after use.</returns>
    public ID2D1DeviceContext CreateDeviceContext()
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(_d2dDevice);
        return _d2dDevice.CreateDeviceContext(DeviceContextOptions.None);
    }

    /// <summary>
    /// Checks the device state and attempts recovery if the device was lost.
    /// </summary>
    /// <returns>True if the device is valid, false if recovery failed.</returns>
    public bool CheckDeviceState()
    {
        if (_d3dDevice is null) return false;

        var reason = _d3dDevice.DeviceRemovedReason;
        if (reason.Success) return true;

        DeviceLost?.Invoke(this, EventArgs.Empty);
        ReleaseDeviceResources();

        try
        {
            CreateDeviceResources();
            DeviceRestored?.Invoke(this, EventArgs.Empty);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void ReleaseDeviceResources()
    {
        _roundStrokeStyle?.Dispose();
        _roundStrokeStyle = null;
        _d2dDevice?.Dispose();
        _d2dDevice = null;
        _d2dFactory?.Dispose();
        _d2dFactory = null;
        _dxgiFactory?.Dispose();
        _dxgiFactory = null;
        _dxgiAdapter?.Dispose();
        _dxgiAdapter = null;
        _dxgiDevice?.Dispose();
        _dxgiDevice = null;
        _d3dContext?.Dispose();
        _d3dContext = null;
        _d3dDevice?.Dispose();
        _d3dDevice = null;
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
        ReleaseDeviceResources();
        s_instance = null;
    }
}
