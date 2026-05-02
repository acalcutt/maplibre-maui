using Maui.MapLibre.Handlers.Geometry;
using Map = Maui.MapLibre.Handlers.Maps.Map;
using Style = Maui.MapLibre.Handlers.Maps.Style;

namespace Maui.MapLibre.Handlers;

public interface IMapLibreMapController : IMapLibreMapOptionsSink
{
    // Events
    public event Action<Map>? OnMapReadyReceived;
    public event Action? OnDidBecomeIdleReceived;
    public event Action<int>? OnCameraMoveStartedReceived;
    public event Action? OnCameraMoveReceived;
    public event Action? OnCameraIdleReceived;
    public event Action<int>? OnCameraTrackingChangedReceived;
    public event Action? OnCameraTrackingDismissedReceived;
    public event Func<LatLng, bool>? OnMapClickReceived;
    public event Func<LatLng, bool>? OnMapLongClickReceived;
    public event Action<Style>? OnStyleLoadedReceived;
    public event Action<Location>? OnUserLocationUpdateReceived;
    
    // Sources
    public void AddGeoJsonSource(string sourceName, string source);

    public void AddRasterSource(string sourceName, string? tileUrl, string[]? tileUrlTemplates, int tileSize,
        int minZoom, int maxZoom);

    public void AddRasterDemSource(string sourceName, string? tileUrl, string[]? tileUrlTemplates, int tileSize,
        int minZoom, int maxZoom);

    public void AddVectorSource(string sourceName, string? tileUrl, string[]? tileUrlTemplates, int minZoom,
        int maxZoom);
    public void AddImageSource(string sourceName, string url, LatLngQuad? coordinates);
    public void SetGeoJsonSource(string sourceName, string source);
    public void SetGeoJsonFeature(string sourceName, string geojsonFeature);
    public void RemoveSource(string sourceId);
    
    // Layers
    public void AddSymbolLayer(
        string layerName,
        string sourceName,
        string? belowLayerId,
        string? sourceLayer,
        IDictionary<string, object?> properties,
        float minZoom = 0,
        float maxZoom = 0,
        bool enableInteraction = false);

    public void AddLineLayer(
        string layerName,
        string sourceName,
        string? belowLayerId,
        string? sourceLayer,
        IDictionary<string, object?> properties,
        float minZoom = 0,
        float maxZoom = 0,
        bool enableInteraction = false);

    public void AddFillLayer(
        string layerName,
        string sourceName,
        string? belowLayerId,
        string? sourceLayer,
        IDictionary<string, object?> properties,
        float minZoom = 0,
        float maxZoom = 0,
        bool enableInteraction = false);

    public void AddFillExtrusionLayer(
        string layerName,
        string sourceName,
        string? belowLayerId,
        string? sourceLayer,
        IDictionary<string, object?> properties,
        float minZoom = 0,
        float maxZoom = 0,
        bool enableInteraction = false);

    public void AddCircleLayer(
        string layerName,
        string sourceName,
        string? belowLayerId,
        string? sourceLayer,
        IDictionary<string, object?> properties,
        float minZoom = 0,
        float maxZoom = 0,
        bool enableInteraction = false);

    public void AddRasterLayer(
        string layerName,
        string sourceName,
        IDictionary<string, object?> properties,
        float minZoom = 0,
        float maxZoom = 0,
        string? belowLayerId = null);

    public void AddHillshadeLayer(
        string layerName,
        string sourceName,
        IDictionary<string, object?> properties,
        float minZoom = 0,
        float maxZoom = 0,
        string? belowLayerId = null);

    public void AddHeatmapLayer(
        string layerName,
        string sourceName,
        IDictionary<string, object?> properties,
        float minZoom = 0,
        float maxZoom = 0,
        string? belowLayerId = null);
    
    public void RemoveLayer(string layerId);

    // Camera
    public void JumpTo(double latitude, double longitude, double zoom);
}