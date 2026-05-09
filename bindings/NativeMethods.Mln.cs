/**
 * NativeMethods.Mln.cs — Sketch P/Invoke layer for maplibre-native-ffi.
 *
 * This file is a sketch/exploration for the ffi-upstream branch. It mirrors the
 * public C headers from maplibre-native-ffi (include/maplibre_native_c/*.h) as
 * source-generated LibraryImport P/Invokes, following the conventions in
 * docs/src/content/docs/development/bindings-csharp.md.
 *
 * Key differences from NativeMethods.cs (mbgl-cabi):
 *   - Library name:  "maplibre_native_c"  (vs "mbgl-cabi")
 *   - All names use mln_ prefix            (vs mbgl_)
 *   - mln_runtime replaces RunLoop + Frontend as the top-level context
 *   - mln_render_session replaces Frontend  (attach to map, not created first)
 *   - Events are polled (MlnRuntime.PollEvent) instead of pushed via callbacks
 *   - Camera/animation use field-mask option structs instead of flat args
 *   - Runtime target: net10.0 upstream convention; we stay on net9.0 for now
 *
 * TODO: Replace with ClangSharp-generated output once the CI pipeline is set up.
 */
using System.Runtime.InteropServices;

namespace Maui.MapLibre.Native.Upstream;

// ── Status ────────────────────────────────────────────────────────────────────

public enum MlnStatus : int
{
    Ok              =  0,
    InvalidArgument = -1,
    InvalidState    = -2,
    WrongThread     = -3,
    Unsupported     = -4,
    NativeError     = -5,
}

// ── Enums ─────────────────────────────────────────────────────────────────────

public enum MlnLogLevel : int
{
    Debug   = 0,
    Info    = 1,
    Warning = 2,
    Error   = 3,
}

[Flags]
public enum MlnMapDebugOption : uint
{
    TileBorders = 1u << 1,
    ParseStatus = 1u << 2,
    Timestamps  = 1u << 3,
    Collision   = 1u << 4,
    Overdraw    = 1u << 5,
    StencilClip = 1u << 6,
    DepthBuffer = 1u << 7,
}

public enum MlnNorthOrientation : uint
{
    Up    = 0,
    Right = 1,
    Down  = 2,
    Left  = 3,
}

public enum MlnMapMode : uint
{
    Continuous = 0,
    Static     = 1,
    Tile       = 2,
}

public enum MlnRuntimeEventType : uint
{
    MapCameraWillChange       = 1,
    MapCameraIsChanging       = 2,
    MapCameraDidChange        = 3,
    MapStyleLoaded            = 4,
    MapLoadingStarted         = 5,
    MapLoadingFinished        = 6,
    MapLoadingFailed          = 7,
    MapIdle                   = 8,
    MapRenderUpdateAvailable  = 9,
    MapRenderError            = 10,
    MapStillImageFinished     = 11,
    MapStillImageFailed       = 12,
    MapRenderFrameStarted     = 13,
    MapRenderFrameFinished    = 14,
    MapRenderMapStarted       = 15,
    MapRenderMapFinished      = 16,
    MapStyleImageMissing      = 17,
    MapTileAction             = 18,
    OfflineRegionStatusChanged     = 19,
    OfflineRegionResponseError     = 20,
    OfflineRegionTileCountLimitExceeded = 21,
}

// ── Structs ───────────────────────────────────────────────────────────────────

/// <summary>Corresponds to mln_lat_lng.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct MlnLatLng
{
    public double Latitude;
    public double Longitude;
}

/// <summary>Corresponds to mln_screen_point.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct MlnScreenPoint
{
    public double X;
    public double Y;
}

/// <summary>Corresponds to mln_edge_insets.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct MlnEdgeInsets
{
    public double Top;
    public double Left;
    public double Bottom;
    public double Right;
}

/// <summary>
/// Corresponds to mln_camera_options. Use <see cref="MlnMethods.CameraOptionsDefault"/>
/// to initialise (sets the correct <see cref="Size"/> for ABI compatibility).
/// Only fields whose bits are set in <see cref="Fields"/> affect the map.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct MlnCameraOptions
{
    public uint          Size;
    public uint          Fields;       // MlnCameraOptionField bits
    public double        Latitude;
    public double        Longitude;
    public double        CenterAltitude;
    public MlnEdgeInsets Padding;
    public MlnScreenPoint Anchor;
    public double        Zoom;
    public double        Bearing;
    public double        Pitch;
    public double        Roll;
    public double        FieldOfView;
}

/// <summary>Corresponds to mln_unit_bezier.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct MlnUnitBezier
{
    public double X1, Y1, X2, Y2;
}

/// <summary>
/// Corresponds to mln_animation_options. Use <see cref="MlnMethods.AnimationOptionsDefault"/>
/// to initialise.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct MlnAnimationOptions
{
    public uint          Size;
    public uint          Fields;       // MlnAnimationOptionField bits
    public double        DurationMs;
    public double        Velocity;
    public double        MinZoom;
    public MlnUnitBezier Easing;
}

/// <summary>Corresponds to mln_camera_fit_options.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct MlnCameraFitOptions
{
    public uint          Size;
    public uint          Fields;
    public MlnEdgeInsets Padding;
    public double        Bearing;
    public double        Pitch;
}

/// <summary>Corresponds to mln_lat_lng_bounds.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct MlnLatLngBounds
{
    public MlnLatLng Sw;
    public MlnLatLng Ne;
}

/// <summary>Corresponds to mln_bound_options.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct MlnBoundOptions
{
    public uint          Size;
    public uint          Fields;
    public MlnLatLngBounds Bounds;
    public double        MinZoom;
    public double        MaxZoom;
    public double        MinPitch;
    public double        MaxPitch;
}

/// <summary>Corresponds to mln_runtime_options.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct MlnRuntimeOptions
{
    public uint   Size;
    public uint   Flags;
    // NOTE: these are const char* in C; passed as IntPtr for manual marshalling
    public IntPtr AssetPath;    // UTF-8, optional
    public IntPtr CachePath;    // UTF-8, optional
    public ulong  MaximumCacheSize;
}

/// <summary>Corresponds to mln_map_options.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct MlnMapOptions
{
    public uint Size;
    public uint Fields;
    public uint Mode;            // MlnMapMode
    public uint NorthOrientation; // MlnNorthOrientation
    public uint ConstrainMode;
    public uint ViewportMode;
}

/// <summary>Minimal runtime event envelope (source_type + event_type + map handle).</summary>
[StructLayout(LayoutKind.Sequential)]
public struct MlnRuntimeEvent
{
    public uint   Size;
    public uint   SourceType;     // MlnRuntimeEventSourceType
    public uint   EventType;      // MlnRuntimeEventType
    public uint   PayloadType;    // MlnRuntimeEventPayloadType
    public IntPtr MapHandle;      // mln_map* — which map fired the event
    // payload union follows — access via specific poll overloads or unsafe cast
}

// ── Metal surface descriptor (iOS / macCatalyst) ───────────────────────────

/// <summary>Corresponds to mln_metal_surface_descriptor.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct MlnMetalSurfaceDescriptor
{
    public uint   Size;
    public uint   Width;
    public uint   Height;
    public double ScaleFactor;
    public IntPtr Layer;          // CAMetalLayer*
    public IntPtr Device;         // id<MTLDevice>, optional
}

// ── Vulkan surface descriptor (Android / Windows) ─────────────────────────

/// <summary>Corresponds to mln_vulkan_surface_descriptor.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct MlnVulkanSurfaceDescriptor
{
    public uint   Size;
    public uint   Width;
    public uint   Height;
    public double ScaleFactor;
    public IntPtr Instance;
    public IntPtr PhysicalDevice;
    public IntPtr Device;
    public IntPtr GraphicsQueue;
    public uint   GraphicsQueueFamilyIndex;
    public IntPtr Surface;           // VkSurfaceKHR
}

// ── P/Invoke declarations ─────────────────────────────────────────────────────

/// <summary>Raw P/Invoke declarations for maplibre-native-ffi. Prefer the typed
/// handle wrappers (<see cref="MlnRuntime"/>, <see cref="MlnMap"/>).</summary>
public static partial class MlnMethods
{
#if IOS || MACCATALYST
    private const string Lib = "__Internal";
#elif ANDROID
    private const string Lib = "maplibre_native_c";
#else
    private const string Lib = "maplibre_native_c";
#endif

    // ── Diagnostics ───────────────────────────────────────────────────────────
    [LibraryImport(Lib, EntryPoint = "mln_thread_last_error_message")]
    [return: MarshalAs(UnmanagedType.LPUTF8Str)]
    public static partial string ThreadLastErrorMessage();

    // ── Runtime ───────────────────────────────────────────────────────────────
    [LibraryImport(Lib, EntryPoint = "mln_runtime_options_default")]
    public static partial MlnRuntimeOptions RuntimeOptionsDefault();

    [LibraryImport(Lib, EntryPoint = "mln_runtime_create")]
    public static unsafe partial MlnStatus RuntimeCreate(
        MlnRuntimeOptions* options, out IntPtr outRuntime);

    [LibraryImport(Lib, EntryPoint = "mln_runtime_destroy")]
    public static partial MlnStatus RuntimeDestroy(IntPtr runtime);

    [LibraryImport(Lib, EntryPoint = "mln_runtime_pump")]
    public static partial MlnStatus RuntimePump(IntPtr runtime);

    [LibraryImport(Lib, EntryPoint = "mln_runtime_poll_event")]
    public static unsafe partial MlnStatus RuntimePollEvent(
        IntPtr runtime, MlnRuntimeEvent* outEvent);

    // ── Map ───────────────────────────────────────────────────────────────────
    [LibraryImport(Lib, EntryPoint = "mln_map_options_default")]
    public static partial MlnMapOptions MapOptionsDefault();

    [LibraryImport(Lib, EntryPoint = "mln_map_create")]
    public static unsafe partial MlnStatus MapCreate(
        IntPtr runtime, MlnMapOptions* options, out IntPtr outMap);

    [LibraryImport(Lib, EntryPoint = "mln_map_destroy")]
    public static partial MlnStatus MapDestroy(IntPtr map);

    [LibraryImport(Lib, EntryPoint = "mln_map_request_repaint")]
    public static partial MlnStatus MapRequestRepaint(IntPtr map);

    // ── Style ─────────────────────────────────────────────────────────────────
    [LibraryImport(Lib, EntryPoint = "mln_map_set_style_url",
        StringMarshalling = StringMarshalling.Utf8)]
    public static partial MlnStatus MapSetStyleUrl(IntPtr map, string url);

    [LibraryImport(Lib, EntryPoint = "mln_map_set_style_json",
        StringMarshalling = StringMarshalling.Utf8)]
    public static partial MlnStatus MapSetStyleJson(IntPtr map, string json);

    // ── Camera ────────────────────────────────────────────────────────────────
    [LibraryImport(Lib, EntryPoint = "mln_camera_options_default")]
    public static partial MlnCameraOptions CameraOptionsDefault();

    [LibraryImport(Lib, EntryPoint = "mln_animation_options_default")]
    public static partial MlnAnimationOptions AnimationOptionsDefault();

    [LibraryImport(Lib, EntryPoint = "mln_camera_fit_options_default")]
    public static partial MlnCameraFitOptions CameraFitOptionsDefault();

    [LibraryImport(Lib, EntryPoint = "mln_bound_options_default")]
    public static partial MlnBoundOptions BoundOptionsDefault();

    [LibraryImport(Lib, EntryPoint = "mln_map_get_camera")]
    public static unsafe partial MlnStatus MapGetCamera(
        IntPtr map, MlnCameraOptions* outCamera);

    [LibraryImport(Lib, EntryPoint = "mln_map_jump_to")]
    public static unsafe partial MlnStatus MapJumpTo(
        IntPtr map, MlnCameraOptions* camera);

    [LibraryImport(Lib, EntryPoint = "mln_map_ease_to")]
    public static unsafe partial MlnStatus MapEaseTo(
        IntPtr map, MlnCameraOptions* camera, MlnAnimationOptions* animation);

    [LibraryImport(Lib, EntryPoint = "mln_map_fly_to")]
    public static unsafe partial MlnStatus MapFlyTo(
        IntPtr map, MlnCameraOptions* camera, MlnAnimationOptions* animation);

    [LibraryImport(Lib, EntryPoint = "mln_map_cancel_transitions")]
    public static partial MlnStatus MapCancelTransitions(IntPtr map);

    [LibraryImport(Lib, EntryPoint = "mln_map_camera_for_lat_lng_bounds")]
    public static unsafe partial MlnStatus MapCameraForLatLngBounds(
        IntPtr map, MlnLatLngBounds* bounds,
        MlnCameraFitOptions* fitOptions, MlnCameraOptions* outCamera);

    [LibraryImport(Lib, EntryPoint = "mln_map_set_bound_options")]
    public static unsafe partial MlnStatus MapSetBoundOptions(
        IntPtr map, MlnBoundOptions* options);

    [LibraryImport(Lib, EntryPoint = "mln_map_set_debug_options")]
    public static partial MlnStatus MapSetDebugOptions(IntPtr map, uint options);

    // ── Render session (Metal) ─────────────────────────────────────────────
    [LibraryImport(Lib, EntryPoint = "mln_metal_surface_descriptor_default")]
    public static partial MlnMetalSurfaceDescriptor MetalSurfaceDescriptorDefault();

    [LibraryImport(Lib, EntryPoint = "mln_map_attach_metal_surface_session")]
    public static unsafe partial MlnStatus MapAttachMetalSurfaceSession(
        IntPtr map, MlnMetalSurfaceDescriptor* descriptor, out IntPtr outSession);

    [LibraryImport(Lib, EntryPoint = "mln_render_session_resize")]
    public static partial MlnStatus RenderSessionResize(
        IntPtr session, uint width, uint height, double scaleFactor);

    [LibraryImport(Lib, EntryPoint = "mln_render_session_render_update")]
    public static partial MlnStatus RenderSessionRenderUpdate(IntPtr session);

    [LibraryImport(Lib, EntryPoint = "mln_render_session_detach")]
    public static partial MlnStatus RenderSessionDetach(IntPtr session);

    [LibraryImport(Lib, EntryPoint = "mln_render_session_destroy")]
    public static partial MlnStatus RenderSessionDestroy(IntPtr session);

    // ── Render session (Vulkan) ────────────────────────────────────────────
    [LibraryImport(Lib, EntryPoint = "mln_vulkan_surface_descriptor_default")]
    public static partial MlnVulkanSurfaceDescriptor VulkanSurfaceDescriptorDefault();

    [LibraryImport(Lib, EntryPoint = "mln_map_attach_vulkan_surface_session")]
    public static unsafe partial MlnStatus MapAttachVulkanSurfaceSession(
        IntPtr map, MlnVulkanSurfaceDescriptor* descriptor, out IntPtr outSession);

    // ── GeoJSON sources ───────────────────────────────────────────────────────
    [LibraryImport(Lib, EntryPoint = "mln_map_add_geojson_source_data",
        StringMarshalling = StringMarshalling.Utf8)]
    public static partial MlnStatus MapAddGeoJsonSourceData(
        IntPtr map, string sourceId, string geoJson);

    [LibraryImport(Lib, EntryPoint = "mln_map_set_geojson_source_data",
        StringMarshalling = StringMarshalling.Utf8)]
    public static partial MlnStatus MapSetGeoJsonSourceData(
        IntPtr map, string sourceId, string geoJson);

    [LibraryImport(Lib, EntryPoint = "mln_map_remove_style_source",
        StringMarshalling = StringMarshalling.Utf8)]
    public static partial MlnStatus MapRemoveStyleSource(
        IntPtr map, string sourceId);

    // ── Layers ────────────────────────────────────────────────────────────────
    [LibraryImport(Lib, EntryPoint = "mln_map_add_style_layer_json",
        StringMarshalling = StringMarshalling.Utf8)]
    public static partial MlnStatus MapAddStyleLayerJson(
        IntPtr map, string layerJson, string? beforeLayerId);

    [LibraryImport(Lib, EntryPoint = "mln_map_remove_style_layer",
        StringMarshalling = StringMarshalling.Utf8)]
    public static partial MlnStatus MapRemoveStyleLayer(
        IntPtr map, string layerId);

    [LibraryImport(Lib, EntryPoint = "mln_map_set_style_layer_property",
        StringMarshalling = StringMarshalling.Utf8)]
    public static partial MlnStatus MapSetStyleLayerProperty(
        IntPtr map, string layerId, string property, string valueJson);

    // ── Logging ───────────────────────────────────────────────────────────────
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int LogFn(
        MlnLogLevel level,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string category,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string message,
        IntPtr userdata);

    [LibraryImport(Lib, EntryPoint = "mln_set_log_callback")]
    public static partial MlnStatus SetLogCallback(LogFn? fn, IntPtr userdata);

    // ── Helpers ───────────────────────────────────────────────────────────────
    public static void ThrowIfFailed(MlnStatus status, string context = "")
    {
        if (status == MlnStatus.Ok) return;
        var msg = ThreadLastErrorMessage();
        throw new InvalidOperationException(
            $"mln call failed ({status}) in {context}: {msg}");
    }
}
