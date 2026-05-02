/**
 * MbglStyle.cs — Typed wrapper around mbgl_style_t (non-owning, valid for the
 * lifetime of its parent MbglMap).
 */
using System.Text.Json;

namespace Maui.MapLibre.Native;

/// <summary>
/// Provides access to sources and layers.  This is a <em>non-owning</em> handle
/// — do not dispose it; it is invalidated when the parent <see cref="MbglMap"/> is disposed.
/// </summary>
public sealed class MbglStyle
{
    internal IntPtr Handle { get; }

    internal MbglStyle(IntPtr handle) => Handle = handle;

    // ── Sources ───────────────────────────────────────────────────────────────

    public bool HasSource(string sourceId)
        => NativeMethods.StyleHasSource(Handle, sourceId) != 0;

    public MbglSource AddGeoJsonSource(string sourceId)
        => new(NativeMethods.StyleAddGeoJsonSource(Handle, sourceId));

    public MbglSource AddGeoJsonSourceUrl(string sourceId, string url)
        => new(NativeMethods.StyleAddGeoJsonSourceUrl(Handle, sourceId, url));

    public MbglSource AddVectorSource(string sourceId, string url)
        => new(NativeMethods.StyleAddVectorSource(Handle, sourceId, url));

    public MbglSource AddRasterSource(string sourceId, string url, int tileSize = 512)
        => new(NativeMethods.StyleAddRasterSource(Handle, sourceId, url, tileSize));

    public MbglSource AddRasterDemSource(string sourceId, string url, int tileSize = 512)
        => new(NativeMethods.StyleAddRasterDemSource(Handle, sourceId, url, tileSize));

    public void RemoveSource(string sourceId)
        => NativeMethods.StyleRemoveSource(Handle, sourceId);

    // ── Layers ────────────────────────────────────────────────────────────────

    public bool HasLayer(string layerId)
        => NativeMethods.StyleHasLayer(Handle, layerId) != 0;

    public MbglLayer AddFillLayer(string layerId, string sourceId, string? beforeLayerId = null)
        => new(NativeMethods.StyleAddFillLayer(Handle, layerId, sourceId, beforeLayerId));

    public MbglLayer AddLineLayer(string layerId, string sourceId, string? beforeLayerId = null)
        => new(NativeMethods.StyleAddLineLayer(Handle, layerId, sourceId, beforeLayerId));

    public MbglLayer AddCircleLayer(string layerId, string sourceId, string? beforeLayerId = null)
        => new(NativeMethods.StyleAddCircleLayer(Handle, layerId, sourceId, beforeLayerId));

    public MbglLayer AddSymbolLayer(string layerId, string sourceId, string? beforeLayerId = null)
        => new(NativeMethods.StyleAddSymbolLayer(Handle, layerId, sourceId, beforeLayerId));

    public MbglLayer AddRasterLayer(string layerId, string sourceId, string? beforeLayerId = null)
        => new(NativeMethods.StyleAddRasterLayer(Handle, layerId, sourceId, beforeLayerId));

    public MbglLayer AddHeatmapLayer(string layerId, string sourceId, string? beforeLayerId = null)
        => new(NativeMethods.StyleAddHeatmapLayer(Handle, layerId, sourceId, beforeLayerId));

    public MbglLayer AddHillshadeLayer(string layerId, string sourceId, string? beforeLayerId = null)
        => new(NativeMethods.StyleAddHillshadeLayer(Handle, layerId, sourceId, beforeLayerId));

    public MbglLayer AddFillExtrusionLayer(string layerId, string sourceId, string? beforeLayerId = null)
        => new(NativeMethods.StyleAddFillExtrusionLayer(Handle, layerId, sourceId, beforeLayerId));

    public MbglLayer AddBackgroundLayer(string layerId, string? beforeLayerId = null)
        => new(NativeMethods.StyleAddBackgroundLayer(Handle, layerId, beforeLayerId));

    public void RemoveLayer(string layerId)
        => NativeMethods.StyleRemoveLayer(Handle, layerId);
}

// ── Source handle ─────────────────────────────────────────────────────────────

/// <summary>Non-owning handle to a source inside a loaded style.</summary>
public sealed class MbglSource
{
    internal IntPtr Handle { get; }
    internal MbglSource(IntPtr handle) => Handle = handle;

    public void SetGeoJson(string geojson)
        => NativeMethods.GeoJsonSourceSetData(Handle, geojson);

    public void SetUrl(string url)
        => NativeMethods.GeoJsonSourceSetUrl(Handle, url);
}

// ── Layer handle ──────────────────────────────────────────────────────────────

/// <summary>Non-owning handle to a layer inside a loaded style.</summary>
public sealed class MbglLayer
{
    internal IntPtr Handle { get; }
    internal MbglLayer(IntPtr handle) => Handle = handle;

    public void SetSourceLayer(string sourceLayer)
        => NativeMethods.LayerSetSourceLayer(Handle, sourceLayer);

    public void SetFilter(string filterJson)
        => NativeMethods.LayerSetFilter(Handle, filterJson);

    public void SetMinZoom(float zoom) => NativeMethods.LayerSetMinZoom(Handle, zoom);
    public void SetMaxZoom(float zoom) => NativeMethods.LayerSetMaxZoom(Handle, zoom);

    public void SetVisible(bool visible)
        => NativeMethods.LayerSetVisibility(Handle, visible ? 1 : 0);

    /// <param name="valueJson">JSON-encoded value, e.g. <c>"\"#ff0000\""</c> or <c>"[\"get\",\"class\"]"</c></param>
    public void SetPaintProperty(string name, string valueJson)
        => NativeMethods.LayerSetPaintProperty(Handle, name, valueJson);

    /// <param name="valueJson">JSON-encoded value</param>
    public void SetLayoutProperty(string name, string valueJson)
        => NativeMethods.LayerSetLayoutProperty(Handle, name, valueJson);

    // Convenience: accept a C# object and serialize to JSON
    public void SetPaintProperty(string name, object? value)
        => SetPaintProperty(name, JsonSerializer.Serialize(value));

    public void SetLayoutProperty(string name, object? value)
        => SetLayoutProperty(name, JsonSerializer.Serialize(value));
}
