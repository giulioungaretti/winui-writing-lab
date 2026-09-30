using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Windows.Devices.Haptics;
using Windows.Devices.Input;
using Windows.UI.Input;

namespace InkControl.Input;

internal sealed class NativePenHaptics(ILogger? logger)
{
    private SimpleHapticsController? _controller;

    public void Start(PointerPoint point)
    {
        if (point.PointerDevice.PointerDeviceType != PointerDeviceType.Pen)
            return;

        try
        {
            _controller = PenDevice.GetFromPointerId(point.PointerId)?.SimpleHapticsController;
            var waveform = _controller?.SupportedFeedback.FirstOrDefault(
                f => f.Waveform == KnownSimpleHapticsControllerWaveforms.InkContinuous);
            if (waveform is not null)
                _controller!.SendHapticFeedback(waveform);
        }
        catch (Exception ex) when (ex is COMException or ArgumentException)
        {
            logger?.LogWarning(ex, "Pen haptic feedback is unavailable");
            _controller = null;
        }
    }

    public void Stop()
    {
        try
        {
            _controller?.StopFeedback();
        }
        catch (COMException ex)
        {
            logger?.LogWarning(ex, "Could not stop pen haptic feedback");
        }
        finally
        {
            _controller = null;
        }
    }
}
