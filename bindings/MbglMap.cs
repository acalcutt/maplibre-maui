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

    public void FlyTo(double lat, double lon, double zoom, double bearing, double pitch, long durationMs)
        => NativeMethods.MapFlyTo(Handle, lat, lon, zoom, bearing, pitch, durationMs);

    /// <summary>Set geographic constraints and zoom/pitch limits.
    /// Pass <see cref="double.NaN"/> for any parameter to leave it unconstrained.</summary>
    public void SetBounds(double latSw = double.NaN, double lonSw = double.NaN,
                          double latNe = double.NaN, double lonNe = double.NaN,
                          double minZoom = double.NaN, double maxZoom = double.NaN,
                          double minPitch = double.NaN, double maxPitch = double.NaN)
        => NativeMethods.MapSetBounds(Handle, latSw, lonSw, latNe, lonNe,
                                      minZoom, maxZoom, minPitch, maxPitch);

    /// <summary>Returns the CameraOptions (lat, lon, zoom, bearing, pitch) that fits the
    /// given bounds with optional screen padding (top, left, bottom, right in pixels).</summary>
    public (double Lat, double Lon, double Zoom, double Bearing, double Pitch)
        CameraForBounds(double latSw, double lonSw, double latNe, double lonNe,
                        double padTop = 0, double padLeft = 0,
                        double padBottom = 0, double padRight = 0)
    {
        NativeMethods.MapCameraForBounds(Handle, latSw, lonSw, latNe, lonNe,
            padTop, padLeft, padBottom, padRight,
            out var lat, out var lon, out var zoom, out var bearing, out var pitch);
        return (lat, lon, zoom, bearing, pitch);
    }

    public (double X, double Y) PixelForLatLng(double lat, double lon)
    {
        NativeMethods.MapPixelForLatLng(Handle, lat, lon, out var x, out var y);
        return (x, y);
    }

    public (double Lat, double Lon) LatLngForPixel(double x, double y)
    {
        NativeMethods.MapLatLngForPixel(Handle, x, y, out var lat, out var lon);
        return (lat, lon);
    }

    public void SetProjectionMode(bool axonometric = false, double xSkew = 0.0, double ySkew = 1.0)
        => NativeMethods.MapSetProjectionMode(Handle, axonometric ? 1 : 0, xSkew, ySkew);

    /// <summary>Query rendered features at a screen point. Returns a GeoJSON FeatureCollection string,
    /// or null if the renderer is not ready.</summary>
    /// <param name="layerIds">Optional comma-separated layer IDs to restrict the query.</param>
    public string? QueryRenderedFeaturesAtPoint(double x, double y, string? layerIds = null)
    {
        var ptr = NativeMethods.MapQueryRenderedFeaturesAtPoint(Handle, x, y, layerIds);
        if (ptr == IntPtr.Zero) return null;
        var result = Marshal.PtrToStringUTF8(ptr);
        NativeMethods.FreeString(ptr);
        return result;
    }

    /// <summary>Query rendered features in a screen bounding box.</summary>
    public string? QueryRenderedFeaturesInBox(double x1, double y1, double x2, double y2,
                                               string? layerIds = null)
    {
        var ptr = NativeMethods.MapQueryRenderedFeaturesInBox(Handle, x1, y1, x2, y2, layerIds);
        if (ptr == IntPtr.Zero) return null;
        var result = Marshal.PtrToStringUTF8(ptr);
        NativeMethods.FreeString(ptr);
        return result;
    }

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
