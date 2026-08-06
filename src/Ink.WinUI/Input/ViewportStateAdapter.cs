using System;
using System.Numerics;
using inkapp.Core.Input;
using inkapp.Core.Models;

namespace InkControl.Input;

/// <summary>
/// Adapts a ViewportState to the IViewportProvider interface.
/// This allows InkInputProcessor (which uses IViewportProvider) to work with
/// the ViewportState used in the UI layer.
/// </summary>
internal sealed class ViewportStateAdapter : IViewportProvider
{
    private readonly Func<ViewportState> _getViewport;

    /// <summary>
    /// Creates an adapter that gets the current viewport state from a function.
    /// This allows the adapter to always use the latest viewport state.
    /// </summary>
    /// <param name="getViewport">Function that returns the current viewport state.</param>
    public ViewportStateAdapter(Func<ViewportState> getViewport)
    {
        ArgumentNullException.ThrowIfNull(getViewport);
        _getViewport = getViewport;
    }

    /// <inheritdoc />
    public Vector2 ScreenToWorld(Vector2 screenPosition)
    {
        var viewport = _getViewport();
        return viewport?.ScreenToWorld(screenPosition) ?? screenPosition;
    }

    /// <inheritdoc />
    public Vector2 WorldToScreen(Vector2 worldPosition)
    {
        var viewport = _getViewport();
        return viewport?.WorldToScreen(worldPosition) ?? worldPosition;
    }
}
