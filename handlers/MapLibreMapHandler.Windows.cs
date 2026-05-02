#if WINDOWS
using Microsoft.Maui.Handlers;
using Microsoft.UI.Xaml.Controls;
using WinRT.Interop;

namespace Maui.MapLibre.Handlers;

public partial class MapLibreMapHandler : ViewHandler<MapLibreMap, Grid>
{
    private MapLibreMapController _controller = null!;
    private string _styleUrl = string.Empty;

    public IMapLibreMapController Controller => _controller;

    public MapLibreMapHandler() : base(PropertyMapper) { }

    protected override Grid CreatePlatformView()
    {
        var window  = MauiContext?.Services?.GetService<Microsoft.UI.Xaml.Window>()
                   ?? GetWindowFromApplication();

        float dpi = GetDpiForWindow(window);

        var hwnd = WindowNative.GetWindowHandle(window);

        _controller = MapLibreMapFactory.Create(hwnd, dpi, new Dictionary<string, object>
        {
            ["styleString"] = _styleUrl
        });

        _controller.OnMapReadyReceived              += VirtualView.OnMapReady;
        _controller.OnStyleLoadedReceived           += VirtualView.OnStyleLoaded;
        _controller.OnDidBecomeIdleReceived         += VirtualView.OnDidBecomeIdle;
        _controller.OnCameraMoveStartedReceived      += VirtualView.OnCameraMoveStarted;
        _controller.OnCameraMoveReceived            += VirtualView.OnCameraMove;
        _controller.OnCameraIdleReceived            += VirtualView.OnCameraIdle;
        _controller.OnCameraTrackingChangedReceived += VirtualView.OnCameraTrackingChanged;
        _controller.OnCameraTrackingDismissedReceived += VirtualView.OnCameraTrackingDismissed;
        _controller.OnMapClickReceived              += VirtualView.OnMapClick;
        _controller.OnMapLongClickReceived          += VirtualView.OnMapLongClick;
        _controller.OnUserLocationUpdateReceived    += VirtualView.OnUserLocationUpdate;

        _controller.Init();
        return _controller.View;
    }

    public void UpdateStyleUrl(string styleUrl)
    {
        _styleUrl = styleUrl;
        _controller.SetStyleString(styleUrl);
    }

    public void UpdateMinMaxZoomPreference(double? minZoom, double? maxZoom)
        => _controller.SetMinMaxZoomPreference(minZoom, maxZoom);

    public void UpdateRotateGestureEnabled(bool v)   => _controller.SetRotateGesturesEnabled(v);
    public void UpdateScrollGesturesEnabled(bool v)  => _controller.SetScrollGesturesEnabled(v);
    public void UpdateTiltGesturesEnabled(bool v)    => _controller.SetTiltGesturesEnabled(v);
    public void UpdateTrackCameraPosition(bool v)    => _controller.SetTrackCameraPosition(v);
    public void UpdateZoomGesturesEnabled(bool v)    => _controller.SetZoomGesturesEnabled(v);
    public void UpdateMyLocationEnabled(bool v)      => _controller.SetMyLocationEnabled(v);
    public void UpdateMyLocationTrackingMode(int v)  => _controller.SetMyLocationTrackingMode(v);
    public void UpdateMyLocationRenderMode(int v)    => _controller.SetMyLocationRenderMode(v);

    public void UpdateLogoViewMargins(int x, int y)
        => _controller.SetLogoViewMargins(x, y);

    public void UpdateCompassGravity(int gravity)
        => _controller.SetCompassGravity(gravity);

    public void UpdateCompassViewMargins(int x, int y)
        => _controller.SetCompassViewMargins(x, y);

    public void UpdateAttributionButtonGravity(int gravity)
        => _controller.SetAttributionButtonGravity(gravity);

    public void UpdateAttributionButtonMargins(int x, int y)
        => _controller.SetAttributionButtonMargins(x, y);

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Microsoft.UI.Xaml.Window GetWindowFromApplication()
        => ((Microsoft.Maui.MauiWinUIApplication)Microsoft.UI.Xaml.Application.Current)
            .MainWindow;

    private static float GetDpiForWindow(Microsoft.UI.Xaml.Window window)
    {
        // XamlRoot.RasterizationScale is the DPI scale (1.0 = 96 dpi)
        return (float)(window.Content?.XamlRoot?.RasterizationScale ?? 1.0);
    }
}
#endif
