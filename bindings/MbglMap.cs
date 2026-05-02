/**
 * MbglMap.cs — Typed C# wrapper around the native mbgl_map_t handle.
 *
 * Lifetime: must be disposed on the same thread as its MbglRunLoop.
 * The MbglFrontend must outlive the MbglMap.
 */
using System.Runtime.InteropServices;

namespace Maui.MapLibre.Native;

/// <summary>Wraps <c>mbgl_map_t*</c>. Dispose on the render thread.</summary>
public sealed class MbglMap : IDisposable
{
    internal IntPtr Handle { get; private set; }

    // Keep a GC handle to the native delegate so it isn't collected
    private GCHandle _observerHandle;

    public MbglMap(
        MbglFrontend frontend,
        MbglRunLoop runLoop,
        string? cachePath = null,
        string? assetPath = null,
        float   pixelRatio = 1.0f,
        Action<string>? observer = null)
    {
        NativeMethods.MapObserverFn? nativeObserver = null;
        if (observer != null)
        {
            nativeObserver = (eventName, _) => observer(eventName);
            _observerHandle = GCHandle.Alloc(nativeObserver);
        }

        Handle = NativeMethods.MapCreate(
            frontend.Handle, runLoop.Handle,
            cachePath, assetPath,
            pixelRatio,
            nativeObserver, IntPtr.Zero);

        if (Handle == IntPtr.Zero)
            throw new InvalidOperationException("mbgl_map_create returned null.");
    }

    public void SetStyleUrl(string url)  => NativeMethods.MapSetStyleUrl(Handle, url);
    public void SetStyleJson(string json) => NativeMethods.MapSetStyleJson(Handle, json);

    public void SetSize(int widthPx, int heightPx)
        => NativeMethods.MapSetSize(Handle, widthPx, heightPx);

    public void JumpTo(double lat, double lon, double zoom, double bearing = 0, double pitch = 0)
        => NativeMethods.MapJumpTo(Handle, lat, lon, zoom, bearing, pitch);

    public void EaseTo(double lat, double lon, double zoom, double bearing, double pitch, long durationMs)
        => NativeMethods.MapEaseTo(Handle, lat, lon, zoom, bearing, pitch, durationMs);

    public double Zoom    => NativeMethods.MapGetZoom(Handle);
    public double Bearing => NativeMethods.MapGetBearing(Handle);
    public double Pitch   => NativeMethods.MapGetPitch(Handle);

    public (double Lat, double Lon) Center
    {
        get
        {
            NativeMethods.MapGetCenter(Handle, out var lat, out var lon);
            return (lat, lon);
        }
    }

    public void SetMinZoom(double zoom) => NativeMethods.MapSetMinZoom(Handle, zoom);
    public void SetMaxZoom(double zoom) => NativeMethods.MapSetMaxZoom(Handle, zoom);

    public void OnScroll(double delta, double cx, double cy)
        => NativeMethods.MapOnScroll(Handle, delta, cx, cy);
    public void OnDoubleTap(double x, double y)
        => NativeMethods.MapOnDoubleTap(Handle, x, y);
    public void OnPanStart(double x, double y)
        => NativeMethods.MapOnPanStart(Handle, x, y);
    public void OnPanMove(double dx, double dy)
        => NativeMethods.MapOnPanMove(Handle, dx, dy);
    public void OnPanEnd()
        => NativeMethods.MapOnPanEnd(Handle);
    public void OnPinch(double scaleFactor, double cx, double cy)
        => NativeMethods.MapOnPinch(Handle, scaleFactor, cx, cy);

    public void TriggerRepaint() => NativeMethods.MapTriggerRepaint(Handle);

    public MbglStyle GetStyle()
    {
        var styleHandle = NativeMethods.MapGetStyle(Handle);
        if (styleHandle == IntPtr.Zero)
            throw new InvalidOperationException("Style is not yet loaded.");
        return new MbglStyle(styleHandle);
    }

    public void Dispose()
    {
        if (Handle != IntPtr.Zero)
        {
            NativeMethods.MapDestroy(Handle);
            Handle = IntPtr.Zero;
        }
        if (_observerHandle.IsAllocated)
            _observerHandle.Free();
    }
}
