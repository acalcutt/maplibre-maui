/**
 * mbgl_cabi.h — Flat C ABI for MapLibre Native.
 *
 * All handles are opaque pointers (void*).
 * Thread-safety: Map must be used on the same thread as its RunLoop.
 */
#pragma once

#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

/* ── Export macro ──────────────────────────────────────────────────────────── */
#if defined(_WIN32)
#  ifdef MBGL_CABI_EXPORT
#    define MBGL_CABI_API __declspec(dllexport)
#  else
#    define MBGL_CABI_API __declspec(dllimport)
#  endif
#else
#  define MBGL_CABI_API __attribute__((visibility("default")))
#endif

/* ── Opaque handle typedefs ────────────────────────────────────────────────── */
typedef void* mbgl_runloop_t;
typedef void* mbgl_frontend_t;
typedef void* mbgl_map_t;
typedef void* mbgl_style_t;
typedef void* mbgl_source_t;
typedef void* mbgl_layer_t;

/* ── Callbacks ─────────────────────────────────────────────────────────────── */
typedef void (*mbgl_render_fn)(void* userdata);
/** Observer callback fired for named map lifecycle events.
 *  @param event_name  Camel-case event name matching the MapObserver virtual method
 *                     (e.g. "onDidFinishLoadingStyle", "onDidBecomeIdle").
 *  @param detail      Optional extra detail: error message for onDidFailLoadingMap,
 *                     image ID for onStyleImageMissing, source ID for onSourceChanged,
 *                     "animated" or "immediate" for camera change events, else NULL.
 *  @param userdata    Opaque pointer passed to mbgl_map_create. */
typedef void (*mbgl_map_observer_fn)(const char* event_name, const char* detail, void* userdata);

/* ── RunLoop ───────────────────────────────────────────────────────────────── */
MBGL_CABI_API mbgl_runloop_t  mbgl_runloop_create(void);
MBGL_CABI_API void            mbgl_runloop_destroy(mbgl_runloop_t rl);
MBGL_CABI_API void            mbgl_runloop_run_once(mbgl_runloop_t rl);

/* ── Frontend ──────────────────────────────────────────────────────────────── */
MBGL_CABI_API mbgl_frontend_t mbgl_frontend_create_gl(
    void*          surface_handle,
    void*          gl_context,
    int            width_px,
    int            height_px,
    float          pixel_ratio,
    mbgl_render_fn render_callback,
    void*          render_userdata);
MBGL_CABI_API void            mbgl_frontend_destroy(mbgl_frontend_t fe);
MBGL_CABI_API void            mbgl_frontend_render(mbgl_frontend_t fe);
MBGL_CABI_API void            mbgl_frontend_set_size(mbgl_frontend_t fe, int width_px, int height_px);
MBGL_CABI_API void*           mbgl_frontend_get_native_view(mbgl_frontend_t fe);

/* ── Map ───────────────────────────────────────────────────────────────────── */
MBGL_CABI_API mbgl_map_t      mbgl_map_create(
    mbgl_frontend_t       fe,
    mbgl_runloop_t        rl,
    const char*           cache_path,
    const char*           asset_path,
    float                 pixel_ratio,
    mbgl_map_observer_fn  observer,
    void*                 observer_userdata);
MBGL_CABI_API void            mbgl_map_destroy(mbgl_map_t map);

MBGL_CABI_API void            mbgl_map_set_style_url(mbgl_map_t map, const char* url);
MBGL_CABI_API void            mbgl_map_set_style_json(mbgl_map_t map, const char* json);
MBGL_CABI_API void            mbgl_map_set_size(mbgl_map_t map, int width_px, int height_px);

MBGL_CABI_API void            mbgl_map_jump_to(mbgl_map_t map, double lat, double lon,
                                               double zoom, double bearing, double pitch);
MBGL_CABI_API void            mbgl_map_ease_to(mbgl_map_t map, double lat, double lon,
                                               double zoom, double bearing, double pitch,
                                               int64_t duration_ms);

MBGL_CABI_API double          mbgl_map_get_zoom(mbgl_map_t map);
MBGL_CABI_API double          mbgl_map_get_bearing(mbgl_map_t map);
MBGL_CABI_API double          mbgl_map_get_pitch(mbgl_map_t map);
MBGL_CABI_API void            mbgl_map_get_center(mbgl_map_t map, double* out_lat, double* out_lon);

MBGL_CABI_API void            mbgl_map_set_min_zoom(mbgl_map_t map, double zoom);
MBGL_CABI_API void            mbgl_map_set_max_zoom(mbgl_map_t map, double zoom);
MBGL_CABI_API void            mbgl_map_trigger_repaint(mbgl_map_t map);
MBGL_CABI_API void            mbgl_map_cancel_transitions(mbgl_map_t map);
MBGL_CABI_API int             mbgl_map_is_fully_loaded(mbgl_map_t map);

/* ── Map – gesture / interactive movement ──────────────────────────────────── */
/** Inform the map that a user gesture is in progress (suppresses animated
 *  camera snap-back during panning).  Call with 1 on touch-down, 0 on touch-up. */
MBGL_CABI_API void            mbgl_map_set_gesture_in_progress(mbgl_map_t map, int in_progress);
/** Translate the viewport by (dx, dy) screen pixels, optionally animated. */
MBGL_CABI_API void            mbgl_map_move_by(mbgl_map_t map, double dx, double dy,
                                               int64_t duration_ms);
/** Rotate the map by dragging first→second screen coordinate. */
MBGL_CABI_API void            mbgl_map_rotate_by(mbgl_map_t map,
                                                  double x0, double y0,
                                                  double x1, double y1);
/** Pitch the map by delta degrees, optionally animated. */
MBGL_CABI_API void            mbgl_map_pitch_by(mbgl_map_t map, double delta_degrees,
                                                int64_t duration_ms);

/* ── Map – map options (post-create) ────────────────────────────────────────── */
/** 0=None 1=NorthUp 2=Compass 3=Manual */
MBGL_CABI_API void            mbgl_map_set_north_orientation(mbgl_map_t map, int orientation);
/** 0=None 1=HeightOnly 2=WidthAndHeight */
MBGL_CABI_API void            mbgl_map_set_constrain_mode(mbgl_map_t map, int mode);
/** 0=Default 1=FlippedY */
MBGL_CABI_API void            mbgl_map_set_viewport_mode(mbgl_map_t map, int mode);

/* ── Map – camera constraints read-back ────────────────────────────────────── */
/** Read current BoundOptions.  Pass NULL for fields you don't need. */
MBGL_CABI_API void            mbgl_map_get_bounds(mbgl_map_t map,
                                                   double* out_lat_sw, double* out_lon_sw,
                                                   double* out_lat_ne, double* out_lon_ne,
                                                   double* out_min_zoom, double* out_max_zoom,
                                                   double* out_min_pitch, double* out_max_pitch);

/* ── Map – tile LOD controls (Tier 2) ──────────────────────────────────────── */
MBGL_CABI_API void            mbgl_map_set_prefetch_zoom_delta(mbgl_map_t map, int delta);
MBGL_CABI_API int             mbgl_map_get_prefetch_zoom_delta(mbgl_map_t map);
MBGL_CABI_API void            mbgl_map_set_tile_lod_min_radius(mbgl_map_t map, double radius);
MBGL_CABI_API void            mbgl_map_set_tile_lod_scale(mbgl_map_t map, double scale);
MBGL_CABI_API void            mbgl_map_set_tile_lod_pitch_threshold(mbgl_map_t map, double threshold_rad);
MBGL_CABI_API void            mbgl_map_set_tile_lod_zoom_shift(mbgl_map_t map, double shift);
/** mode: 0=default, 1=distance */
MBGL_CABI_API void            mbgl_map_set_tile_lod_mode(mbgl_map_t map, int mode);

/* ── Map – camera for points / geometry (Tier 2) ───────────────────────────── */
/** Compute camera to fit an arbitrary list of lat/lon pairs with padding.
 *  @param latlngs  Flat array of alternating lat, lon values (length = count * 2). */
MBGL_CABI_API void            mbgl_map_camera_for_latlngs(mbgl_map_t map,
                                                            const double* latlngs, int count,
                                                            double pad_top, double pad_left,
                                                            double pad_bottom, double pad_right,
                                                            double* out_lat, double* out_lon,
                                                            double* out_zoom,
                                                            double* out_bearing,
                                                            double* out_pitch);
/** Batch-project N lat/lon pairs to screen pixels.
 *  @param latlngs  Flat array [lat0, lon0, lat1, lon1, ...] length = count * 2.
 *  @param out_xy   Caller-allocated output [x0, y0, x1, y1, ...] length = count * 2. */
MBGL_CABI_API void            mbgl_map_pixels_for_latlngs(mbgl_map_t map,
                                                            const double* latlngs, int count,
                                                            double* out_xy);
/** Batch un-project N screen pixel pairs to lat/lon.
 *  @param xy        Flat array [x0, y0, x1, y1, ...] length = count * 2.
 *  @param out_ll    Caller-allocated output [lat0, lon0, ...] length = count * 2. */
MBGL_CABI_API void            mbgl_map_latlngs_for_pixels(mbgl_map_t map,
                                                            const double* xy, int count,
                                                            double* out_ll);

/* ── Style – enumeration (Tier 1) ──────────────────────────────────────────── */
/** Get style metadata.  Returned strings must be freed with mbgl_free_string(). */
MBGL_CABI_API char*           mbgl_style_get_url(mbgl_style_t st);
MBGL_CABI_API char*           mbgl_style_get_name(mbgl_style_t st);
/** Returns a newline-separated list of source IDs; caller frees with mbgl_free_string(). */
MBGL_CABI_API char*           mbgl_style_get_source_ids(mbgl_style_t st);
/** Returns a newline-separated list of layer IDs in draw order; caller frees. */
MBGL_CABI_API char*           mbgl_style_get_layer_ids(mbgl_style_t st);
/** Get a layer handle by ID (returns NULL if not found). */
MBGL_CABI_API mbgl_layer_t    mbgl_style_get_layer(mbgl_style_t st, const char* layer_id);
/** Get a source handle by ID (returns NULL if not found). */
MBGL_CABI_API mbgl_source_t   mbgl_style_get_source(mbgl_style_t st, const char* source_id);

/* ── Layer – read-back ──────────────────────────────────────────────────────── */
/** Returns a JSON-encoded value or NULL if not set; caller frees. */
MBGL_CABI_API char*           mbgl_layer_get_paint_property(mbgl_layer_t layer, const char* name);
MBGL_CABI_API char*           mbgl_layer_get_layout_property(mbgl_layer_t layer, const char* name);
/** Returns 1 if visible, 0 if none. */
MBGL_CABI_API int             mbgl_layer_get_visibility(mbgl_layer_t layer);(mbgl_map_t map, double delta, double cx, double cy);
MBGL_CABI_API void            mbgl_map_on_double_tap(mbgl_map_t map, double x, double y);
MBGL_CABI_API void            mbgl_map_on_pan_start(mbgl_map_t map, double x, double y);
MBGL_CABI_API void            mbgl_map_on_pan_move(mbgl_map_t map, double dx, double dy);
MBGL_CABI_API void            mbgl_map_on_pan_end(mbgl_map_t map);
MBGL_CABI_API void            mbgl_map_on_pinch(mbgl_map_t map, double scale_factor,
                                                double cx, double cy);

/* ── Map – additional camera / bounds / projection ─────────────────────────── */
MBGL_CABI_API void            mbgl_map_fly_to(mbgl_map_t map, double lat, double lon,
                                              double zoom, double bearing, double pitch,
                                              int64_t duration_ms);

/** Set geographic camera bounds and optional zoom/pitch limits.
 *  Pass NaN for any field to leave it unset (e.g. no lat/lng constraint). */
MBGL_CABI_API void            mbgl_map_set_bounds(mbgl_map_t map,
                                                   double lat_sw, double lon_sw,
                                                   double lat_ne, double lon_ne,
                                                   double min_zoom, double max_zoom,
                                                   double min_pitch, double max_pitch);

/** Compute CameraOptions that fits the given LatLngBounds with optional padding.
 *  Padding order: top, left, bottom, right (matches mbgl::EdgeInsets field order). */
MBGL_CABI_API void            mbgl_map_camera_for_bounds(mbgl_map_t map,
                                                          double lat_sw, double lon_sw,
                                                          double lat_ne, double lon_ne,
                                                          double pad_top, double pad_left,
                                                          double pad_bottom, double pad_right,
                                                          double* out_lat, double* out_lon,
                                                          double* out_zoom, double* out_bearing,
                                                          double* out_pitch);

MBGL_CABI_API void            mbgl_map_pixel_for_latlng(mbgl_map_t map,
                                                         double lat, double lon,
                                                         double* out_x, double* out_y);
MBGL_CABI_API void            mbgl_map_latlng_for_pixel(mbgl_map_t map,
                                                         double x, double y,
                                                         double* out_lat, double* out_lon);

MBGL_CABI_API void            mbgl_map_set_projection_mode(mbgl_map_t map,
                                                            int axonometric,
                                                            double x_skew, double y_skew);

/* ── Style – images ─────────────────────────────────────────────────────────── */
/** Add a sprite image from premultiplied RGBA bytes (length = width * height * 4). */
MBGL_CABI_API void            mbgl_style_add_image(mbgl_style_t st, const char* image_id,
                                                    int width, int height,
                                                    float pixel_ratio, int sdf,
                                                    const uint8_t* rgba_premultiplied);
MBGL_CABI_API void            mbgl_style_remove_image(mbgl_style_t st, const char* image_id);

/** Returns the currently loaded style as a JSON string; caller must free with mbgl_free_string(). */
MBGL_CABI_API char*           mbgl_style_get_json(mbgl_style_t st);

/** Set the global style transition duration and delay (milliseconds). */
MBGL_CABI_API void            mbgl_style_set_transition(mbgl_style_t st,
                                                          int64_t duration_ms,
                                                          int64_t delay_ms);

/** Set a Light property by name using a JSON-encoded value.
 *  Valid names: "anchor" ("map"|"viewport"), "color" ("#rrggbb"),
 *               "intensity" (0-1 float), "position" ([radial, azimuthal, polar]). */
MBGL_CABI_API void            mbgl_style_set_light_property(mbgl_style_t st,
                                                              const char* name,
                                                              const char* value_json);

/* ── Style – additional layer types ─────────────────────────────────────────── */
MBGL_CABI_API mbgl_layer_t    mbgl_style_add_location_indicator_layer(mbgl_style_t st,
                                                                        const char* id,
                                                                        const char* before);
MBGL_CABI_API mbgl_layer_t    mbgl_style_add_color_relief_layer(mbgl_style_t st,
                                                                  const char* id,
                                                                  const char* src,
                                                                  const char* before);

/* ── Feature queries ────────────────────────────────────────────────────────── */
/** Query rendered features at a screen point.
 *  Returns a JSON FeatureCollection string; caller must free with mbgl_free_string().
 *  @param layer_ids  Comma-separated layer IDs to restrict the query, or NULL for all. */
MBGL_CABI_API char*           mbgl_map_query_rendered_features_at_point(mbgl_map_t map,
                                                                          double x, double y,
                                                                          const char* layer_ids);
/** Query rendered features in a screen bounding box. */
MBGL_CABI_API char*           mbgl_map_query_rendered_features_in_box(mbgl_map_t map,
                                                                        double x1, double y1,
                                                                        double x2, double y2,
                                                                        const char* layer_ids);
/** Free a string returned by any mbgl query function. */
MBGL_CABI_API void            mbgl_free_string(char* str);

/* ── Style ─────────────────────────────────────────────────────────────────── */
MBGL_CABI_API mbgl_style_t    mbgl_map_get_style(mbgl_map_t map);

/* ── Sources ───────────────────────────────────────────────────────────────── */
MBGL_CABI_API mbgl_source_t   mbgl_style_add_geojson_source(mbgl_style_t st, const char* source_id);
MBGL_CABI_API mbgl_source_t   mbgl_style_add_geojson_source_url(mbgl_style_t st, const char* source_id, const char* url);
MBGL_CABI_API void            mbgl_geojson_source_set_data(mbgl_source_t src, const char* geojson);
MBGL_CABI_API void            mbgl_geojson_source_set_url(mbgl_source_t src, const char* url);
MBGL_CABI_API mbgl_source_t   mbgl_style_add_vector_source(mbgl_style_t st, const char* source_id, const char* url);
MBGL_CABI_API mbgl_source_t   mbgl_style_add_raster_source(mbgl_style_t st, const char* source_id, const char* url, int tile_size);
MBGL_CABI_API mbgl_source_t   mbgl_style_add_rasterdem_source(mbgl_style_t st, const char* source_id, const char* url, int tile_size);
MBGL_CABI_API mbgl_source_t   mbgl_style_add_image_source(mbgl_style_t st, const char* source_id, const char* url,
                                                          double lat0, double lon0, double lat1, double lon1,
                                                          double lat2, double lon2, double lat3, double lon3);
MBGL_CABI_API void            mbgl_style_remove_source(mbgl_style_t st, const char* source_id);
MBGL_CABI_API int             mbgl_style_has_source(mbgl_style_t st, const char* source_id);

/* ── Layers ────────────────────────────────────────────────────────────────── */
MBGL_CABI_API mbgl_layer_t    mbgl_style_add_fill_layer(mbgl_style_t st, const char* id, const char* src, const char* before);
MBGL_CABI_API mbgl_layer_t    mbgl_style_add_line_layer(mbgl_style_t st, const char* id, const char* src, const char* before);
MBGL_CABI_API mbgl_layer_t    mbgl_style_add_circle_layer(mbgl_style_t st, const char* id, const char* src, const char* before);
MBGL_CABI_API mbgl_layer_t    mbgl_style_add_symbol_layer(mbgl_style_t st, const char* id, const char* src, const char* before);
MBGL_CABI_API mbgl_layer_t    mbgl_style_add_raster_layer(mbgl_style_t st, const char* id, const char* src, const char* before);
MBGL_CABI_API mbgl_layer_t    mbgl_style_add_heatmap_layer(mbgl_style_t st, const char* id, const char* src, const char* before);
MBGL_CABI_API mbgl_layer_t    mbgl_style_add_hillshade_layer(mbgl_style_t st, const char* id, const char* src, const char* before);
MBGL_CABI_API mbgl_layer_t    mbgl_style_add_fill_extrusion_layer(mbgl_style_t st, const char* id, const char* src, const char* before);
MBGL_CABI_API mbgl_layer_t    mbgl_style_add_background_layer(mbgl_style_t st, const char* id, const char* before);
MBGL_CABI_API void            mbgl_style_remove_layer(mbgl_style_t st, const char* layer_id);
MBGL_CABI_API int             mbgl_style_has_layer(mbgl_style_t st, const char* layer_id);

MBGL_CABI_API void            mbgl_layer_set_source_layer(mbgl_layer_t layer, const char* source_layer);
MBGL_CABI_API void            mbgl_layer_set_filter(mbgl_layer_t layer, const char* filter_json);
MBGL_CABI_API void            mbgl_layer_set_min_zoom(mbgl_layer_t layer, float zoom);
MBGL_CABI_API void            mbgl_layer_set_max_zoom(mbgl_layer_t layer, float zoom);
MBGL_CABI_API void            mbgl_layer_set_visibility(mbgl_layer_t layer, int visible);
MBGL_CABI_API void            mbgl_layer_set_paint_property(mbgl_layer_t layer, const char* name, const char* value_json);
MBGL_CABI_API void            mbgl_layer_set_layout_property(mbgl_layer_t layer, const char* name, const char* value_json);

/* ── Version ───────────────────────────────────────────────────────────────── */
MBGL_CABI_API const char*     mbgl_cabi_version(void);

/* ── Android helpers (only compiled on Android) ────────────────────────────── */
#ifdef __ANDROID__
/** Acquire an ANativeWindow from a Java android.view.Surface.
 *  Caller must call mbgl_android_release_window when done. */
MBGL_CABI_API void*  mbgl_android_acquire_window(void* jni_env, void* surface_jobject);
/** Release an ANativeWindow obtained via mbgl_android_acquire_window. */
MBGL_CABI_API void   mbgl_android_release_window(void* window);
#endif

#ifdef __cplusplus
} // extern "C"
#endif
