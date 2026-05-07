# Changelog

## master
### ✨ Features and improvements
- _...Add new stuff here..._

### 🐞 Bug fixes
- _...Add new stuff here..._

## 1.1.9
### 🐞 Bug fixes
- **Windows: nav and attribution overlays no longer flicker.** `PositionOverlays()` is called on every 16ms render tick; even with `WM_ERASEBKGND` suppressed, calling `SetWindowPos` each tick forces a `WM_PAINT` on the overlay window even when the map hasn’t moved. Fixed by caching the last computed rect for each overlay (`_lastNavRect`, `_lastAttrRect`) and skipping `SetWindowPos` when the position and size are unchanged.- **Windows: attribution overlay now populates correctly.** `onDidFinishLoadingStyle` fires when the style JSON is parsed, but TileJSON sources fetch their metadata asynchronously afterwards. `Source::getAttribution()` is only populated after the TileJSON response arrives. `RefreshAttributionText()` is now also called on `onDidBecomeIdle` when the attribution text is still empty.
- **Windows: nav buttons (zoom in/out, reset north) now work.** `ZoomIn`, `ZoomOut`, and `ResetNorth` were calling `EaseTo` without setting `_renderNeedsUpdate = true`, so the render loop never fired to show the animation. `ResetNorth` now also matches maplibre-gl-js behaviour: if bearing is already ~0°, it resets pitch to 0° as well; otherwise it only snaps bearing north.
## 1.1.8
### ✨ Features and improvements
- **`ShowNavigationControls` now defaults to `false`** (opt-in, consistent with maplibre-gl-js where `NavigationControl` must be explicitly added). Set `ShowNavigationControls="True"` in your `MapLibreMap` to enable the overlay.

### 🐞 Bug fixes
- **Windows: nav overlay now visible on first load.** `CreateOverlays()` called `ShowOverlays()` before `_initialized` was set to `true`, so the visibility guard hid the nav panel immediately after creating it. Fixed by calling `ShowOverlays()` once more after `_initialized = true` at the end of `TryInitialize()`.
- **Windows: touchpad pinch-to-zoom no longer crashes and now zooms correctly.** Two bugs: (1) `ManipulationMode = Scale | TranslateX | TranslateY` triggers an arithmetic overflow inside WinUI 3's manipulation tracker. Fixed by using `ManipulationModes.Scale` only — pan is already handled by the popup HWND's `WM_LBUTTONDOWN`/`WM_MOUSEMOVE` WndProc. (2) `ManipulationDelta.Scale` is an incremental per-frame ratio (e.g. 1.01×), but `mbgl_map_on_pinch` expects a cumulative scale factor from gesture start. Fixed by accumulating into `_pinchCumulativeScale`, reset on `ManipulationStarted`/`ManipulationCompleted`.

## 1.1.7
### ✨ Features and improvements
- **`mbgl_source_get_attribution()`** added to the C ABI: reads `Source::getAttribution()` (populated from TileJSON metadata) and returns a caller-owned string.
- **`MbglStyle.GetSourceAttributions()`** — iterates all loaded style sources and returns unique, non-empty attribution strings. Foundation for OSM-compliant attribution display.
- **`MapLibreMap.ShowNavigationControls`** (default `true`) and **`MapLibreMap.ShowAttributionControl`** (default `true`) bindable properties added, plus **`MapLibreMap.CustomAttribution`** for app-supplied attribution text.
- **Windows: navigation overlay** — two extra `WS_POPUP` HWNDs are created alongside the GL popup after `TryInitialize`. The *nav overlay* (top-right, 29×90 px) paints zoom-in (+), zoom-out (−) and compass/reset-north (↑) buttons using GDI. Clicking calls `EaseTo(zoom±1)` and `EaseTo(bearing:0)`. The *attribution overlay* (bottom-right) is a `WS_EX_LAYERED` popup (92% alpha) that always shows the concatenated TileJSON source attributions as plain text, refreshed on every `StyleLoaded` event. HTML `<a>` tags are stripped to plain text. Both overlays track the GL popup position via `UpdateChildWindowPosition()` and are destroyed safely in `DisposeNative()`. Android/iOS stubs added (`SetShowNavigationControls`/`SetShowAttributionControl` no-ops); full platform implementations planned.

## 1.1.6
### ✨ Features and improvements
- **Windows: mouse pan, scroll-wheel zoom, and double-click zoom now work.** The GL render target is a top-level popup window (the WinUI 3 airspace workaround introduced in 1.1.2), which sits above the XAML compositor and intercepts all Win32 mouse messages before WinUI/MAUI sees them. Previously the MAUI pointer events wired on the placeholder `Grid` never fired over the map area. Fixed by subclassing the popup HWND's `WndProc` via `SetWindowLongPtr(GWLP_WNDPROC)` to handle `WM_LBUTTONDOWN` / `WM_MOUSEMOVE` / `WM_LBUTTONUP` / `WM_MOUSEWHEEL` / `WM_LBUTTONDBLCLK` directly in the controller and forwarding them to the existing `OnPanStart` / `OnPanMove` / `OnPanEnd` / `OnScroll` / `OnDoubleTap` mbgl camera APIs. The original WndProc is restored before `DestroyWindow` on teardown.

## 1.1.5
### 🐞 Bug fixes
- **Fixed regression introduced in 1.1.4: null-pointer crash (`ExecutionEngineException`) on first map load.** `MbglFrontend.TransferOwnership()` previously zeroed the native `Handle`, so subsequent `Render()` / `SetSize()` calls passed `IntPtr.Zero` to the native layer, causing an immediate null-dereference crash. Changed `TransferOwnership()` to set a boolean flag instead; `Handle` remains valid throughout the frontend's lifetime. `Dispose()` uses the flag to skip `mbgl_frontend_destroy` (avoiding the double-free fixed in 1.1.4) while still allowing all normal operations to work.

## 1.1.4
### 🐞 Bug fixes
- **All platforms: fixed heap corruption / `0xc0000374` crash on page navigation (double-free of the frontend native object).** `mbgl_map_create` transfers ownership of the `mbgl_frontend_t*` pointer into the internal `CabiMap` struct, so `mbgl_map_destroy` already destroys the frontend C++ object. The controllers were additionally calling `mbgl_frontend_destroy` on the already-freed pointer, causing a double-free on every normal teardown path. Fixed by adding `MbglFrontend.TransferOwnership()` (zeroes the handle), called from the `MbglMap` constructor immediately after `mbgl_map_create` succeeds. `MbglFrontend.Dispose()` is now a safe no-op after ownership transfer. All three controllers (Windows, Android, MaciOS) updated to not call `_frontend.Dispose()` after `_map.Dispose()`.

## 1.1.3
### 🐞 Bug fixes
- **Windows: crash / heap corruption on page navigation now fixed via `DisconnectHandler` override.** Added `DisconnectHandler(Grid)` to `MapLibreMapHandler` (Windows) that calls `controller.Shutdown()` and unhooks all input events before letting MAUI disconnect the platform view. Previously, MAUI's navigation system could call `DisconnectHandler` in patterns where the WinUI `Unloaded` event fires asynchronously or is skipped (e.g. Shell tab switches), leaving the 16 ms dispatcher timer running and calling into already-freed native GL/mbgl objects.

## 1.1.2
### ✨ Features and improvements
- **Windows: map now renders correctly inside .NET MAUI / WinUI 3 windows.** WinUI 3 composes its XAML content via DirectComposition on top of a `Microsoft.UI.Content.DesktopChildSiteBridge` HWND, which obscures any plain `WS_CHILD` HWND parented inside the window (the well-known "airspace" issue). The Windows controller now creates a borderless top-level popup window (`WS_POPUP | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW`) owned by the main XAML window and tracks its position via `TransformToVisual` + `ClientToScreen` against the discovered XAML bridge HWND. The popup renders above the DComp surface, so the GL content is actually visible.
- **Windows: libuv runloop is now pumped on the UI thread** via a 16 ms `DispatcherTimer`. This unblocks asynchronous HTTP callbacks for style / sprite / glyph / tile downloads — previously requests were issued but their completions never fired, so styles never finished loading.
- **Windows: render scheduling is now coalesced** through the dispatcher tick. `OnRender` only marks the frontend dirty; the render itself (`wglMakeCurrent` → `glBindFramebuffer(0)` → `glViewport` → `glClear` → `frontend.Render()` → `SwapBuffers`) runs on the next tick. This avoids re-entrant rendering during MAUI layout passes.
- **Windows: XAML island HWND is now discovered via `EnumChildWindows`**, looking for class names containing `ContentBridge` or `DesktopChildSiteBridge`. The previous approach via `XamlRoot.ContentIslandEnvironment.AppWindowId` returned the top-level window on current Windows App SDK builds, which produced incorrect screen coordinates.

### 🐞 Bug fixes
- **GL backends: `updateAssumedState()` now resyncs mbgl's GL state cache** by calling `assumeFramebufferBinding(ImplicitFramebufferBinding)` and `assumeViewport(0, 0, size)` — applied to both the Windows (WGL) and Android (EGL) C++ frontends, matching the Apple/Metal frontend in this project and the Qt / GLFW / MaplibreNative.NET-ac WGL backends. Previously these were empty no-ops, so when the surrounding host code mutated framebuffer / viewport state between frames (e.g. clearing the default framebuffer to a background color), mbgl's internal cache thought its bindings were still current and skipped re-binding — producing missing fills, missing labels, and dropped draw calls.
- **All platforms: heap corruption / use-after-free on shutdown** mitigated by tightening teardown order in every controller (`Windows`, `Android`, `MaciOS`): null `_style` → dispose `_map` → drain the libuv runloop several times → dispose `_frontend` → drain the runloop again → dispose `_runLoop`. Previously the frontend/renderer was destroyed while libuv-scheduled tile/glyph completions were still in flight, which on Windows produced `0xc0000374` heap corruption.
- **Windows: `mbgl-cabi.dll` is now copied next to the executable** for `ProjectReference` consumers. The DLL was previously only flowed via NuGet `runtimes/win-x64/native/`, so apps that consumed the handlers/bindings directly via `ProjectReference` (e.g. for local development) failed to load with `0x8007007E`.

## 1.1.1
### 🐞 Bug fixes
- Fixed native DLLs missing from NuGet package: `Pack=true` / `PackagePath` items were inside a TFM-conditioned `ItemGroup` that NuGet silently skips during the outer (multi-targeting) build pass. Moved those declarations to an unconditional `ItemGroup`; `CopyToOutputDirectory` remains TFM-conditioned for local builds. `runtimes/win-x64/native/mbgl-cabi.dll` and `runtimes/win-arm64/native/mbgl-cabi.dll` are now correctly included in `Maui.MapLibre.Native`.

## 1.1.0
### ✨ Features and improvements
- Camera operations exposed on `IMapLibreMapController`: `FlyTo`, `EaseTo`, `JumpTo` (bearing + pitch), `SetCameraTargetBounds` (min/max zoom + pitch)
- Camera read-back: `GetZoom()`, `GetBearing()`, `GetPitch()`, `GetCenter()`
- Projection helpers: `LatLngToScreenPoint`, `ScreenPointToLatLng`
- Queries: `QueryRenderedFeaturesAtPoint`, `QueryRenderedFeaturesInBox` (returns GeoJSON FeatureCollection string)
- Observer wiring fixed — all 14 `MapObserver` virtuals now correctly routed to the C callback via `CabiMapObserver`
- Light API: `MbglStyle.SetLightProperty(name, valueJson)` — anchor, color, intensity, position
- Transition options: `MbglStyle.SetTransition(durationMs, delayMs)`
- `CancelTransitions()` + `IsFullyLoaded` on `MbglMap`
- **Tier 1** — Interactive movement: `SetGestureInProgress`, `MoveBy`, `RotateBy`, `PitchBy` (all with optional animation duration)
- **Tier 1** — Map option post-create setters: `SetNorthOrientation`, `SetConstrainMode`, `SetViewportMode`
- **Tier 1** — Bounds read-back: `GetBounds()` → `BoundOptions` record (lat/lng box + zoom/pitch limits, NaN for unset)
- **Tier 1** — Style enumeration: `GetUrl()`, `GetName()`, `GetSourceIds()`, `GetLayerIds()`, `GetLayer(id)`, `GetSource(id)`
- **Tier 1** — Layer read-back: `GetPaintProperty(name)`, `GetLayoutProperty(name)`, `GetVisibility()`
- **Tier 2** — Tile LOD controls: `SetTileLodMinRadius`, `SetTileLodScale`, `SetTileLodPitchThreshold`, `SetTileLodZoomShift`, `SetTileLodMode` (0=Default, 1=Distance)
- **Tier 2** — Tile prefetch: `SetPrefetchZoomDelta` / `GetPrefetchZoomDelta`
- **Tier 2** — Camera fit to point set: `CameraForLatLngs(points, padding)` → `CameraResult`
- **Tier 2** — Batch projection: `PixelsForLatLngs(points)`, `LatLngsForPixels(pixels)`
- All new APIs surface on `IMapLibreMapController` and are implemented in all three platform controllers (Android, iOS/macCatalyst, Windows)

### 🐞 Bug fixes
- Memory leak in `mbgl_map_destroy`: `frontend.release()` → `frontend.reset()`

## 1.0.0

### ✨ Features and improvements

- Unified all platforms (Android, iOS, macCatalyst, Windows) to a single flat C ABI (`mbgl-cabi`) — removes all legacy Xamarin `Org.Maplibre.*` binding dependencies
- Added sample pages: `BasicMapPage`, `GeoJsonLayersPage`, `MarkersPage`
- Android frontend: EGL surface + `ANativeWindow` via JNI, backed by `SurfaceView`
- iOS / macCatalyst frontend: Metal via `MTKView` (owned by C++ backend, inserted as UIKit subview)
- Windows frontend: OpenGL 3.2 core context via `wglCreateContextAttribsARB`, child HWND render target
- Camera: `flyTo`, `easeTo`, `jumpTo` (bearing + pitch), `setBounds`, `cameraForBounds` (C ABI level)
- Style: `addImage` / `removeImage` (sprite images), `setProjectionMode` (axonometric)
- Layers: fill, line, circle, symbol, raster, heatmap, hillshade, fill-extrusion, background, location-indicator, color-relief
- Sources: GeoJSON (inline + URL), vector, raster, raster-DEM, image
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
