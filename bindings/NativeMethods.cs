/**
 * MbglCabi.cs — P/Invoke declarations for mbgl_cabi native library.
 *
 * All handles (RunLoop, Map, Frontend, Style, Source, Layer) are opaque IntPtr.
 * Thread-safety: Map must be used on the same thread as its RunLoop.
 */
using System.Runtime.InteropServices;

namespace Maui.MapLibre.Native;

/// <summary>Raw P/Invoke bindings — prefer the typed wrappers in MbglMap etc.</summary>
internal static partial class NativeMethods
{
#if IOS || MACCATALYST
    private const string Lib = "__Internal";
#elif ANDROID
    private const string Lib = "mbgl-cabi";
#else
    private const string Lib = "mbgl-cabi";
#endif

    // ── Callbacks ─────────────────────────────────────────────────────────────
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void MapObserverFn([MarshalAs(UnmanagedType.LPUTF8Str)] string eventName, IntPtr userdata);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void RenderFn(IntPtr userdata);

    // ── RunLoop ───────────────────────────────────────────────────────────────
    [LibraryImport(Lib, EntryPoint = "mbgl_runloop_create")]
    public static partial IntPtr RunLoopCreate();

    [LibraryImport(Lib, EntryPoint = "mbgl_runloop_destroy")]
    public static partial void RunLoopDestroy(IntPtr rl);

    [LibraryImport(Lib, EntryPoint = "mbgl_runloop_run_once")]
    public static partial void RunLoopRunOnce(IntPtr rl);

    // ── Frontend ──────────────────────────────────────────────────────────────
    [LibraryImport(Lib, EntryPoint = "mbgl_frontend_create_gl")]
    public static partial IntPtr FrontendCreateGl(
        IntPtr surfaceHandle,
        IntPtr glContext,
        int    widthPx,
        int    heightPx,
        float  pixelRatio,
        RenderFn renderCallback,
        IntPtr   renderUserdata);

    [LibraryImport(Lib, EntryPoint = "mbgl_frontend_destroy")]
    public static partial void FrontendDestroy(IntPtr fe);

    [LibraryImport(Lib, EntryPoint = "mbgl_frontend_render")]
    public static partial void FrontendRender(IntPtr fe);

    [LibraryImport(Lib, EntryPoint = "mbgl_frontend_set_size")]
    public static partial void FrontendSetSize(IntPtr fe, int widthPx, int heightPx);

    [LibraryImport(Lib, EntryPoint = "mbgl_frontend_get_native_view")]
    public static partial IntPtr FrontendGetNativeView(IntPtr fe);

    // ── Map ───────────────────────────────────────────────────────────────────
    [LibraryImport(Lib, EntryPoint = "mbgl_map_create",
        StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr MapCreate(
        IntPtr fe,
        IntPtr rl,
        string? cachePath,
        string? assetPath,
        float   pixelRatio,
        MapObserverFn? observer,
        IntPtr  observerUserdata);

    [LibraryImport(Lib, EntryPoint = "mbgl_map_destroy")]
    public static partial void MapDestroy(IntPtr map);

    [LibraryImport(Lib, EntryPoint = "mbgl_map_set_style_url",
        StringMarshalling = StringMarshalling.Utf8)]
    public static partial void MapSetStyleUrl(IntPtr map, string url);

    [LibraryImport(Lib, EntryPoint = "mbgl_map_set_style_json",
        StringMarshalling = StringMarshalling.Utf8)]
    public static partial void MapSetStyleJson(IntPtr map, string json);

    [LibraryImport(Lib, EntryPoint = "mbgl_map_set_size")]
    public static partial void MapSetSize(IntPtr map, int widthPx, int heightPx);

    [LibraryImport(Lib, EntryPoint = "mbgl_map_jump_to")]
    public static partial void MapJumpTo(IntPtr map, double lat, double lon, double zoom, double bearing, double pitch);

    [LibraryImport(Lib, EntryPoint = "mbgl_map_ease_to")]
    public static partial void MapEaseTo(IntPtr map, double lat, double lon, double zoom, double bearing, double pitch, long durationMs);

    [LibraryImport(Lib, EntryPoint = "mbgl_map_get_zoom")]
    public static partial double MapGetZoom(IntPtr map);

    [LibraryImport(Lib, EntryPoint = "mbgl_map_get_bearing")]
    public static partial double MapGetBearing(IntPtr map);

    [LibraryImport(Lib, EntryPoint = "mbgl_map_get_pitch")]
    public static partial double MapGetPitch(IntPtr map);

    [LibraryImport(Lib, EntryPoint = "mbgl_map_get_center")]
    public static partial void MapGetCenter(IntPtr map, out double lat, out double lon);

    [LibraryImport(Lib, EntryPoint = "mbgl_map_set_min_zoom")]
    public static partial void MapSetMinZoom(IntPtr map, double zoom);

    [LibraryImport(Lib, EntryPoint = "mbgl_map_set_max_zoom")]
    public static partial void MapSetMaxZoom(IntPtr map, double zoom);

    [LibraryImport(Lib, EntryPoint = "mbgl_map_on_scroll")]
    public static partial void MapOnScroll(IntPtr map, double delta, double cx, double cy);

    [LibraryImport(Lib, EntryPoint = "mbgl_map_on_double_tap")]
    public static partial void MapOnDoubleTap(IntPtr map, double x, double y);

    [LibraryImport(Lib, EntryPoint = "mbgl_map_on_pan_start")]
    public static partial void MapOnPanStart(IntPtr map, double x, double y);

    [LibraryImport(Lib, EntryPoint = "mbgl_map_on_pan_move")]
    public static partial void MapOnPanMove(IntPtr map, double dx, double dy);

    [LibraryImport(Lib, EntryPoint = "mbgl_map_on_pan_end")]
    public static partial void MapOnPanEnd(IntPtr map);

    [LibraryImport(Lib, EntryPoint = "mbgl_map_on_pinch")]
    public static partial void MapOnPinch(IntPtr map, double scaleFactor, double cx, double cy);

    [LibraryImport(Lib, EntryPoint = "mbgl_map_trigger_repaint")]
    public static partial void MapTriggerRepaint(IntPtr map);

    [LibraryImport(Lib, EntryPoint = "mbgl_map_fly_to")]
    public static partial void MapFlyTo(IntPtr map, double lat, double lon,
        double zoom, double bearing, double pitch, long durationMs);

    [LibraryImport(Lib, EntryPoint = "mbgl_map_set_bounds")]
    public static partial void MapSetBounds(IntPtr map,
        double latSw, double lonSw, double latNe, double lonNe,
        double minZoom, double maxZoom, double minPitch, double maxPitch);

    [LibraryImport(Lib, EntryPoint = "mbgl_map_camera_for_bounds")]
    public static partial void MapCameraForBounds(IntPtr map,
        double latSw, double lonSw, double latNe, double lonNe,
        double padTop, double padLeft, double padBottom, double padRight,
        out double outLat, out double outLon,
        out double outZoom, out double outBearing, out double outPitch);

    [LibraryImport(Lib, EntryPoint = "mbgl_map_pixel_for_latlng")]
    public static partial void MapPixelForLatLng(IntPtr map, double lat, double lon,
        out double outX, out double outY);

    [LibraryImport(Lib, EntryPoint = "mbgl_map_latlng_for_pixel")]
    public static partial void MapLatLngForPixel(IntPtr map, double x, double y,
        out double outLat, out double outLon);

    [LibraryImport(Lib, EntryPoint = "mbgl_map_set_projection_mode")]
    public static partial void MapSetProjectionMode(IntPtr map,
        int axonometric, double xSkew, double ySkew);

    [LibraryImport(Lib, EntryPoint = "mbgl_map_query_rendered_features_at_point",
        StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr MapQueryRenderedFeaturesAtPoint(IntPtr map,
        double x, double y, string? layerIds);

    [LibraryImport(Lib, EntryPoint = "mbgl_map_query_rendered_features_in_box",
        StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr MapQueryRenderedFeaturesInBox(IntPtr map,
        double x1, double y1, double x2, double y2, string? layerIds);

    [LibraryImport(Lib, EntryPoint = "mbgl_free_string")]
    public static partial void FreeString(IntPtr str);

    // ── Style ─────────────────────────────────────────────────────────────────
    [LibraryImport(Lib, EntryPoint = "mbgl_map_get_style")]
    public static partial IntPtr MapGetStyle(IntPtr map);

    // ── Sources ───────────────────────────────────────────────────────────────
    [LibraryImport(Lib, EntryPoint = "mbgl_style_add_geojson_source",
        StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr StyleAddGeoJsonSource(IntPtr style, string sourceId);

    [LibraryImport(Lib, EntryPoint = "mbgl_style_add_geojson_source_url",
        StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr StyleAddGeoJsonSourceUrl(IntPtr style, string sourceId, string url);

    [LibraryImport(Lib, EntryPoint = "mbgl_geojson_source_set_data",
        StringMarshalling = StringMarshalling.Utf8)]
    public static partial void GeoJsonSourceSetData(IntPtr source, string geojson);

    [LibraryImport(Lib, EntryPoint = "mbgl_geojson_source_set_url",
        StringMarshalling = StringMarshalling.Utf8)]
    public static partial void GeoJsonSourceSetUrl(IntPtr source, string url);

    [LibraryImport(Lib, EntryPoint = "mbgl_style_add_vector_source",
        StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr StyleAddVectorSource(IntPtr style, string sourceId, string url);

    [LibraryImport(Lib, EntryPoint = "mbgl_style_add_raster_source",
        StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr StyleAddRasterSource(IntPtr style, string sourceId, string url, int tileSize);

    [LibraryImport(Lib, EntryPoint = "mbgl_style_add_rasterdem_source",
        StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr StyleAddRasterDemSource(IntPtr style, string sourceId, string url, int tileSize);

    [LibraryImport(Lib, EntryPoint = "mbgl_style_remove_source",
        StringMarshalling = StringMarshalling.Utf8)]
    public static partial void StyleRemoveSource(IntPtr style, string sourceId);

    [LibraryImport(Lib, EntryPoint = "mbgl_style_has_source",
        StringMarshalling = StringMarshalling.Utf8)]
    public static partial int StyleHasSource(IntPtr style, string sourceId);

    // ── Layers ────────────────────────────────────────────────────────────────
    [LibraryImport(Lib, EntryPoint = "mbgl_style_add_fill_layer",
        StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr StyleAddFillLayer(IntPtr style, string layerId, string sourceId, string? beforeLayerId);

    [LibraryImport(Lib, EntryPoint = "mbgl_style_add_line_layer",
        StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr StyleAddLineLayer(IntPtr style, string layerId, string sourceId, string? beforeLayerId);

    [LibraryImport(Lib, EntryPoint = "mbgl_style_add_circle_layer",
        StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr StyleAddCircleLayer(IntPtr style, string layerId, string sourceId, string? beforeLayerId);

    [LibraryImport(Lib, EntryPoint = "mbgl_style_add_symbol_layer",
        StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr StyleAddSymbolLayer(IntPtr style, string layerId, string sourceId, string? beforeLayerId);

    [LibraryImport(Lib, EntryPoint = "mbgl_style_add_raster_layer",
        StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr StyleAddRasterLayer(IntPtr style, string layerId, string sourceId, string? beforeLayerId);

    [LibraryImport(Lib, EntryPoint = "mbgl_style_add_heatmap_layer",
        StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr StyleAddHeatmapLayer(IntPtr style, string layerId, string sourceId, string? beforeLayerId);

    [LibraryImport(Lib, EntryPoint = "mbgl_style_add_hillshade_layer",
        StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr StyleAddHillshadeLayer(IntPtr style, string layerId, string sourceId, string? beforeLayerId);

    [LibraryImport(Lib, EntryPoint = "mbgl_style_add_fill_extrusion_layer",
        StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr StyleAddFillExtrusionLayer(IntPtr style, string layerId, string sourceId, string? beforeLayerId);

    [LibraryImport(Lib, EntryPoint = "mbgl_style_add_background_layer",
        StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr StyleAddBackgroundLayer(IntPtr style, string layerId, string? beforeLayerId);

    [LibraryImport(Lib, EntryPoint = "mbgl_style_add_location_indicator_layer",
        StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr StyleAddLocationIndicatorLayer(IntPtr style, string layerId, string? beforeLayerId);

    [LibraryImport(Lib, EntryPoint = "mbgl_style_add_color_relief_layer",
        StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr StyleAddColorReliefLayer(IntPtr style, string layerId, string sourceId, string? beforeLayerId);

    [LibraryImport(Lib, EntryPoint = "mbgl_style_add_image",
        StringMarshalling = StringMarshalling.Utf8)]
    public static unsafe partial void StyleAddImage(IntPtr style, string imageId,
        int width, int height, float pixelRatio, int sdf, byte* rgbaPremultiplied);

    [LibraryImport(Lib, EntryPoint = "mbgl_style_remove_image",
        StringMarshalling = StringMarshalling.Utf8)]
    public static partial void StyleRemoveImage(IntPtr style, string imageId);

    [LibraryImport(Lib, EntryPoint = "mbgl_style_remove_layer",
        StringMarshalling = StringMarshalling.Utf8)]
    public static partial void StyleRemoveLayer(IntPtr style, string layerId);

    [LibraryImport(Lib, EntryPoint = "mbgl_style_has_layer",
        StringMarshalling = StringMarshalling.Utf8)]
    public static partial int StyleHasLayer(IntPtr style, string layerId);

    [LibraryImport(Lib, EntryPoint = "mbgl_layer_set_source_layer",
        StringMarshalling = StringMarshalling.Utf8)]
    public static partial void LayerSetSourceLayer(IntPtr layer, string sourceLayer);

    [LibraryImport(Lib, EntryPoint = "mbgl_layer_set_filter",
        StringMarshalling = StringMarshalling.Utf8)]
    public static partial void LayerSetFilter(IntPtr layer, string filterJson);

    [LibraryImport(Lib, EntryPoint = "mbgl_layer_set_min_zoom")]
    public static partial void LayerSetMinZoom(IntPtr layer, float zoom);

    [LibraryImport(Lib, EntryPoint = "mbgl_layer_set_max_zoom")]
    public static partial void LayerSetMaxZoom(IntPtr layer, float zoom);

    [LibraryImport(Lib, EntryPoint = "mbgl_layer_set_visibility")]
    public static partial void LayerSetVisibility(IntPtr layer, int visible);

    [LibraryImport(Lib, EntryPoint = "mbgl_layer_set_paint_property",
        StringMarshalling = StringMarshalling.Utf8)]
    public static partial void LayerSetPaintProperty(IntPtr layer, string name, string valueJson);

    [LibraryImport(Lib, EntryPoint = "mbgl_layer_set_layout_property",
        StringMarshalling = StringMarshalling.Utf8)]
    public static partial void LayerSetLayoutProperty(IntPtr layer, string name, string valueJson);

#if ANDROID
    // ── Android ANativeWindow helpers ──────────────────────────────────────────
    [DllImport(Lib, EntryPoint = "mbgl_android_acquire_window")]
    public static extern IntPtr AndroidAcquireWindow(IntPtr jniEnv, IntPtr surface);

    [DllImport(Lib, EntryPoint = "mbgl_android_release_window")]
    public static extern void AndroidReleaseWindow(IntPtr window);
#endif
}
