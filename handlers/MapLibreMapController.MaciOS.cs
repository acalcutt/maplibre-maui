#if IOS || MACCATALYST
using CoreGraphics;
using Foundation;
using ObjCRuntime;
using UIKit;
using System.Text.Json;
using Maui.MapLibre.Native;
using Maui.MapLibre.Handlers.Geometry;
using Map    = Maui.MapLibre.Handlers.Maps.Map;
using Style  = Maui.MapLibre.Handlers.Maps.Style;
using Location = Microsoft.Maui.Devices.Sensors.Location;

namespace Maui.MapLibre.Handlers;

// -- Container UIView --------------------------------------------------------

/// <summary>Simple container view; the MTKView rendered by the C++ backend is
/// inserted as a subview once the frontend is initialised.</summary>
[Register("MapContainerView")]
public sealed class MapContainerView : UIView
{
    public Action<int, int>? OnResized;

    public override void LayoutSubviews()
    {
        base.LayoutSubviews();

        // Keep any MTKView subview filling the container.
        foreach (var sv in Subviews)
            sv.Frame = Bounds;

        var scale = UIScreen.MainScreen.Scale;
        int w = Math.Max(1, (int)(Bounds.Width  * scale));
        int h = Math.Max(1, (int)(Bounds.Height * scale));
        OnResized?.Invoke(w, h);
    }
}

// -- Controller ----------------------------------------------------------------

/// <summary>
/// iOS / Mac Catalyst IMapLibreMapController backed by mbgl-cabi (Metal frontend).
/// Platform view is a plain container UIView; the C++ Metal backend owns an MTKView
/// which is retrieved via GetNativeView() and added as a subview on first layout.
/// </summary>
public class MapLibreMapController : IMapLibreMapController
{
    // -- Layout property names (same set as Windows/Android) ------------------

    private static readonly HashSet<string> LayoutPropertyNames = new(StringComparer.Ordinal)
    {
        "visibility",
        "symbol-placement","symbol-spacing","symbol-avoid-edges","symbol-sort-key","symbol-z-order",
        "icon-allow-overlap","icon-ignore-placement","icon-optional","icon-rotation-alignment",
        "icon-size","icon-text-fit","icon-text-fit-padding","icon-image","icon-rotate",
        "icon-padding","icon-keep-upright","icon-offset","icon-anchor","icon-pitch-alignment",
        "text-pitch-alignment","text-rotation-alignment","text-field","text-font","text-size",
        "text-max-width","text-line-height","text-letter-spacing","text-justify",
        "text-radial-offset","text-variable-anchor","text-anchor","text-max-angle",
        "text-writing-mode","text-rotate","text-padding","text-keep-upright","text-transform",
        "text-offset","text-allow-overlap","text-ignore-placement","text-optional",
        "line-cap","line-join","line-miter-limit","line-round-limit","line-sort-key",
        "fill-sort-key","circle-sort-key",
    };

    // -- State -----------------------------------------------------------------

    private readonly string? _styleString;
    private readonly float   _pixelRatio;

    private MbglRunLoop?  _runLoop;
    private MbglFrontend? _frontend;
    private MbglMap?      _map;
    private MbglStyle?    _style;
    private bool          _styleReady;

    public MapContainerView View { get; }

    // -- Events ----------------------------------------------------------------

    public event Action<Map>?                OnMapReadyReceived;
    public event Action?                     OnDidBecomeIdleReceived;
    public event Action<int>?                OnCameraMoveStartedReceived;
    public event Action?                     OnCameraMoveReceived;
    public event Action?                     OnCameraIdleReceived;
    public event Action<int>?                OnCameraTrackingChangedReceived;
    public event Action?                     OnCameraTrackingDismissedReceived;
    public event Func<LatLng, bool>?         OnMapClickReceived;
    public event Func<LatLng, bool>?         OnMapLongClickReceived;
    public event Action<Style>?              OnStyleLoadedReceived;
    public event Action<Location>?           OnUserLocationUpdateReceived;

    // -- Construction ----------------------------------------------------------

    public MapLibreMapController(float pixelRatio, string? styleString)
    {
        _pixelRatio  = pixelRatio;
        _styleString = styleString;

        View = new MapContainerView { OnResized = OnViewResized };
    }

    // -- View size -------------------------------------------------------------

    private void OnViewResized(int w, int h)
    {
        if (_frontend == null)
        {
            TryInitialize(w, h);
            return;
        }
        _frontend.SetSize(w, h);
        _map?.SetSize(w, h);
        _map?.TriggerRepaint();
    }

    private void TryInitialize(int w, int h)
    {
        if (_frontend != null || w < 1 || h < 1) return;

        _runLoop  = new MbglRunLoop();
        // surface_handle unused on Apple (Metal backend creates its own MTKView).
        _frontend = new MbglFrontend(
            IntPtr.Zero,
            IntPtr.Zero,
            w, h, _pixelRatio, OnRender);

        // Wire the MTKView (created by the C++ backend) into the container view.
        var nativeViewPtr = _frontend.GetNativeView();
        if (nativeViewPtr != IntPtr.Zero)
        {
            var metalView = ObjCRuntime.Runtime.GetNSObject<UIView>(nativeViewPtr)!;
            metalView.Frame = View.Bounds;
            View.InsertSubview(metalView, 0);
        }

        _map = new MbglMap(_frontend, _runLoop,
                           pixelRatio: _pixelRatio,
                           observer: OnMapObserverEvent);
        _map.SetSize(w, h);

        if (!string.IsNullOrEmpty(_styleString))
        {
            if (_styleString!.StartsWith('{')) _map.SetStyleJson(_styleString);
            else                               _map.SetStyleUrl(_styleString);
        }

        OnMapReadyReceived?.Invoke(new Map(null));
    }

    private void OnRender()
    {
        // Dispatch to main thread � Metal command buffers must be committed there.
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _frontend?.Render();
            _runLoop?.RunOnce();
        });
    }

    private void OnMapObserverEvent(string eventName)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            switch (eventName)
            {
                case "onDidFinishLoadingStyle":
                    _styleReady = true;
                    _style = _map?.GetStyle();
                    OnStyleLoadedReceived?.Invoke(new Style(null));
                    break;
                case "onDidBecomeIdle":
                    OnDidBecomeIdleReceived?.Invoke();
                    break;
                case "onCameraIsChanging":
                    OnCameraMoveReceived?.Invoke();
                    break;
                case "onCameraDidChange":
                    OnCameraIdleReceived?.Invoke();
                    break;
            }
        });
    }

    // -- IMapLibreMapOptionsSink -----------------------------------------------

    public void SetStyleString(string styleString)
    {
        if (_map == null) return;
        if (styleString.StartsWith('{')) _map.SetStyleJson(styleString);
        else                             _map.SetStyleUrl(styleString);
    }

    public void SetMinMaxZoomPreference(double? min, double? max)
    {
        if (min.HasValue) _map?.SetMinZoom(min.Value);
        if (max.HasValue) _map?.SetMaxZoom(max.Value);
    }

    public void SetCameraTargetBounds(LatLngBounds bounds,
        double minZoom = double.NaN, double maxZoom = double.NaN,
        double minPitch = double.NaN, double maxPitch = double.NaN)
    {
        _map?.SetBounds(bounds.SouthWest.Latitude, bounds.SouthWest.Longitude,
                        bounds.NorthEast.Latitude, bounds.NorthEast.Longitude,
                        minZoom, maxZoom, minPitch, maxPitch);
    }
    public void SetCompassEnabled(bool v)                     { }
    public void SetRotateGesturesEnabled(bool v)              { }
    public void SetScrollGesturesEnabled(bool v)              { }
    public void SetTiltGesturesEnabled(bool v)                { }
    public void SetTrackCameraPosition(bool v)                { }
    public void SetZoomGesturesEnabled(bool v)                { }
    public void SetMyLocationEnabled(bool v)                  { }
    public void SetMyLocationTrackingMode(int v)              { }
    public void SetMyLocationRenderMode(int v)                { }
    public void SetLogoViewMargins(int x, int y)              { }
    public void SetCompassGravity(int gravity)                { }
    public void SetCompassViewMargins(int x, int y)           { }
    public void SetAttributionButtonGravity(int v)            { }
    public void SetAttributionButtonMargins(int x, int y)     { }

    // -- Sources ---------------------------------------------------------------

    public void AddGeoJsonSource(string sourceName, string source)
    {
        if (!_styleReady || _style == null) return;
        if (_style.HasSource(sourceName)) return;
        var s = _style.AddGeoJsonSource(sourceName);
        s.SetGeoJson(source);
    }

    public void SetGeoJsonSource(string sourceName, string source)
        => AddGeoJsonSource(sourceName, source);

    public void SetGeoJsonFeature(string sourceName, string geojsonFeature)
    {
        if (!_styleReady || _style == null) return;
        if (_style.HasSource(sourceName)) _style.RemoveSource(sourceName);
        AddGeoJsonSource(sourceName, geojsonFeature);
    }

    public void AddRasterSource(string sourceName, string? tileUrl,
        string[]? tileUrlTemplates, int tileSize, int minZoom, int maxZoom)
    {
        if (!_styleReady || _style == null) return;
        var url = tileUrl ?? tileUrlTemplates?.FirstOrDefault();
        if (url != null) _style.AddRasterSource(sourceName, url, tileSize);
    }

    public void AddRasterDemSource(string sourceName, string? tileUrl,
        string[]? tileUrlTemplates, int tileSize, int minZoom, int maxZoom)
    {
        if (!_styleReady || _style == null) return;
        var url = tileUrl ?? tileUrlTemplates?.FirstOrDefault();
        if (url != null) _style.AddRasterDemSource(sourceName, url, tileSize);
    }

    public void AddVectorSource(string sourceName, string? tileUrl,
        string[]? tileUrlTemplates, int minZoom, int maxZoom)
    {
        if (!_styleReady || _style == null) return;
        var url = tileUrl ?? tileUrlTemplates?.FirstOrDefault();
        if (url != null) _style.AddVectorSource(sourceName, url);
    }

    public void AddImageSource(string sourceName, string url, LatLngQuad? coordinates)
    {
        if (!_styleReady || _style == null) return;
        _style.AddRasterSource(sourceName, url);
    }

    public void RemoveSource(string sourceId)
    {
        if (!_styleReady || _style == null) return;
        _style.RemoveSource(sourceId);
    }

    // -- Layers ----------------------------------------------------------------

    public void AddFillLayer(string layerName, string sourceName, string? belowLayerId,
        string? sourceLayer, IDictionary<string, object?> properties,
        float minZoom = 0, float maxZoom = 0, bool enableInteraction = false)
    {
        if (!_styleReady || _style == null) return;
        var layer = _style.AddFillLayer(layerName, sourceName, belowLayerId);
        ApplyLayerMeta(layer, sourceLayer, minZoom, maxZoom);
        ApplyProperties(layer, properties);
    }

    public void AddLineLayer(string layerName, string sourceName, string? belowLayerId,
        string? sourceLayer, IDictionary<string, object?> properties,
        float minZoom = 0, float maxZoom = 0, bool enableInteraction = false)
    {
        if (!_styleReady || _style == null) return;
        var layer = _style.AddLineLayer(layerName, sourceName, belowLayerId);
        ApplyLayerMeta(layer, sourceLayer, minZoom, maxZoom);
        ApplyProperties(layer, properties);
    }

    public void AddCircleLayer(string layerName, string sourceName, string? belowLayerId,
        string? sourceLayer, IDictionary<string, object?> properties,
        float minZoom = 0, float maxZoom = 0, bool enableInteraction = false)
    {
        if (!_styleReady || _style == null) return;
        var layer = _style.AddCircleLayer(layerName, sourceName, belowLayerId);
        ApplyLayerMeta(layer, sourceLayer, minZoom, maxZoom);
        ApplyProperties(layer, properties);
    }

    public void AddSymbolLayer(string layerName, string sourceName, string? belowLayerId,
        string? sourceLayer, IDictionary<string, object?> properties,
        float minZoom = 0, float maxZoom = 0, bool enableInteraction = false)
    {
        if (!_styleReady || _style == null) return;
        var layer = _style.AddSymbolLayer(layerName, sourceName, belowLayerId);
        ApplyLayerMeta(layer, sourceLayer, minZoom, maxZoom);
        ApplyProperties(layer, properties);
    }

    public void AddRasterLayer(string layerName, string sourceName,
        IDictionary<string, object?> properties,
        float minZoom = 0, float maxZoom = 0, string? belowLayerId = null)
    {
        if (!_styleReady || _style == null) return;
        var layer = _style.AddRasterLayer(layerName, sourceName, belowLayerId);
        ApplyLayerMeta(layer, null, minZoom, maxZoom);
        ApplyProperties(layer, properties);
    }

    public void AddHillshadeLayer(string layerName, string sourceName,
        IDictionary<string, object?> properties,
        float minZoom = 0, float maxZoom = 0, string? belowLayerId = null)
    {
        if (!_styleReady || _style == null) return;
        var layer = _style.AddHillshadeLayer(layerName, sourceName, belowLayerId);
        ApplyLayerMeta(layer, null, minZoom, maxZoom);
        ApplyProperties(layer, properties);
    }

    public void AddFillExtrusionLayer(string layerName, string sourceName,
        string? belowLayerId, string? sourceLayer,
        IDictionary<string, object?> properties,
        float minZoom = 0, float maxZoom = 0, bool enableInteraction = false)
    {
        if (!_styleReady || _style == null) return;
        var layer = _style.AddFillExtrusionLayer(layerName, sourceName, belowLayerId);
        ApplyLayerMeta(layer, sourceLayer, minZoom, maxZoom);
        ApplyProperties(layer, properties);
    }

    public void AddHeatmapLayer(string layerName, string sourceName,
        IDictionary<string, object?> properties,
        float minZoom = 0, float maxZoom = 0, string? belowLayerId = null)
    {
        if (!_styleReady || _style == null) return;
        var layer = _style.AddHeatmapLayer(layerName, sourceName, belowLayerId);
        ApplyLayerMeta(layer, null, minZoom, maxZoom);
        ApplyProperties(layer, properties);
    }

    public void RemoveLayer(string layerId)
    {
        if (!_styleReady || _style == null) return;
        _style.RemoveLayer(layerId);
    }

    // -- Helpers ---------------------------------------------------------------

    private static void ApplyLayerMeta(MbglLayer layer, string? sourceLayer,
        float minZoom, float maxZoom)
    {
        if (sourceLayer != null) layer.SetSourceLayer(sourceLayer);
        if (minZoom > 0) layer.SetMinZoom(minZoom);
        if (maxZoom > 0) layer.SetMaxZoom(maxZoom);
    }

    private void ApplyProperties(MbglLayer layer, IDictionary<string, object?> props)
    {
        foreach (var (k, v) in props)
        {
            var json = JsonSerializer.Serialize(v);
            if (LayoutPropertyNames.Contains(k))
                layer.SetLayoutProperty(k, json);
            else
                layer.SetPaintProperty(k, json);
        }
    }

    // -- Camera ----------------------------------------------------------------

    public void JumpTo(double latitude, double longitude, double zoom,
        double bearing = 0, double pitch = 0)
        => _map?.JumpTo(latitude, longitude, zoom, bearing, pitch);

    public void EaseTo(double latitude, double longitude, double zoom,
        double bearing = 0, double pitch = 0, long durationMs = 300)
        => _map?.EaseTo(latitude, longitude, zoom, bearing, pitch, durationMs);

    public void FlyTo(double latitude, double longitude, double zoom,
        double bearing = 0, double pitch = 0, long durationMs = 500)
        => _map?.FlyTo(latitude, longitude, zoom, bearing, pitch, durationMs);

    public double GetZoom()    => _map?.Zoom    ?? 0;
    public double GetBearing() => _map?.Bearing ?? 0;
    public double GetPitch()   => _map?.Pitch   ?? 0;
    public LatLng GetCenter()
    {
        if (_map == null) return new LatLng(0, 0);
        var (lat, lon) = _map.Center;
        return new LatLng(lat, lon);
    }

    public (double X, double Y) LatLngToScreenPoint(double latitude, double longitude)
        => _map?.PixelForLatLng(latitude, longitude) ?? (0, 0);

    public LatLng ScreenPointToLatLng(double x, double y)
    {
        if (_map == null) return new LatLng(0, 0);
        var (lat, lon) = _map.LatLngForPixel(x, y);
        return new LatLng(lat, lon);
    }

    public string? QueryRenderedFeaturesAtPoint(double x, double y, string? layerIds = null)
        => _map?.QueryRenderedFeaturesAtPoint(x, y, layerIds);

    public string? QueryRenderedFeaturesInBox(double x1, double y1, double x2, double y2,
        string? layerIds = null)
        => _map?.QueryRenderedFeaturesInBox(x1, y1, x2, y2, layerIds);

    // -- Cleanup ---------------------------------------------------------------

    private void DisposeNative()
    {
        _style    = null;
        _map?.Dispose();      _map      = null;
        _frontend?.Dispose(); _frontend = null;
        _runLoop?.Dispose();  _runLoop  = null;
        _styleReady = false;
    }
}
#endif
