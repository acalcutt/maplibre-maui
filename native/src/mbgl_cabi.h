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
typedef void (*mbgl_map_observer_fn)(const char* event_name, void* userdata);

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

MBGL_CABI_API void            mbgl_map_on_scroll(mbgl_map_t map, double delta, double cx, double cy);
MBGL_CABI_API void            mbgl_map_on_double_tap(mbgl_map_t map, double x, double y);
MBGL_CABI_API void            mbgl_map_on_pan_start(mbgl_map_t map, double x, double y);
MBGL_CABI_API void            mbgl_map_on_pan_move(mbgl_map_t map, double dx, double dy);
MBGL_CABI_API void            mbgl_map_on_pan_end(mbgl_map_t map);
MBGL_CABI_API void            mbgl_map_on_pinch(mbgl_map_t map, double scale_factor,
                                                double cx, double cy);

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
