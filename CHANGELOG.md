# Changelog

## master
### ✨ Features and improvements
- _...Add new stuff here..._

### 🐞 Bug fixes
- _...Add new stuff here..._


## 1.0.0

### ✨ Features and improvements

- Unified all platforms (Android, iOS, macCatalyst, Windows) to a single flat C ABI (`mbgl-cabi`) — removes all legacy Xamarin `Org.Maplibre.*` binding dependencies
- Added sample pages: `BasicMapPage`, `GeoJsonLayersPage`, `MarkersPage`
- Android frontend: EGL surface + `ANativeWindow` via JNI, backed by `SurfaceView`
- iOS / macCatalyst frontend: Metal via `MTKView` (owned by C++ backend, inserted as UIKit subview)
- Windows frontend: OpenGL 3.2 core context via `wglCreateContextAttribsARB`, child HWND render target
- Camera: `flyTo`, `easeTo`, `jumpTo` (bearing + pitch), `setBounds`, `cameraForBounds`
- Style: `addImage` / `removeImage` (sprite images), `setProjectionMode` (axonometric)
- Layers: fill, line, circle, symbol, raster, heatmap, hillshade, fill-extrusion, background, location-indicator, color-relief
- Sources: GeoJSON (inline + URL), vector, raster, raster-DEM, image
- Queries: `queryRenderedFeaturesAtPoint`, `queryRenderedFeaturesInBox` (returns GeoJSON FeatureCollection string)
- Projection helpers: `pixelForLatLng`, `latLngForPixel`
- Camera read-back: `GetZoom()`, `GetBearing()`, `GetPitch()`, `GetCenter()`
- Multi-target NuGet packages: `net9.0-android`, `net9.0-ios`, `net9.0-maccatalyst`, `net9.0-windows10.0.19041.0`
- GitHub Actions CI with per-platform native builds (Windows x64/arm64, Android arm64/x86_64, iOS arm64, macCatalyst) and NuGet packaging on `macos-15`
- BSD 2-Clause license (matching maplibre-native)
- maplibre-native pinned as a git submodule

### 🐞 Bug fixes

- Fixed linker errors on Android (`LocalGlyphRasterizer`, `Collator`, `formatNumber`, `Log::platformRecord` stubs; HTTP file source stub; LTO flag alignment)
- Fixed Metal frontend: `MTKView` rewrites, `activate`/`deactivate` on `MetalBackend`, correct `BackendScope` namespace in `drawFrame`
- Fixed `RendererFrontend::getThreadPool()` in all platform frontends; `getSize()` vs `.size` API correction
- Fixed `onDidFinishRenderingFrame` signature; added `metal-cpp` include path and `MetalKit`
- Fixed macCatalyst `UIViewAutoresizing` guard for `TARGET_OS_IPHONE`
- Fixed Android build parallelism cap (4 jobs) to prevent linker OOM
- Fixed Android CI artifact path (wildcard); macOS CI runner upgraded to `macos-15` for Xcode 16 / Mac Catalyst 18.0
- Fixed Windows CI artifact wildcard path; `.NET 9` SDK pinned in `global.json`
- Removed dead `default_file_source.hpp` include; added missing `map_observer.hpp`
- Removed duplicate native cabi headers and unused `nuget.config`
- Removed legacy Xamarin binding projects (`Org.Maplibre.Android`, `Org.Maplibre.MaciOS`)
- Removed `Org.Maplibre` casts from `Maps/Style.cs`, `Maps/Source.cs`, `Maps/Layer.cs`; `NativeMethods` visibility set to `public`
