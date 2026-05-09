# Upstream FFI Exploration (`ffi-upstream` branch)

## Goal

Evaluate replacing the hand-written `mbgl-cabi` native layer with the official
[maplibre-native-ffi](https://github.com/maplibre/maplibre-native-ffi) C API as
the foundation for maplibre-maui-ac.

---

## Repository Layout (reference)

```
c:\Users\Andrew\Documents\GitHub\
  maplibre-maui-ac/          ← this repo  (branch: ffi-upstream)
  maplibre-native-ffi/       ← official upstream C API project (sibling)
```

`maplibre-native-ffi` lives as a sibling repo. If we adopt it, the most natural
integration is either:
1. **Git submodule** under `dependencies/maplibre-native-ffi/`
2. **Pre-built native package** consumed as a NuGet native asset

---

## API Comparison

### Handle / object model

| Concept | mbgl-cabi (current) | maplibre-native-ffi (upstream) |
|---|---|---|
| Event loop / context | `mbgl_runloop_t*` | `mln_runtime*` |
| Render target | `mbgl_frontend_t*` (OpenGL) | `mln_render_session*` (Metal/Vulkan) |
| Map | `mbgl_map_t*` | `mln_map*` |
| Style | `mbgl_style_t*` | (no separate handle — style ops on `mln_map*`) |
| Status enum | `mbgl_status_t` | `mln_status` |
| Prefix | `mbgl_` | `mln_` |

### Creation flow

**Current (`mbgl-cabi`)**
```c
mbgl_runloop_t* rl  = mbgl_runloop_create();
mbgl_frontend_t* fe = mbgl_frontend_create_gl(surface, ctx, w, h, dpr, cb, ud);
mbgl_map_t* map     = mbgl_map_create(fe, rl, cache, asset, dpr, observer, ud);
// fe ownership transfers into map
```

**Upstream (`mln`)**
```c
mln_runtime_options opts = mln_runtime_options_default();
mln_runtime* rt = NULL;
mln_runtime_create(&opts, &rt);

mln_map_options mopts = mln_map_options_default();
mln_map* map = NULL;
mln_map_create(rt, &mopts, &map);

// Attach render target (Metal example):
mln_metal_surface_descriptor desc = mln_metal_surface_descriptor_default();
desc.layer  = caMetalLayer;
desc.width  = w; desc.height = h; desc.scale_factor = dpr;
mln_render_session* session = NULL;
mln_map_attach_metal_surface_session(map, &desc, &session);
```

### Event / callback model

| | mbgl-cabi | mln |
|---|---|---|
| Map events | Push callback (`mbgl_map_observer_fn`) | Poll: `mln_runtime_poll_event(&event)` |
| Render ready | Push callback (`mbgl_render_fn`) | `MLN_RUNTIME_EVENT_MAP_RENDER_UPDATE_AVAILABLE` |
| Log | Push callback (`mbgl_log_fn`) | Push callback (unchanged concept) |

The polling model requires a driver loop on the owner thread:
```csharp
while (mln_runtime_poll_event(rt, out var ev) == MlnStatus.Ok)
    HandleEvent(ev);
```

### Render backends

| Platform | mbgl-cabi (current) | mln (upstream) |
|---|---|---|
| iOS / macCatalyst | Metal (MTKView) | Metal ✓ |
| Android | OpenGL ES / EGL | **Vulkan only** (no EGL today) |
| Windows | OpenGL / WGL | **Vulkan only** (no WGL today) |

> **Critical gap**: `maplibre-native-ffi` currently has no OpenGL backend —
> only Metal and Vulkan. Android and Windows support in this project depends on
> EGL/WGL OpenGL, so those platforms would need Vulkan support added on the
> native side, or we wait for the upstream to add OpenGL texture/surface
> descriptors.

---

## What Stays the Same

- The MAUI handler layer (`Maui.Maplibre.Handlers`) does not change at all.
  It talks to the `Maui.MapLibre.Native` C# wrappers, not directly to C.
- The `Maui.MapLibre.Native` project is the only thing that needs to change:
  `NativeMethods.cs`, `MbglMap.cs`, `MbglFrontend.cs`, `MbglRunLoop.cs`.
- Style operations (`SetStyleUrl`, `SetStyleJson`, source/layer manipulation)
  map 1:1 to `mln_map_set_style_*` equivalents.
- Camera operations (`JumpTo`, `EaseTo`, `FlyTo`, `SetBounds`) all exist on
  `mln_map` with richer field-mask options structs.

---

## What Needs to Change

### `bindings/` project

| File | Change |
|---|---|
| `NativeMethods.cs` | Replace `mbgl_*` P/Invokes with `mln_*` P/Invokes from `maplibre_native_c.h`. Use source-generated P/Invokes (`LibraryImport`). |
| `MbglRunLoop.cs` → `MlnRuntime.cs` | Wraps `mln_runtime*`. Add `PollEvents()` loop. |
| `MbglFrontend.cs` → removed | Frontend is now `mln_render_session*`, created via `mln_map_attach_*_session()`. |
| `MbglMap.cs` → `MlnMap.cs` | Use `mln_map_create(runtime, options, &map)`. Attach session separately. |
| `MbglStyle.cs` | Merge style methods into `MlnMap` — no separate style handle in upstream. |

### `native/` project (C++ layer)

The entire `mbgl-cabi` native C++ project would be **replaced** by
`maplibre-native-ffi` as a dependency. We would no longer maintain our own C
wrapper; instead we build/consume the upstream library.

### Native library name

The P/Invoke `DllImport` name changes from `"mbgl-cabi"` to `"maplibre_native_c"`
(or whatever the upstream publishes as the shared library name).

---

## Suggested Integration Strategy

### Phase 1 — iOS/macCatalyst only (Metal, fully supported upstream)

1. Add `maplibre-native-ffi` as a git submodule at
   `dependencies/maplibre-native-ffi/`.
2. Build it for iOS/macCatalyst in the Apple CI job (it already builds
   MapLibre Native as its own submodule — reconcile with our existing
   `dependencies/maplibre-native`).
3. Replace `NativeMethods.cs` with the `mln_*` sketch (see
   `bindings/NativeMethods.Mln.cs`).
4. Replace `MbglMap.cs` / `MbglRunLoop.cs` with `MlnMap.cs` / `MlnRuntime.cs`.
5. Keep the old `mbgl-cabi` code paths behind `#if !USE_MLN_FFI` until Android
   and Windows are ready.

### Phase 2 — Android and Windows (Vulkan path)

- Android: `maplibre-native-ffi` requires Vulkan. Most modern Android devices
  support Vulkan 1.1+. The surface descriptor needs a `VkSurfaceKHR` created
  from an `ANativeWindow` via `vkCreateAndroidSurfaceKHR`.
- Windows: Vulkan via `vkCreateWin32SurfaceKHR`. The MAUI handler's
  `HwndSource` (or `SwapChainPanel`) provides the `HWND`.

### Phase 3 — Remove mbgl-cabi

Once all three platforms are on `mln_*`, delete the `native/` C++ project and
pull `maplibre-native-ffi` as the sole native dependency.

---

## Open Questions

1. **ABI stability**: `maplibre-native-ffi` is pre-1.0 (`mln_c_version()` returns 0).
   The ABI can change between releases. We need to pin to a commit/tag.
2. **Duplicate MapLibre Native submodule**: both repos bring in
   `maplibre-native` as a submodule. We need to reconcile which version is used.
3. **ClangSharp generation**: the C# binding convention document recommends
   generating the internal P/Invoke layer with ClangSharp. We could integrate
   that into the CI pipeline.
4. **net10.0 target**: the upstream C# convention targets `net10.0`, but our
   MAUI project targets `net9.0-android` etc. We should stay on `net9.0` for
   now and revisit when MAUI ships on .NET 10.
5. **Thread model**: `mln_runtime` is 1:1 with its owner thread (same as our
   RunLoop). The polling event loop fits naturally into a dedicated render
   thread.

---

## Files Added in This Branch

- `UPSTREAM_FFI_EXPLORATION.md` — this document
- `bindings/NativeMethods.Mln.cs` — sketch of P/Invoke layer for `mln_*` API
- `bindings/MlnRuntime.cs` — sketch of `RuntimeHandle` wrapper
- `bindings/MlnMap.cs` — sketch of `MapHandle` wrapper
