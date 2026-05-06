#if WINDOWS
using System.Runtime.InteropServices;
using System.Text.Json;
using Maui.MapLibre.Native;
using Maui.MapLibre.Handlers.Geometry;
using Map = Maui.MapLibre.Handlers.Maps.Map;
using Style = Maui.MapLibre.Handlers.Maps.Style;
using Location = Microsoft.Maui.Devices.Sensors.Location;

namespace Maui.MapLibre.Handlers;

/// <summary>
/// Windows-specific IMapLibreMapController implementation backed by the C ABI
/// mbgl-cabi.dll via MbglMap / MbglFrontend / MbglRunLoop P/Invoke bindings.
/// </summary>
public class MapLibreMapController : IMapLibreMapController
{
    // ── Win32 interop ─────────────────────────────────────────────────────────

    [DllImport("user32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
    private static extern ushort RegisterClassExA(ref WNDCLASSEXA wc);

    [DllImport("user32.dll")]
    private static extern IntPtr DefWindowProcA(IntPtr hWnd, uint msg, IntPtr w, IntPtr l);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetModuleHandleW(IntPtr zero);

    [DllImport("user32.dll")]
    private static extern IntPtr CreateWindowExA(
        uint dwExStyle, string lpClassName, string lpWindowName, uint dwStyle,
        int x, int y, int nWidth, int nHeight,
        IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

    [DllImport("user32.dll")]
    private static extern bool DestroyWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr after,
        int x, int y, int cx, int cy, uint flags);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    [DllImport("user32.dll")]
    private static extern IntPtr GetParent(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ScreenToClient(IntPtr hWnd, ref Windows.Foundation.Point pt);

    [DllImport("user32.dll")]
    private static extern bool ClientToScreen(IntPtr hWnd, ref POINT pt);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X; public int Y; }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int GetClassNameW(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern bool EnumChildWindows(IntPtr hWndParent, EnumChildProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern bool BringWindowToTop(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool RedrawWindow(IntPtr hWnd, IntPtr lprcUpdate, IntPtr hrgnUpdate, uint flags);

    private const uint RDW_INVALIDATE = 0x0001;
    private const uint RDW_UPDATENOW  = 0x0100;
    private const uint RDW_ALLCHILDREN = 0x0080;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    private delegate bool EnumChildProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("gdi32.dll")]
    private static extern int ChoosePixelFormat(IntPtr hdc, ref PIXELFORMATDESCRIPTOR ppfd);

    [DllImport("gdi32.dll")]
    private static extern bool SetPixelFormat(IntPtr hdc, int iPixelFormat, ref PIXELFORMATDESCRIPTOR ppfd);

    [DllImport("gdi32.dll")]
    private static extern bool SwapBuffers(IntPtr hdc);

    [DllImport("opengl32.dll")]
    private static extern IntPtr wglCreateContext(IntPtr hDC);

    [DllImport("opengl32.dll")]
    private static extern bool wglMakeCurrent(IntPtr hDC, IntPtr hGLRC);

    [DllImport("opengl32.dll")]
    private static extern bool wglDeleteContext(IntPtr hGLRC);

    [DllImport("opengl32.dll")]
    private static extern IntPtr wglGetProcAddress(string name);

    [DllImport("opengl32.dll")]
    private static extern void glViewport(int x, int y, int width, int height);

    [DllImport("opengl32.dll")]
    private static extern void glClearColor(float r, float g, float b, float a);

    [DllImport("opengl32.dll")]
    private static extern void glClear(uint mask);

    private delegate void glBindFramebufferDelegate(uint target, uint framebuffer);
    private static glBindFramebufferDelegate? _glBindFramebuffer;

    private const uint GL_COLOR_BUFFER_BIT   = 0x00004000;
    private const uint GL_DEPTH_BUFFER_BIT   = 0x00000100;
    private const uint GL_STENCIL_BUFFER_BIT = 0x00000400;
    private const uint GL_FRAMEBUFFER        = 0x8D40;

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate IntPtr WglCreateContextAttribsARBDelegate(IntPtr hDC, IntPtr hShareContext, int[] attribList);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    private struct WNDCLASSEXA
    {
        public uint cbSize, style;
        public IntPtr lpfnWndProc;
        public int cbClsExtra, cbWndExtra;
        public IntPtr hInstance, hIcon, hCursor, hbrBackground;
        [MarshalAs(UnmanagedType.LPStr)] public string? lpszMenuName;
        [MarshalAs(UnmanagedType.LPStr)] public string lpszClassName;
        public IntPtr hIconSm;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PIXELFORMATDESCRIPTOR
    {
        public ushort nSize, nVersion;
        public uint dwFlags;
        public byte iPixelType, cColorBits, cRedBits, cRedShift, cGreenBits, cGreenShift, cBlueBits, cBlueShift;
        public byte cAlphaBits, cAlphaShift, cAccumBits, cAccumRedBits, cAccumGreenBits, cAccumBlueBits, cAccumAlphaBits;
        public byte cDepthBits, cStencilBits, cAuxBuffers, iLayerType, bReserved;
        public uint dwLayerMask, dwVisibleMask, dwDamageMask;
    }

    private const uint PFD_DRAW_TO_WINDOW = 0x00000004;
    private const uint PFD_SUPPORT_OPENGL = 0x00000020;
    private const uint PFD_DOUBLEBUFFER   = 0x00000001;
    private const uint WS_POPUP        = 0x80000000;
    private const uint WS_CHILD           = 0x40000000;
    private const uint WS_EX_LAYERED   = 0x00080000;
    private const uint WS_EX_NOACTIVATE = 0x08000000;
    private const uint WS_EX_TOOLWINDOW = 0x00000080;
    private const uint WS_EX_TRANSPARENT = 0x00000020;
    private const uint WS_VISIBLE         = 0x10000000;
    private const uint WS_CLIPCHILDREN    = 0x02000000;
    private const uint WS_CLIPSIBLINGS    = 0x04000000;
    private const uint CS_OWNDC           = 0x0020;
    private const uint CS_DBLCLKS         = 0x0008;
    private const int  WGL_CONTEXT_MAJOR_VERSION_ARB = 0x2091;
    private const int  WGL_CONTEXT_MINOR_VERSION_ARB = 0x2092;
    private const uint SWP_NOZORDER       = 0x0004;
    private const uint SWP_NOACTIVATE      = 0x0010;
    private const uint SWP_NOMOVE          = 0x0002;
    private const uint SWP_NOSIZE          = 0x0001;
    private static readonly IntPtr HWND_TOP = IntPtr.Zero;

    private const string GlWindowClass = "MapLibreCabiGL";
    private static WndProcDelegate? _wndProcKeepAlive;
    private static bool _classRegistered;

    private static void EnsureGlWindowClassRegistered()
    {
        if (_classRegistered) return;
        _wndProcKeepAlive = (hWnd, msg, w, l) => DefWindowProcA(hWnd, msg, w, l);
        var wc = new WNDCLASSEXA
        {
            cbSize        = (uint)Marshal.SizeOf<WNDCLASSEXA>(),
            style         = CS_OWNDC | CS_DBLCLKS,
            lpfnWndProc   = Marshal.GetFunctionPointerForDelegate(_wndProcKeepAlive),
            hInstance     = GetModuleHandleW(IntPtr.Zero),
            lpszClassName = GlWindowClass,
        };
        RegisterClassExA(ref wc);
        _classRegistered = true;
    }

    // ── Known layout property names ───────────────────────────────────────────
    // All others are treated as paint properties.

    private static readonly HashSet<string> LayoutPropertyNames = new(StringComparer.Ordinal)
    {
        "visibility",
        // symbol layout
        "symbol-placement","symbol-spacing","symbol-avoid-edges","symbol-sort-key","symbol-z-order",
        "icon-allow-overlap","icon-ignore-placement","icon-optional","icon-rotation-alignment",
        "icon-size","icon-text-fit","icon-text-fit-padding","icon-image","icon-rotate",
        "icon-padding","icon-keep-upright","icon-offset","icon-anchor","icon-pitch-alignment",
        "text-pitch-alignment","text-rotation-alignment","text-field","text-font","text-size",
        "text-max-width","text-line-height","text-letter-spacing","text-justify",
        "text-radial-offset","text-variable-anchor","text-anchor","text-max-angle",
        "text-writing-mode","text-rotate","text-padding","text-keep-upright","text-transform",
        "text-offset","text-allow-overlap","text-ignore-placement","text-optional",
        // line layout
        "line-cap","line-join","line-miter-limit","line-round-limit","line-sort-key",
        // fill layout
        "fill-sort-key",
        // circle layout
        "circle-sort-key",
    };

    // ── State ─────────────────────────────────────────────────────────────────

    private readonly IntPtr _parentHwnd;
    private readonly string? _styleString;
    private readonly float   _pixelRatio;

    private IntPtr       _effectiveParentHwnd = IntPtr.Zero;
    private IntPtr       _childHwnd = IntPtr.Zero;
    private IntPtr       _hDC       = IntPtr.Zero;
    private IntPtr       _hGLRC     = IntPtr.Zero;
    private MbglRunLoop? _runLoop;
    private MbglFrontend? _frontend;
    private MbglMap?      _map;
    private MbglStyle?    _style;

    // Pumps the libuv run loop on the UI thread. Without this, async HTTP responses
    // for style/tile downloads are never delivered and StyleLoaded never fires.
    private Microsoft.UI.Xaml.DispatcherTimer? _runLoopTimer;
    private bool _renderNeedsUpdate;

    private bool _initialized;
    private bool _styleReady;

    /// <summary>The WinUI placeholder element the handler uses as the platform view.</summary>
    public Microsoft.UI.Xaml.Controls.Grid View { get; } = new();

    // ── Events ────────────────────────────────────────────────────────────────
    public event Action<Map>?                            OnMapReadyReceived;
    public event Action?                                 OnDidBecomeIdleReceived;
    public event Action<int>?                            OnCameraMoveStartedReceived;
    public event Action?                                 OnCameraMoveReceived;
    public event Action?                                 OnCameraIdleReceived;
    public event Action<int>?                            OnCameraTrackingChangedReceived;
    public event Action?                                 OnCameraTrackingDismissedReceived;
    public event Func<LatLng, bool>?                     OnMapClickReceived;
    public event Func<LatLng, bool>?                     OnMapLongClickReceived;
    public event Action<Maps.Style>?                     OnStyleLoadedReceived;
    public event Action<Location>?                       OnUserLocationUpdateReceived;
    public event Action<string>?                         OnDidFailLoadingMapReceived;
    public event Action<string>?                         OnStyleImageMissingReceived;

    public MapLibreMapController(IntPtr parentHwnd, float pixelRatio, string? styleString)
    {
        _parentHwnd  = parentHwnd;
        _pixelRatio  = pixelRatio;
        _styleString = styleString;
    }

    // ── Init ──────────────────────────────────────────────────────────────────

    public void Init()
    {
        View.Loaded       += (_, _) => TryInitialize();
        View.SizeChanged  += (_, e) => OnViewSizeChanged(new Microsoft.Maui.Graphics.Size(e.NewSize.Width, e.NewSize.Height));
        View.Unloaded     += (_, _) => DisposeNative();
    }

    private void TryInitialize()
    {
        if (_initialized || _parentHwnd == IntPtr.Zero) return;
        if (View.ActualWidth < 2 || View.ActualHeight < 2) return;

        // AIRSPACE WORKAROUND: Create the GL window as a borderless top-level popup
        // OWNED BY (not parented to) the main XAML window. This bypasses WinUI's
        // DirectComposition surface, which renders on top of any HWND child of the
        // bridge. We then track main-window movement to keep the popup aligned.
        IntPtr ownerHwnd = _parentHwnd;
        System.Diagnostics.Debug.WriteLine(
            $"[MapLibre.Win] TryInitialize (popup mode) owner=0x{ownerHwnd.ToInt64():X}");

        int physW = Math.Max(1, (int)(View.ActualWidth  * _pixelRatio));
        int physH = Math.Max(1, (int)(View.ActualHeight * _pixelRatio));

        EnsureGlWindowClassRegistered();

        _childHwnd = CreateWindowExA(
            WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW,
            GlWindowClass, "",
            WS_POPUP | WS_VISIBLE | WS_CLIPCHILDREN | WS_CLIPSIBLINGS,
            0, 0, physW, physH,
            ownerHwnd, IntPtr.Zero, GetModuleHandleW(IntPtr.Zero), IntPtr.Zero);

        if (_childHwnd == IntPtr.Zero)
            throw new InvalidOperationException("Failed to create GL popup window.");

        _effectiveParentHwnd = ownerHwnd;

        InitOpenGl(physW, physH);
        InitMaplibre(physW, physH);
        UpdateChildWindowPosition();
        StartRunLoopPump();

        // Force at least one paint so we can visually verify the GL HWND is rendering
        // (vs. being covered by the WinUI compositor).
        _renderNeedsUpdate = true;

        _initialized = true;
        System.Diagnostics.Debug.WriteLine(
            $"[MapLibre.Win] TryInitialize done. childHwnd=0x{_childHwnd.ToInt64():X} " +
            $"size={physW}x{physH} pixelRatio={_pixelRatio} visible={IsWindowVisible(_childHwnd)}");
    }

    private void InitOpenGl(int physW, int physH)
    {
        _hDC = GetDC(_childHwnd);
        var pfd = new PIXELFORMATDESCRIPTOR
        {
            nSize      = (ushort)Marshal.SizeOf<PIXELFORMATDESCRIPTOR>(),
            nVersion   = 1,
            dwFlags    = PFD_DRAW_TO_WINDOW | PFD_SUPPORT_OPENGL | PFD_DOUBLEBUFFER,
            cColorBits = 32,
            cDepthBits = 24,
            cStencilBits = 8,
        };
        int fmt = ChoosePixelFormat(_hDC, ref pfd);
        SetPixelFormat(_hDC, fmt, ref pfd);

        // Bootstrap context to get wglCreateContextAttribsARB
        var tmpCtx = wglCreateContext(_hDC);
        wglMakeCurrent(_hDC, tmpCtx);

        var fn = wglGetProcAddress("wglCreateContextAttribsARB");
        if (fn != IntPtr.Zero)
        {
            var createFn = Marshal.GetDelegateForFunctionPointer<WglCreateContextAttribsARBDelegate>(fn);
            var attribs = new[]
            {
                WGL_CONTEXT_MAJOR_VERSION_ARB, 3,
                WGL_CONTEXT_MINOR_VERSION_ARB, 2,
                0
            };
            wglMakeCurrent(IntPtr.Zero, IntPtr.Zero);
            wglDeleteContext(tmpCtx);
            _hGLRC = createFn(_hDC, IntPtr.Zero, attribs);
        }
        else
        {
            _hGLRC = tmpCtx;
        }
        wglMakeCurrent(_hDC, _hGLRC);

        // Resolve the GL3 entry point we need to bind the default framebuffer
        // before each frontend.Render() call. Without this, mbgl renders into its
        // own offscreen FBO and the visible back buffer stays solid black.
        if (_glBindFramebuffer == null)
        {
            var p = wglGetProcAddress("glBindFramebuffer");
            if (p != IntPtr.Zero)
                _glBindFramebuffer = Marshal.GetDelegateForFunctionPointer<glBindFramebufferDelegate>(p);
        }
    }

    private void InitMaplibre(int physW, int physH)
    {
        _runLoop = new MbglRunLoop();

        _frontend = new MbglFrontend(_hDC, _hGLRC, physW, physH, _pixelRatio, OnRender);

        _map = new MbglMap(
            _frontend, _runLoop,
            pixelRatio: _pixelRatio,
            observer: OnMapObserverEvent);

        _map.SetSize(physW, physH);

        if (!string.IsNullOrEmpty(_styleString))
        {
            if (_styleString!.StartsWith('{'))
                _map.SetStyleJson(_styleString);
            else
                _map.SetStyleUrl(_styleString);
        }

        OnMapReadyReceived?.Invoke(new Map(null));
    }

    private void OnRender()
    {
        // Native frontend signalled it has new content — request a redraw on next pump tick.
        _renderNeedsUpdate = true;
    }

    private void StartRunLoopPump()
    {
        if (_runLoopTimer != null) return;
        _runLoopTimer = new Microsoft.UI.Xaml.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        _runLoopTimer.Tick += OnRunLoopTick;
        _runLoopTimer.Start();
    }

    private void OnRunLoopTick(object? sender, object e)
    {
        // Pump libuv so HTTP callbacks for style/tile downloads run on this thread.
        _runLoop?.RunOnce();

        // Keep popup aligned with the View on every tick (track window moves).
        UpdateChildWindowPosition();

        if (_renderNeedsUpdate && _hGLRC != IntPtr.Zero && _hDC != IntPtr.Zero && _frontend != null)
        {
            _renderNeedsUpdate = false;
            wglMakeCurrent(_hDC, _hGLRC);

            // Bind default framebuffer + clear so frontend.Render() composites into
            // the back buffer that SwapBuffers will present.
            int physW = Math.Max(1, (int)(View.ActualWidth  * _pixelRatio));
            int physH = Math.Max(1, (int)(View.ActualHeight * _pixelRatio));
            _glBindFramebuffer?.Invoke(GL_FRAMEBUFFER, 0);
            glViewport(0, 0, physW, physH);
            // Neutral light-gray clear; mbgl draws the style's background layer over this.
            // Do NOT use a "garish" diagnostic color in release — any pixel mbgl doesn't
            // touch (e.g. before first frame, between tiles) will show through.
            glClearColor(0.85f, 0.90f, 0.97f, 1f);
            glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT | GL_STENCIL_BUFFER_BIT);

            try { _frontend.Render(); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[MapLibre.Win] Render threw: {ex.Message}"); }
            SwapBuffers(_hDC);

            if (++_renderTickCounter <= 5 || _renderTickCounter % 60 == 0)
                System.Diagnostics.Debug.WriteLine(
                    $"[MapLibre.Win] tick#{_renderTickCounter} rendered+swapped {physW}x{physH}");
        }
    }

    private int _renderTickCounter;

    private void OnMapObserverEvent(string eventName, string? detail)
    {
        switch (eventName)
        {
            case "onDidFinishLoadingStyle":
                _styleReady = true;
                _style = _map?.GetStyle();
                _renderNeedsUpdate = true;
                OnStyleLoadedReceived?.Invoke(new Maps.Style(null));
                break;
            case "onDidBecomeIdle":
                OnDidBecomeIdleReceived?.Invoke();
                break;
            case "onCameraIsChanging":
                OnCameraMoveReceived?.Invoke();
                break;
            case "onCameraDidChange":
                OnCameraIdleReceived?.Invoke();
                break;
            case "onDidFailLoadingMap":
                OnDidFailLoadingMapReceived?.Invoke(detail ?? string.Empty);
                break;
            case "onStyleImageMissing":
                OnStyleImageMissingReceived?.Invoke(detail ?? string.Empty);
                break;
        }
    }

    private void OnViewSizeChanged(Microsoft.Maui.Graphics.Size newSize)
    {
        if (!_initialized) return;
        int physW = Math.Max(1, (int)(newSize.Width  * _pixelRatio));
        int physH = Math.Max(1, (int)(newSize.Height * _pixelRatio));

        _frontend?.SetSize(physW, physH);
        _map?.SetSize(physW, physH);
        SetWindowPos(_childHwnd, IntPtr.Zero, 0, 0, physW, physH, SWP_NOZORDER);
        UpdateChildWindowPosition();
        _renderNeedsUpdate = true;
        _map?.TriggerRepaint();
    }

    private void UpdateChildWindowPosition()
    {
        if (_childHwnd == IntPtr.Zero || !View.IsLoaded) return;

        // Popup is top-level → coordinates are SCREEN coords.
        // Get the View's position in XAML root, then translate via the owner
        // window's client-to-screen mapping.
        var transform = View.TransformToVisual(null);
        var pt = transform.TransformPoint(new Windows.Foundation.Point(0, 0));

        // Convert XAML-root client coords to screen coords using the owner HWND.
        // Owner is the top-level XAML window. ClientToScreen with (pt.X*ratio, pt.Y*ratio)
        // — but XAML root coords map to the *island bridge* client area, not the
        // top-level client. The bridge's screen origin we already saw was (34,26)
        // on the diagnostic earlier; computing precisely:
        IntPtr bridgeHwnd = TryGetXamlIslandHwnd();
        IntPtr coordOrigin = bridgeHwnd != IntPtr.Zero ? bridgeHwnd : _parentHwnd;
        var origin = new POINT { X = 0, Y = 0 };
        ClientToScreen(coordOrigin, ref origin);

        int x = (int)(origin.X + pt.X * _pixelRatio);
        int y = (int)(origin.Y + pt.Y * _pixelRatio);
        int w = Math.Max(1, (int)(View.ActualWidth  * _pixelRatio));
        int h = Math.Max(1, (int)(View.ActualHeight * _pixelRatio));

        SetWindowPos(_childHwnd, HWND_TOP, x, y, w, h, SWP_NOACTIVATE);

        if (_logPositionCount < 5)
        {
            _logPositionCount++;
            GetWindowRect(_childHwnd, out var sr);
            System.Diagnostics.Debug.WriteLine(
                $"[MapLibre.Win] UpdateChildWindowPosition #{_logPositionCount} " +
                $"set screen=({x},{y},{w}x{h}) actual=({sr.Left},{sr.Top})-({sr.Right},{sr.Bottom}) " +
                $"originHwnd=0x{coordOrigin.ToInt64():X} screenOrigin=({origin.X},{origin.Y})");
        }
    }
    private int _logPositionCount;

    private IntPtr TryGetXamlIslandHwnd()
    {
        // Strategy: WinUI 3 hosts XAML content inside a child HWND of the top-level
        // window. The class names that have appeared across SDK versions:
        //   "Microsoft.UI.Content.DesktopChildSiteBridge"
        //   "Microsoft.UI.Content.DesktopWindowContentBridge"
        //   "Windows.UI.Composition.DesktopWindowContentBridge"
        // We need to parent our GL HWND inside that child so it renders on top of
        // the XAML compositor surface (which uses DirectComposition; HWND children
        // of the bridge naturally render above DComp content).
        IntPtr found = IntPtr.Zero;
        try
        {
            EnumChildWindows(_parentHwnd, (h, _) =>
            {
                var sb = new System.Text.StringBuilder(256);
                GetClassNameW(h, sb, sb.Capacity);
                var cls = sb.ToString();
                if (cls.Contains("ContentBridge") || cls.Contains("DesktopChildSiteBridge"))
                {
                    found = h;
                    System.Diagnostics.Debug.WriteLine(
                        $"[MapLibre.Win] Found XAML bridge: 0x{h.ToInt64():X} class='{cls}'");
                    return false; // stop
                }
                return true;
            }, IntPtr.Zero);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MapLibre.Win] TryGetXamlIslandHwnd failed: {ex.Message}");
        }
        return found;
    }

    // ── IMapLibreMapOptionsSink ───────────────────────────────────────────────

    public void SetCameraTargetBounds(LatLngBounds bounds,
        double minZoom = double.NaN, double maxZoom = double.NaN,
        double minPitch = double.NaN, double maxPitch = double.NaN)
    {
        _map?.SetBounds(bounds.SouthWest.Latitude, bounds.SouthWest.Longitude,
                        bounds.NorthEast.Latitude, bounds.NorthEast.Longitude,
                        minZoom, maxZoom, minPitch, maxPitch);
    }
    public void SetCompassEnabled(bool compassEnabled)     { /* no-op — overlay not yet implemented */ }
    public void SetRotateGesturesEnabled(bool v)          { /* TODO: mbgl gesture flags */ }
    public void SetScrollGesturesEnabled(bool v)          { }
    public void SetTiltGesturesEnabled(bool v)            { }
    public void SetTrackCameraPosition(bool v)            { }
    public void SetZoomGesturesEnabled(bool v)            { }
    public void SetMyLocationEnabled(bool v)              { }
    public void SetMyLocationTrackingMode(int v)          { }
    public void SetMyLocationRenderMode(int v)            { }
    public void SetLogoViewMargins(int x, int y)          { }
    public void SetCompassGravity(int gravity)            { }
    public void SetCompassViewMargins(int x, int y)       { }
    public void SetAttributionButtonGravity(int gravity)  { }
    public void SetAttributionButtonMargins(int x, int y) { }

    public void SetStyleString(string styleString)
    {
        if (_map == null) return;
        if (styleString.StartsWith('{'))
            _map.SetStyleJson(styleString);
        else
            _map.SetStyleUrl(styleString);
    }

    public void SetMinMaxZoomPreference(double? min, double? max)
    {
        if (min.HasValue) _map?.SetMinZoom(min.Value);
        if (max.HasValue) _map?.SetMaxZoom(max.Value);
    }

    // ── Sources ───────────────────────────────────────────────────────────────

    public void AddGeoJsonSource(string sourceName, string source)
    {
        if (!_styleReady || _style == null) return;
        if (_style.HasSource(sourceName)) return;
        var s = _style.AddGeoJsonSource(sourceName);
        s.SetGeoJson(source);
    }

    public void SetGeoJsonSource(string sourceName, string source)
    {
        if (!_styleReady || _style == null) return;
        // AddGeoJsonSource with overwrite not directly supported — update via URL trick
        AddGeoJsonSource(sourceName, source);
    }

    public void SetGeoJsonFeature(string sourceName, string geojsonFeature)
    {
        // Partial update: not directly supported in C ABI — replace whole source for now
        if (!_styleReady || _style == null) return;
        if (_style.HasSource(sourceName))
            _style.RemoveSource(sourceName);
        AddGeoJsonSource(sourceName, geojsonFeature);
    }

    public void AddRasterSource(string sourceName, string? tileUrl, string[]? tileUrlTemplates,
        int tileSize, int minZoom, int maxZoom)
    {
        if (!_styleReady || _style == null) return;
        var url = tileUrl ?? tileUrlTemplates?.FirstOrDefault();
        if (url == null) return;
        _style.AddRasterSource(sourceName, url, tileSize);
    }

    public void AddRasterDemSource(string sourceName, string? tileUrl, string[]? tileUrlTemplates,
        int tileSize, int minZoom, int maxZoom)
    {
        if (!_styleReady || _style == null) return;
        var url = tileUrl ?? tileUrlTemplates?.FirstOrDefault();
        if (url == null) return;
        _style.AddRasterDemSource(sourceName, url, tileSize);
    }

    public void AddVectorSource(string sourceName, string? tileUrl, string[]? tileUrlTemplates,
        int minZoom, int maxZoom)
    {
        if (!_styleReady || _style == null) return;
        var url = tileUrl ?? tileUrlTemplates?.FirstOrDefault();
        if (url == null) return;
        _style.AddVectorSource(sourceName, url);
    }

    public void AddImageSource(string sourceName, string url, LatLngQuad? coordinates)
    {
        if (!_styleReady || _style == null) return;
        // C ABI does not yet expose image sources with lat/lng quads; add as raster-tile workaround
        _style.AddRasterSource(sourceName, url);
    }

    public void RemoveSource(string sourceId)
    {
        if (!_styleReady || _style == null) return;
        _style.RemoveSource(sourceId);
    }

    // ── Layers ────────────────────────────────────────────────────────────────

    public void AddFillLayer(string layerName, string sourceName, string? belowLayerId,
        string? sourceLayer, IDictionary<string, object?> properties,
        float minZoom = 0, float maxZoom = 0, bool enableInteraction = false)
    {
        if (!_styleReady || _style == null) return;
        var layer = _style.AddFillLayer(layerName, sourceName, belowLayerId);
        ApplyLayerMeta(layer, sourceLayer, minZoom, maxZoom);
        ApplyProperties(layer, properties);
    }

    public void AddLineLayer(string layerName, string sourceName, string? belowLayerId,
        string? sourceLayer, IDictionary<string, object?> properties,
        float minZoom = 0, float maxZoom = 0, bool enableInteraction = false)
    {
        if (!_styleReady || _style == null) return;
        var layer = _style.AddLineLayer(layerName, sourceName, belowLayerId);
        ApplyLayerMeta(layer, sourceLayer, minZoom, maxZoom);
        ApplyProperties(layer, properties);
    }

    public void AddHeatmapLayer(
        string layerName,
        string sourceName,
        IDictionary<string, object?> properties,
        float minZoom = 0,
        float maxZoom = 0,
        string? belowLayerId = null)
    {
        if (!_styleReady || _style == null) return;
        var layer = _style.AddHeatmapLayer(layerName, sourceName, belowLayerId);
        ApplyLayerMeta(layer, null, minZoom, maxZoom);
        ApplyProperties(layer, properties);
    }

    public void AddCircleLayer(string layerName, string sourceName, string? belowLayerId,
        string? sourceLayer, IDictionary<string, object?> properties,
        float minZoom = 0, float maxZoom = 0, bool enableInteraction = false)
    {
        if (!_styleReady || _style == null) return;
        var layer = _style.AddCircleLayer(layerName, sourceName, belowLayerId);
        ApplyLayerMeta(layer, sourceLayer, minZoom, maxZoom);
        ApplyProperties(layer, properties);
    }

    public void AddSymbolLayer(string layerName, string sourceName, string? belowLayerId,
        string? sourceLayer, IDictionary<string, object?> properties,
        float minZoom = 0, float maxZoom = 0, bool enableInteraction = false)
    {
        if (!_styleReady || _style == null) return;
        var layer = _style.AddSymbolLayer(layerName, sourceName, belowLayerId);
        ApplyLayerMeta(layer, sourceLayer, minZoom, maxZoom);
        ApplyProperties(layer, properties);
    }

    public void AddRasterLayer(string layerName, string sourceName,
        IDictionary<string, object?> properties,
        float minZoom = 0, float maxZoom = 0, string? belowLayerId = null)
    {
        if (!_styleReady || _style == null) return;
        var layer = _style.AddRasterLayer(layerName, sourceName, belowLayerId);
        ApplyLayerMeta(layer, null, minZoom, maxZoom);
        ApplyProperties(layer, properties);
    }

    public void AddHillshadeLayer(string layerName, string sourceName,
        IDictionary<string, object?> properties,
        float minZoom = 0, float maxZoom = 0, string? belowLayerId = null)
    {
        if (!_styleReady || _style == null) return;
        var layer = _style.AddHillshadeLayer(layerName, sourceName, belowLayerId);
        ApplyLayerMeta(layer, null, minZoom, maxZoom);
        ApplyProperties(layer, properties);
    }

    public void AddFillExtrusionLayer(string layerName, string sourceName, string? belowLayerId,
        string? sourceLayer, IDictionary<string, object?> properties,
        float minZoom = 0, float maxZoom = 0, bool enableInteraction = false)
    {
        if (!_styleReady || _style == null) return;
        var layer = _style.AddFillExtrusionLayer(layerName, sourceName, belowLayerId);
        ApplyLayerMeta(layer, sourceLayer, minZoom, maxZoom);
        ApplyProperties(layer, properties);
    }

    public void RemoveLayer(string layerId)
    {
        if (!_styleReady || _style == null) return;
        _style.RemoveLayer(layerId);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void ApplyLayerMeta(MbglLayer layer, string? sourceLayer, float minZoom, float maxZoom)
    {
        if (sourceLayer != null) layer.SetSourceLayer(sourceLayer);
        if (minZoom > 0) layer.SetMinZoom(minZoom);
        if (maxZoom > 0) layer.SetMaxZoom(maxZoom);
    }

    private static void ApplyProperties(MbglLayer layer, IDictionary<string, object?> props)
    {
        foreach (var (k, v) in props)
        {
            var json = JsonSerializer.Serialize(v);
            if (LayoutPropertyNames.Contains(k))
                layer.SetLayoutProperty(k, json);
            else
                layer.SetPaintProperty(k, json);
        }
    }

    // ── Cleanup ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Public entry point called by <see cref="MapLibreMapHandler"/> from its
    /// <c>DisconnectHandler</c> override. Idempotent — safe to call more than once.
    /// </summary>
    public void Shutdown() => DisposeNative();

    private void DisposeNative()
    {
        // Stop the pump FIRST so no tick can run after native objects are freed
        // (avoids 0xc0000374 heap corruption on page tear-down).
        if (_runLoopTimer != null)
        {
            _runLoopTimer.Stop();
            _runLoopTimer.Tick -= OnRunLoopTick;
            _runLoopTimer = null;
        }

        // Make GL context current on this thread so frontend/map destructors can
        // free GL resources cleanly.
        if (_hGLRC != IntPtr.Zero && _hDC != IntPtr.Zero)
        {
            try { wglMakeCurrent(_hDC, _hGLRC); } catch { }
        }

        _style    = null;
        _map?.Dispose();      _map      = null;
        // Drain any cancellation callbacks queued by ~Map() onto the libuv loop
        // before destroying the loop itself — prevents 0xc0000374 heap corruption
        // from late tile/style download completions firing into freed memory.
        try { for (int i = 0; i < 8; i++) _runLoop?.RunOnce(); } catch { }
        _frontend?.Dispose(); _frontend = null;
        try { for (int i = 0; i < 4; i++) _runLoop?.RunOnce(); } catch { }
        _runLoop?.Dispose();  _runLoop  = null;

        if (_hGLRC != IntPtr.Zero)
        {
            wglMakeCurrent(IntPtr.Zero, IntPtr.Zero);
            wglDeleteContext(_hGLRC);
            _hGLRC = IntPtr.Zero;
        }
        if (_hDC != IntPtr.Zero)
        {
            ReleaseDC(_childHwnd, _hDC);
            _hDC = IntPtr.Zero;
        }
        if (_childHwnd != IntPtr.Zero)
        {
            DestroyWindow(_childHwnd);
            _childHwnd = IntPtr.Zero;
        }
        _initialized = false;
    }

    // ── Camera ────────────────────────────────────────────────────────────────

    public void JumpTo(double latitude, double longitude, double zoom,
        double bearing = 0, double pitch = 0)
        => _map?.JumpTo(latitude, longitude, zoom, bearing, pitch);

    public void EaseTo(double latitude, double longitude, double zoom,
        double bearing = 0, double pitch = 0, long durationMs = 300)
        => _map?.EaseTo(latitude, longitude, zoom, bearing, pitch, durationMs);

    public void FlyTo(double latitude, double longitude, double zoom,
        double bearing = 0, double pitch = 0, long durationMs = 500)
        => _map?.FlyTo(latitude, longitude, zoom, bearing, pitch, durationMs);

    public void CancelTransitions() => _map?.CancelTransitions();

    public double GetZoom()    => _map?.Zoom    ?? 0;
    public double GetBearing() => _map?.Bearing ?? 0;
    public double GetPitch()   => _map?.Pitch   ?? 0;
    public LatLng GetCenter()
    {
        if (_map == null) return new LatLng(0, 0);
        var (lat, lon) = _map.Center;
        return new LatLng(lat, lon);
    }

    public (double X, double Y) LatLngToScreenPoint(double latitude, double longitude)
        => _map?.PixelForLatLng(latitude, longitude) ?? (0, 0);

    public LatLng ScreenPointToLatLng(double x, double y)
    {
        if (_map == null) return new LatLng(0, 0);
        var (lat, lon) = _map.LatLngForPixel(x, y);
        return new LatLng(lat, lon);
    }

    public string? QueryRenderedFeaturesAtPoint(double x, double y, string? layerIds = null)
        => _map?.QueryRenderedFeaturesAtPoint(x, y, layerIds);

    public string? QueryRenderedFeaturesInBox(double x1, double y1, double x2, double y2,
        string? layerIds = null)
        => _map?.QueryRenderedFeaturesInBox(x1, y1, x2, y2, layerIds);

    // ── Tier 1 – gesture / interactive movement ───────────────────────────────
    public void SetGestureInProgress(bool inProgress) => _map?.SetGestureInProgress(inProgress);
    public void MoveBy(double dx, double dy, long durationMs = 0) => _map?.MoveBy(dx, dy, durationMs);
    public void RotateBy(double x0, double y0, double x1, double y1) => _map?.RotateBy(x0, y0, x1, y1);
    public void PitchBy(double deltaDegrees, long durationMs = 0) => _map?.PitchBy(deltaDegrees, durationMs);

    // ── Tier 1 – map option setters ───────────────────────────────────────────
    public void SetNorthOrientation(int orientation) => _map?.SetNorthOrientation(orientation);
    public void SetConstrainMode(int mode) => _map?.SetConstrainMode(mode);
    public void SetViewportMode(int mode) => _map?.SetViewportMode(mode);

    // ── Tier 1 – bounds read-back ─────────────────────────────────────────────
    public BoundOptions GetBounds() => _map?.GetBounds() ?? default;

    // ── Tier 2 – tile LOD / prefetch ─────────────────────────────────────────
    public void SetPrefetchZoomDelta(int delta) => _map?.SetPrefetchZoomDelta(delta);
    public int  GetPrefetchZoomDelta() => _map?.GetPrefetchZoomDelta() ?? 4;
    public void SetTileLodMinRadius(double radius) => _map?.SetTileLodMinRadius(radius);
    public void SetTileLodScale(double scale) => _map?.SetTileLodScale(scale);
    public void SetTileLodPitchThreshold(double thresholdRadians) => _map?.SetTileLodPitchThreshold(thresholdRadians);
    public void SetTileLodZoomShift(double shift) => _map?.SetTileLodZoomShift(shift);
    public void SetTileLodMode(int mode) => _map?.SetTileLodMode(mode);

    // ── Tier 2 – camera / batch projection ───────────────────────────────────
    public CameraResult CameraForLatLngs(
        IReadOnlyList<(double Lat, double Lon)> points,
        double padTop = 0, double padLeft = 0, double padBottom = 0, double padRight = 0)
        => _map?.CameraForLatLngs(points, padTop, padLeft, padBottom, padRight) ?? default;

    public (double X, double Y)[] PixelsForLatLngs(IReadOnlyList<(double Lat, double Lon)> points)
        => _map?.PixelsForLatLngs(points) ?? [];

    public (double Lat, double Lon)[] LatLngsForPixels(IReadOnlyList<(double X, double Y)> pixels)
        => _map?.LatLngsForPixels(pixels) ?? [];

    // ── Input forwarding (called by MapLibreMapHandler) ───────────────────────

    public void OnPointerWheelChanged(double delta, double cx, double cy)
        => _map?.OnScroll(delta, cx, cy);

    public void OnPointerPressed(double x, double y)
        => _map?.OnPanStart(x, y);

    public void OnPointerMoved(double dx, double dy)
        => _map?.OnPanMove(dx, dy);

    public void OnPointerReleased()
        => _map?.OnPanEnd();

    public void OnDoubleTapped(double x, double y)
        => _map?.OnDoubleTap(x, y);

    public void OnPinch(double scale, double cx, double cy)
        => _map?.OnPinch(scale, cx, cy);
}
#endif
