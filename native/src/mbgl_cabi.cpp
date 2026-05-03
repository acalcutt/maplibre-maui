/**
 * mbgl_cabi.cpp — Implementation of the flat C ABI wrapper.
 *
 * Compiles as a plain C++ shared library (no C++/CLI, no JNI, no ObjC).
 * The platform-specific frontend (rendering surface binding) is handled by
 * PlatformFrontend, which has per-platform .cpp files included via CMake.
 */

#include "mbgl_cabi.h"

#include <mbgl/map/map.hpp>
#include <mbgl/map/map_options.hpp>
#include <mbgl/map/camera.hpp>
#include <mbgl/util/run_loop.hpp>
#include <mbgl/storage/resource_options.hpp>
#include <mbgl/style/style.hpp>
#include <mbgl/style/sources/geojson_source.hpp>
#include <mbgl/style/sources/vector_source.hpp>
#include <mbgl/style/sources/raster_source.hpp>
#include <mbgl/style/sources/raster_dem_source.hpp>
#include <mbgl/style/sources/image_source.hpp>
#include <mbgl/style/layers/fill_layer.hpp>
#include <mbgl/style/layers/line_layer.hpp>
#include <mbgl/style/layers/circle_layer.hpp>
#include <mbgl/style/layers/symbol_layer.hpp>
#include <mbgl/style/layers/raster_layer.hpp>
#include <mbgl/style/layers/heatmap_layer.hpp>
#include <mbgl/style/layers/hillshade_layer.hpp>
#include <mbgl/style/layers/fill_extrusion_layer.hpp>
#include <mbgl/style/layers/background_layer.hpp>
#include <mbgl/style/layers/location_indicator_layer.hpp>
#include <mbgl/style/layers/color_relief_layer.hpp>
#include <mbgl/style/conversion/geojson.hpp>
#include <mbgl/style/conversion/filter.hpp>
#include <mbgl/util/rapidjson.hpp>
#include <mbgl/style/rapidjson_conversion.hpp>
#include <mbgl/map/map_observer.hpp>
#include <mbgl/style/image.hpp>
#include <mbgl/util/image.hpp>
#include <mbgl/renderer/renderer.hpp>
#include <mbgl/util/geojson.hpp>

#include <memory>
#include <string>
#include <stdexcept>
#include <cmath>
#include <sstream>

// Platform frontend is provided separately per platform.
#include "platform_frontend.hpp"

/// Factory function defined in platform_frontend_<platform>.cpp/.mm
extern PlatformFrontend* createPlatformFrontend(
    void*          surface_handle,
    void*          gl_context,
    mbgl::Size     size,
    float          pixel_ratio,
    mbgl_render_fn render_callback,
    void*          render_userdata);

/* ─── Internal structs ──────────────────────────────────────────────────────── */

struct CabiRunLoop {
    mbgl::util::RunLoop loop;
};

struct CabiMap {
    std::unique_ptr<PlatformFrontend>     frontend;
    std::unique_ptr<mbgl::Map>            map;
    mbgl_map_observer_fn                  observer_fn  = nullptr;
    void*                                 observer_ud  = nullptr;
};

/* Helper: safe C-string copy */
static inline std::string safe_str(const char* s) {
    return s ? std::string(s) : std::string{};
}

/* ─── RunLoop ───────────────────────────────────────────────────────────────── */

mbgl_runloop_t mbgl_runloop_create() {
    return new CabiRunLoop{};
}

void mbgl_runloop_destroy(mbgl_runloop_t rl) {
    delete static_cast<CabiRunLoop*>(rl);
}

void mbgl_runloop_run_once(mbgl_runloop_t rl) {
    static_cast<CabiRunLoop*>(rl)->loop.runOnce();
}

/* ─── Frontend ──────────────────────────────────────────────────────────────── */

mbgl_frontend_t mbgl_frontend_create_gl(
    void*  surface_handle,
    void*  gl_context,
    int    width_px,
    int    height_px,
    float  pixel_ratio,
    mbgl_render_fn render_callback,
    void*  render_userdata)
{
    return createPlatformFrontend(
        surface_handle, gl_context,
        mbgl::Size{ static_cast<uint32_t>(width_px), static_cast<uint32_t>(height_px) },
        pixel_ratio,
        render_callback, render_userdata
    );
}

void mbgl_frontend_destroy(mbgl_frontend_t fe) {
    delete static_cast<PlatformFrontend*>(fe);
}

void mbgl_frontend_render(mbgl_frontend_t fe) {
    static_cast<PlatformFrontend*>(fe)->render();
}

void mbgl_frontend_set_size(mbgl_frontend_t fe, int width_px, int height_px) {
    static_cast<PlatformFrontend*>(fe)->setSize(
        mbgl::Size{ static_cast<uint32_t>(width_px), static_cast<uint32_t>(height_px) });
}

void* mbgl_frontend_get_native_view(mbgl_frontend_t fe) {
    return static_cast<PlatformFrontend*>(fe)->getNativeView();
}

/* ─── Map ───────────────────────────────────────────────────────────────────── */

mbgl_map_t mbgl_map_create(
    mbgl_frontend_t  fe,
    mbgl_runloop_t   /*rl*/,
    const char*      cache_path,
    const char*      asset_path,
    float            pixel_ratio,
    mbgl_map_observer_fn observer,
    void*            observer_userdata)
{
    auto* cabi_fe  = static_cast<PlatformFrontend*>(fe);
    auto* cabi_map = new CabiMap{};
    cabi_map->frontend   = std::unique_ptr<PlatformFrontend>(cabi_fe);
    cabi_map->observer_fn = observer;
    cabi_map->observer_ud = observer_userdata;

    mbgl::ResourceOptions resOpts;
    if (cache_path) resOpts.withCachePath(cache_path);
    if (asset_path) resOpts.withAssetPath(asset_path);

    mbgl::MapOptions mapOpts;
    mapOpts.withMapMode(mbgl::MapMode::Continuous)
           .withConstrainMode(mbgl::ConstrainMode::HeightOnly)
           .withViewportMode(mbgl::ViewportMode::Default)
           .withSize(cabi_fe->getSize())
           .withPixelRatio(pixel_ratio);

    cabi_map->map = std::make_unique<mbgl::Map>(
        *cabi_fe,          // RendererFrontend&
        cabi_map->frontend->getObserver(),
        mapOpts,
        resOpts
    );

    return cabi_map;
}

void mbgl_map_destroy(mbgl_map_t map) {
    auto* m = static_cast<CabiMap*>(map);
    m->map.reset();
    // frontend already owned by CabiMap, reset separately so map goes first
    m->frontend.release(); // already destroyed via map
    delete m;
}

void mbgl_map_set_style_url(mbgl_map_t map, const char* url) {
    static_cast<CabiMap*>(map)->map->getStyle().loadURL(safe_str(url));
}

void mbgl_map_set_style_json(mbgl_map_t map, const char* json) {
    static_cast<CabiMap*>(map)->map->getStyle().loadJSON(safe_str(json));
}

void mbgl_map_set_size(mbgl_map_t map, int width_px, int height_px) {
    mbgl::Size sz{ static_cast<uint32_t>(width_px), static_cast<uint32_t>(height_px) };
    auto* m = static_cast<CabiMap*>(map);
    m->map->setSize(sz);
    m->frontend->setSize(sz);
}

void mbgl_map_jump_to(mbgl_map_t map, double lat, double lon, double zoom, double bearing, double pitch) {
    mbgl::CameraOptions cam;
    cam.center  = mbgl::LatLng{ lat, lon };
    cam.zoom    = zoom;
    cam.bearing = bearing;
    cam.pitch   = pitch;
    static_cast<CabiMap*>(map)->map->jumpTo(cam);
}

void mbgl_map_ease_to(mbgl_map_t map, double lat, double lon, double zoom, double bearing, double pitch, int64_t duration_ms) {
    mbgl::CameraOptions cam;
    cam.center  = mbgl::LatLng{ lat, lon };
    cam.zoom    = zoom;
    cam.bearing = bearing;
    cam.pitch   = pitch;
    mbgl::AnimationOptions anim{ mbgl::Duration(std::chrono::milliseconds(duration_ms)) };
    static_cast<CabiMap*>(map)->map->easeTo(cam, anim);
}

double mbgl_map_get_zoom(mbgl_map_t map) {
    return static_cast<CabiMap*>(map)->map->getCameraOptions().zoom.value_or(0.0);
}

double mbgl_map_get_bearing(mbgl_map_t map) {
    return static_cast<CabiMap*>(map)->map->getCameraOptions().bearing.value_or(0.0);
}

double mbgl_map_get_pitch(mbgl_map_t map) {
    return static_cast<CabiMap*>(map)->map->getCameraOptions().pitch.value_or(0.0);
}

void mbgl_map_get_center(mbgl_map_t map, double* out_lat, double* out_lon) {
    auto cam = static_cast<CabiMap*>(map)->map->getCameraOptions();
    if (cam.center) { *out_lat = cam.center->latitude(); *out_lon = cam.center->longitude(); }
    else            { *out_lat = 0.0; *out_lon = 0.0; }
}

void mbgl_map_set_min_zoom(mbgl_map_t map, double zoom) { static_cast<CabiMap*>(map)->map->setBounds(mbgl::BoundOptions{}.withMinZoom(zoom)); }
void mbgl_map_set_max_zoom(mbgl_map_t map, double zoom) { static_cast<CabiMap*>(map)->map->setBounds(mbgl::BoundOptions{}.withMaxZoom(zoom)); }

void mbgl_map_trigger_repaint(mbgl_map_t map) { static_cast<CabiMap*>(map)->map->triggerRepaint(); }

/* Input — MapLibre internal camera manipulation via ScreenCoordinate transform */
void mbgl_map_on_scroll(mbgl_map_t map, double delta, double cx, double cy) {
    auto* m = static_cast<CabiMap*>(map);
    double zoom = m->map->getCameraOptions().zoom.value_or(0.0) + delta / 100.0;
    mbgl::CameraOptions cam;
    cam.zoom   = zoom;
    cam.anchor = mbgl::ScreenCoordinate{ cx, cy };
    m->map->jumpTo(cam);
}

void mbgl_map_on_double_tap(mbgl_map_t map, double x, double y) {
    auto* m = static_cast<CabiMap*>(map);
    double zoom = m->map->getCameraOptions().zoom.value_or(0.0) + 1.0;
    mbgl::CameraOptions cam;
    cam.zoom   = zoom;
    cam.anchor = mbgl::ScreenCoordinate{ x, y };
    mbgl::AnimationOptions anim{ mbgl::Duration(std::chrono::milliseconds(300)) };
    m->map->easeTo(cam, anim);
}

// Pan start/move/end — tracked via static anchor approach
static thread_local mbgl::CameraOptions s_panStart;
static thread_local mbgl::ScreenCoordinate s_panAnchor;

void mbgl_map_on_pan_start(mbgl_map_t map, double x, double y) {
    s_panStart  = static_cast<CabiMap*>(map)->map->getCameraOptions();
    s_panAnchor = { x, y };
}

void mbgl_map_on_pan_move(mbgl_map_t map, double dx, double dy) {
    static_cast<CabiMap*>(map)->map->moveBy(mbgl::ScreenCoordinate{dx, dy});
}

void mbgl_map_on_pan_end(mbgl_map_t /*map*/) {}

void mbgl_map_on_pinch(mbgl_map_t map, double scale_factor, double cx, double cy) {
    auto* m = static_cast<CabiMap*>(map);
    double zoom = m->map->getCameraOptions().zoom.value_or(0.0) + std::log2(scale_factor);
    mbgl::CameraOptions cam;
    cam.zoom   = zoom;
    cam.anchor = mbgl::ScreenCoordinate{ cx, cy };
    m->map->jumpTo(cam);
}

/* ─── Style ─────────────────────────────────────────────────────────────────── */

mbgl_style_t mbgl_map_get_style(mbgl_map_t map) {
    return &static_cast<CabiMap*>(map)->map->getStyle();
}

static mbgl::style::Style& style_ref(mbgl_style_t st) {
    return *static_cast<mbgl::style::Style*>(st);
}

/* ─── Sources ───────────────────────────────────────────────────────────────── */

mbgl_source_t mbgl_style_add_geojson_source(mbgl_style_t st, const char* source_id) {
    auto src = std::make_unique<mbgl::style::GeoJSONSource>(safe_str(source_id));
    auto* raw = src.get();
    style_ref(st).addSource(std::move(src));
    return raw;
}

mbgl_source_t mbgl_style_add_geojson_source_url(mbgl_style_t st, const char* source_id, const char* url) {
    auto src = std::make_unique<mbgl::style::GeoJSONSource>(safe_str(source_id));
    src->setURL(safe_str(url));
    auto* raw = src.get();
    style_ref(st).addSource(std::move(src));
    return raw;
}

void mbgl_geojson_source_set_data(mbgl_source_t src, const char* geojson) {
    auto* gs = static_cast<mbgl::style::GeoJSONSource*>(src);
    mbgl::style::conversion::Error err;
    auto result = mbgl::style::conversion::parseGeoJSON(safe_str(geojson), err);
    if (result) gs->setGeoJSON(*result);
}

void mbgl_geojson_source_set_url(mbgl_source_t src, const char* url) {
    static_cast<mbgl::style::GeoJSONSource*>(src)->setURL(safe_str(url));
}

mbgl_source_t mbgl_style_add_vector_source(mbgl_style_t st, const char* source_id, const char* url) {
    auto src = std::make_unique<mbgl::style::VectorSource>(safe_str(source_id), safe_str(url));
    auto* raw = src.get();
    style_ref(st).addSource(std::move(src));
    return raw;
}

mbgl_source_t mbgl_style_add_raster_source(mbgl_style_t st, const char* source_id, const char* url, int tile_size) {
    auto src = std::make_unique<mbgl::style::RasterSource>(safe_str(source_id), safe_str(url), static_cast<uint16_t>(tile_size));
    auto* raw = src.get();
    style_ref(st).addSource(std::move(src));
    return raw;
}

mbgl_source_t mbgl_style_add_rasterdem_source(mbgl_style_t st, const char* source_id, const char* url, int tile_size) {
    auto src = std::make_unique<mbgl::style::RasterDEMSource>(safe_str(source_id), safe_str(url), static_cast<uint16_t>(tile_size));
    auto* raw = src.get();
    style_ref(st).addSource(std::move(src));
    return raw;
}

mbgl_source_t mbgl_style_add_image_source(mbgl_style_t st, const char* source_id, const char* url,
                                           double lat0, double lon0, double lat1, double lon1,
                                           double lat2, double lon2, double lat3, double lon3)
{
    std::array<mbgl::LatLng, 4> coords{
        mbgl::LatLng{lat0,lon0}, mbgl::LatLng{lat1,lon1},
        mbgl::LatLng{lat2,lon2}, mbgl::LatLng{lat3,lon3}
    };
    auto src = std::make_unique<mbgl::style::ImageSource>(safe_str(source_id), coords);
    src->setURL(safe_str(url));
    auto* raw = src.get();
    style_ref(st).addSource(std::move(src));
    return raw;
}

void mbgl_style_remove_source(mbgl_style_t st, const char* source_id) {
    style_ref(st).removeSource(safe_str(source_id));
}

int mbgl_style_has_source(mbgl_style_t st, const char* source_id) {
    return style_ref(st).getSource(safe_str(source_id)) != nullptr ? 1 : 0;
}

/* ─── Layers ────────────────────────────────────────────────────────────────── */

template<typename LayerT>
static mbgl_layer_t add_layer(mbgl_style_t st, const char* layer_id, const char* source_id, const char* before_id) {
    auto layer = std::make_unique<LayerT>(safe_str(layer_id), source_id ? safe_str(source_id) : "");
    auto* raw = layer.get();
    if (before_id) style_ref(st).addLayer(std::move(layer), safe_str(before_id));
    else           style_ref(st).addLayer(std::move(layer));
    return raw;
}

template<typename LayerT>
static mbgl_layer_t add_layer_no_source(mbgl_style_t st, const char* layer_id, const char* before_id) {
    auto layer = std::make_unique<LayerT>(safe_str(layer_id));
    auto* raw = layer.get();
    if (before_id) style_ref(st).addLayer(std::move(layer), safe_str(before_id));
    else           style_ref(st).addLayer(std::move(layer));
    return raw;
}

mbgl_layer_t mbgl_style_add_fill_layer          (mbgl_style_t st, const char* id, const char* src, const char* before) { return add_layer<mbgl::style::FillLayer>(st,id,src,before); }
mbgl_layer_t mbgl_style_add_line_layer          (mbgl_style_t st, const char* id, const char* src, const char* before) { return add_layer<mbgl::style::LineLayer>(st,id,src,before); }
mbgl_layer_t mbgl_style_add_circle_layer        (mbgl_style_t st, const char* id, const char* src, const char* before) { return add_layer<mbgl::style::CircleLayer>(st,id,src,before); }
mbgl_layer_t mbgl_style_add_symbol_layer        (mbgl_style_t st, const char* id, const char* src, const char* before) { return add_layer<mbgl::style::SymbolLayer>(st,id,src,before); }
mbgl_layer_t mbgl_style_add_raster_layer        (mbgl_style_t st, const char* id, const char* src, const char* before) { return add_layer<mbgl::style::RasterLayer>(st,id,src,before); }
mbgl_layer_t mbgl_style_add_heatmap_layer       (mbgl_style_t st, const char* id, const char* src, const char* before) { return add_layer<mbgl::style::HeatmapLayer>(st,id,src,before); }
mbgl_layer_t mbgl_style_add_hillshade_layer     (mbgl_style_t st, const char* id, const char* src, const char* before) { return add_layer<mbgl::style::HillshadeLayer>(st,id,src,before); }
mbgl_layer_t mbgl_style_add_fill_extrusion_layer(mbgl_style_t st, const char* id, const char* src, const char* before) { return add_layer<mbgl::style::FillExtrusionLayer>(st,id,src,before); }
mbgl_layer_t mbgl_style_add_background_layer    (mbgl_style_t st, const char* id, const char* before) { return add_layer_no_source<mbgl::style::BackgroundLayer>(st,id,before); }
mbgl_layer_t mbgl_style_add_location_indicator_layer(mbgl_style_t st, const char* id, const char* before) { return add_layer_no_source<mbgl::style::LocationIndicatorLayer>(st,id,before); }

void mbgl_style_remove_layer(mbgl_style_t st, const char* layer_id) {
    style_ref(st).removeLayer(safe_str(layer_id));
}

int mbgl_style_has_layer(mbgl_style_t st, const char* layer_id) {
    return style_ref(st).getLayer(safe_str(layer_id)) != nullptr ? 1 : 0;
}

void mbgl_layer_set_source_layer(mbgl_layer_t layer, const char* source_layer) {
    // mbgl::style::Layer base class has setSourceLayer() directly — no dynamic_cast needed.
    static_cast<mbgl::style::Layer*>(layer)->setSourceLayer(safe_str(source_layer));
}

void mbgl_layer_set_filter(mbgl_layer_t layer, const char* filter_json) {
    auto* l = static_cast<mbgl::style::Layer*>(layer);
    mbgl::JSDocument doc;
    doc.Parse(filter_json);
    if (doc.HasParseError()) return;
    mbgl::style::conversion::Error err;
    auto filter = mbgl::style::conversion::convert<mbgl::style::Filter>(doc, err);
    if (filter) l->setFilter(*filter);
}

void mbgl_layer_set_min_zoom(mbgl_layer_t layer, float zoom) {
    static_cast<mbgl::style::Layer*>(layer)->setMinZoom(zoom);
}

void mbgl_layer_set_max_zoom(mbgl_layer_t layer, float zoom) {
    static_cast<mbgl::style::Layer*>(layer)->setMaxZoom(zoom);
}

void mbgl_layer_set_visibility(mbgl_layer_t layer, int visible) {
    static_cast<mbgl::style::Layer*>(layer)->setVisibility(
        visible ? mbgl::style::VisibilityType::Visible : mbgl::style::VisibilityType::None);
}

void mbgl_layer_set_paint_property(mbgl_layer_t layer, const char* name, const char* value_json) {
    auto* l = static_cast<mbgl::style::Layer*>(layer);
    mbgl::JSDocument doc;
    doc.Parse(value_json);
    if (doc.HasParseError()) return;
    const mbgl::JSValue& v = doc;
    l->setProperty(safe_str(name), mbgl::style::conversion::Convertible(&v));
}

void mbgl_layer_set_layout_property(mbgl_layer_t layer, const char* name, const char* value_json) {
    auto* l = static_cast<mbgl::style::Layer*>(layer);
    mbgl::JSDocument doc;
    doc.Parse(value_json);
    if (doc.HasParseError()) return;
    const mbgl::JSValue& v = doc;
    l->setProperty(safe_str(name), mbgl::style::conversion::Convertible(&v));
}

/* ─── Map – additional camera / bounds / projection ─────────────────────────── */

void mbgl_map_fly_to(mbgl_map_t map, double lat, double lon,
                     double zoom, double bearing, double pitch,
                     int64_t duration_ms) {
    mbgl::CameraOptions cam;
    cam.center  = mbgl::LatLng{ lat, lon };
    cam.zoom    = zoom;
    cam.bearing = bearing;
    cam.pitch   = pitch;
    mbgl::AnimationOptions anim{ mbgl::Duration(std::chrono::milliseconds(duration_ms)) };
    static_cast<CabiMap*>(map)->map->flyTo(cam, anim);
}

void mbgl_map_set_bounds(mbgl_map_t map,
                          double lat_sw, double lon_sw,
                          double lat_ne, double lon_ne,
                          double min_zoom, double max_zoom,
                          double min_pitch, double max_pitch) {
    mbgl::BoundOptions opts;
    if (!std::isnan(lat_sw) && !std::isnan(lon_sw) &&
        !std::isnan(lat_ne) && !std::isnan(lon_ne)) {
        opts.withLatLngBounds(mbgl::LatLngBounds::hull(
            mbgl::LatLng{ lat_sw, lon_sw }, mbgl::LatLng{ lat_ne, lon_ne }));
    }
    if (!std::isnan(min_zoom))  opts.withMinZoom(min_zoom);
    if (!std::isnan(max_zoom))  opts.withMaxZoom(max_zoom);
    if (!std::isnan(min_pitch)) opts.withMinPitch(min_pitch);
    if (!std::isnan(max_pitch)) opts.withMaxPitch(max_pitch);
    static_cast<CabiMap*>(map)->map->setBounds(opts);
}

void mbgl_map_camera_for_bounds(mbgl_map_t map,
                                  double lat_sw, double lon_sw,
                                  double lat_ne, double lon_ne,
                                  double pad_top,    double pad_left,
                                  double pad_bottom, double pad_right,
                                  double* out_lat, double* out_lon,
                                  double* out_zoom, double* out_bearing,
                                  double* out_pitch) {
    auto bounds  = mbgl::LatLngBounds::hull(mbgl::LatLng{ lat_sw, lon_sw },
                                            mbgl::LatLng{ lat_ne, lon_ne });
    mbgl::EdgeInsets padding{ pad_top, pad_left, pad_bottom, pad_right };
    auto cam = static_cast<CabiMap*>(map)->map->cameraForLatLngBounds(bounds, padding);
    *out_lat     = cam.center  ? cam.center->latitude()  : 0.0;
    *out_lon     = cam.center  ? cam.center->longitude() : 0.0;
    *out_zoom    = cam.zoom    ? *cam.zoom    : 0.0;
    *out_bearing = cam.bearing ? *cam.bearing : 0.0;
    *out_pitch   = cam.pitch   ? *cam.pitch   : 0.0;
}

void mbgl_map_pixel_for_latlng(mbgl_map_t map, double lat, double lon,
                                double* out_x, double* out_y) {
    auto sc = static_cast<CabiMap*>(map)->map->pixelForLatLng(mbgl::LatLng{ lat, lon });
    *out_x = sc.x;
    *out_y = sc.y;
}

void mbgl_map_latlng_for_pixel(mbgl_map_t map, double x, double y,
                                double* out_lat, double* out_lon) {
    auto ll = static_cast<CabiMap*>(map)->map->latLngForPixel(mbgl::ScreenCoordinate{ x, y });
    *out_lat = ll.latitude();
    *out_lon = ll.longitude();
}

void mbgl_map_set_projection_mode(mbgl_map_t map, int axonometric,
                                   double x_skew, double y_skew) {
    mbgl::ProjectionMode mode;
    mode.axonometric = (axonometric != 0);
    mode.xSkew = x_skew;
    mode.ySkew = y_skew;
    static_cast<CabiMap*>(map)->map->setProjectionMode(mode);
}

/* ─── Style – images ────────────────────────────────────────────────────────── */

void mbgl_style_add_image(mbgl_style_t st, const char* image_id,
                           int width, int height, float pixel_ratio,
                           int sdf, const uint8_t* rgba_premultiplied) {
    mbgl::PremultipliedImage img(
        { static_cast<uint32_t>(width), static_cast<uint32_t>(height) },
        rgba_premultiplied,
        static_cast<size_t>(width) * static_cast<size_t>(height) * 4u);
    style_ref(st).addImage(std::make_unique<mbgl::style::Image>(
        safe_str(image_id), std::move(img), pixel_ratio, sdf != 0));
}

void mbgl_style_remove_image(mbgl_style_t st, const char* image_id) {
    style_ref(st).removeImage(safe_str(image_id));
}

/* ─── Layers – additional types ─────────────────────────────────────────────── */

mbgl_layer_t mbgl_style_add_color_relief_layer(mbgl_style_t st, const char* id,
                                                const char* src, const char* before) {
    return add_layer<mbgl::style::ColorReliefLayer>(st, id, src, before);
}

/* ─── Feature queries ────────────────────────────────────────────────────────── */

static std::vector<std::string> split_layer_ids(const char* csv) {
    std::vector<std::string> result;
    if (!csv || !*csv) return result;
    std::istringstream ss(csv);
    std::string token;
    while (std::getline(ss, token, ',')) {
        if (!token.empty()) result.push_back(std::move(token));
    }
    return result;
}

static char* features_to_json(std::vector<mbgl::Feature>&& features) {
    mbgl::FeatureCollection fc(features.begin(), features.end());
    std::string json = mapbox::geojson::stringify(mbgl::GeoJSON{ fc });
    char* result = new char[json.size() + 1];
    std::copy(json.begin(), json.end(), result);
    result[json.size()] = '\0';
    return result;
}

char* mbgl_map_query_rendered_features_at_point(mbgl_map_t map, double x, double y,
                                                  const char* layer_ids) {
    auto* m        = static_cast<CabiMap*>(map);
    auto* renderer = m->frontend->getRenderer();
    if (!renderer) return nullptr;
    mbgl::RenderedQueryOptions opts;
    auto ids = split_layer_ids(layer_ids);
    if (!ids.empty()) opts.layerIDs = ids;
    auto features = renderer->queryRenderedFeatures(mbgl::ScreenCoordinate{ x, y }, opts);
    return features_to_json(std::move(features));
}

char* mbgl_map_query_rendered_features_in_box(mbgl_map_t map,
                                               double x1, double y1,
                                               double x2, double y2,
                                               const char* layer_ids) {
    auto* m        = static_cast<CabiMap*>(map);
    auto* renderer = m->frontend->getRenderer();
    if (!renderer) return nullptr;
    mbgl::RenderedQueryOptions opts;
    auto ids = split_layer_ids(layer_ids);
    if (!ids.empty()) opts.layerIDs = ids;
    mbgl::ScreenBox box{ { x1, y1 }, { x2, y2 } };
    auto features = renderer->queryRenderedFeatures(box, opts);
    return features_to_json(std::move(features));
}

void mbgl_free_string(char* str) {
    delete[] str;
}

/* ─── Version ───────────────────────────────────────────────────────────────── */
const char* mbgl_cabi_version(void) {
    return "1.0.0";
}

/* ─── Android window helpers ────────────────────────────────────────────────── */
#ifdef __ANDROID__
#include <android/native_window_jni.h>
#include <jni.h>

void* mbgl_android_acquire_window(void* jni_env, void* surface_jobject) {
    return ANativeWindow_fromSurface(
        reinterpret_cast<JNIEnv*>(jni_env),
        reinterpret_cast<jobject>(surface_jobject));
}

void mbgl_android_release_window(void* window) {
    ANativeWindow_release(reinterpret_cast<ANativeWindow*>(window));
}
#endif
