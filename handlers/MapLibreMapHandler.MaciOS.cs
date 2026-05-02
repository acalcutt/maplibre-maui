#nullable enable
using Microsoft.Maui.Handlers;
using UIKit;

namespace Maui.MapLibre.Handlers;

public partial class MapLibreMapHandler : ViewHandler<MapLibreMap, UIView>
{
    private MapLibreMapController _controller = null!;

    public IMapLibreMapController Controller => _controller;

    public MapLibreMapHandler() : base(PropertyMapper) { }

    protected override UIView CreatePlatformView()
    {
        var scale = (float)UIScreen.MainScreen.Scale;
        _controller = MapLibreMapFactory.Create(scale, VirtualView?.StyleUrl);

        _controller.OnMapReadyReceived              += VirtualView.OnMapReady;
        _controller.OnStyleLoadedReceived           += VirtualView.OnStyleLoaded;
        _controller.OnDidBecomeIdleReceived         += VirtualView.OnDidBecomeIdle;
        _controller.OnCameraMoveStartedReceived     += VirtualView.OnCameraMoveStarted;
        _controller.OnCameraMoveReceived            += VirtualView.OnCameraMove;
        _controller.OnCameraIdleReceived            += VirtualView.OnCameraIdle;
        _controller.OnCameraTrackingChangedReceived += VirtualView.OnCameraTrackingChanged;
        _controller.OnMapClickReceived              += VirtualView.OnMapClick;
        _controller.OnMapLongClickReceived          += VirtualView.OnMapLongClick;
        _controller.OnUserLocationUpdateReceived    += VirtualView.OnUserLocationUpdate;

        return _controller.View;
    }

    public void UpdateStyleUrl(string styleUrl)         => _controller.SetStyleString(styleUrl);
    public void UpdateMinMaxZoomPreference(double? min, double? max) => _controller.SetMinMaxZoomPreference(min, max);
    public void UpdateRotateGestureEnabled(bool v)      => _controller.SetRotateGesturesEnabled(v);
    public void UpdateScrollGesturesEnabled(bool v)     => _controller.SetScrollGesturesEnabled(v);
    public void UpdateTiltGesturesEnabled(bool v)       => _controller.SetTiltGesturesEnabled(v);
    public void UpdateTrackCameraPosition(bool v)       => _controller.SetTrackCameraPosition(v);
    public void UpdateZoomGesturesEnabled(bool v)       => _controller.SetZoomGesturesEnabled(v);
    public void UpdateMyLocationEnabled(bool v)         => _controller.SetMyLocationEnabled(v);
    public void UpdateMyLocationTrackingMode(int v)     => _controller.SetMyLocationTrackingMode(v);
    public void UpdateMyLocationRenderMode(int v)       => _controller.SetMyLocationRenderMode(v);
    public void UpdateLogoViewMargins(int?[]? margin)   { if (margin?.Length >= 2 && margin[0] != null && margin[1] != null) _controller.SetLogoViewMargins(margin[0]!.Value, margin[1]!.Value); }
    public void UpdateCompassGravity(int v)             => _controller.SetCompassGravity(v);
    public void UpdateCompassViewMargins(int?[]? margin){ if (margin?.Length >= 2 && margin[0] != null && margin[1] != null) _controller.SetCompassViewMargins(margin[0]!.Value, margin[1]!.Value); }
    public void UpdateAttributionButtonGravity(int v)   => _controller.SetAttributionButtonGravity(v);
    public void UpdateAttributionButtonMargins(int?[]? margin) { if (margin?.Length >= 2 && margin[0] != null && margin[1] != null) _controller.SetAttributionButtonMargins(margin[0]!.Value, margin[1]!.Value); }
}

    
     public void UpdateStyleUrl(string styleUrl)
    {
        _styleUrl = styleUrl;
        _controller.SetStyleString(styleUrl);
    }
    
    public void UpdateMinMaxZoomPreference(double? minZoom, double? maxZoom)
    {
        _controller.SetMinMaxZoomPreference(minZoom, maxZoom);
    }
    
    public void UpdateRotateGestureEnabled(bool rotateGestureEnabled)
    {
        _controller.SetRotateGesturesEnabled(rotateGestureEnabled);
    }
    
    public void UpdateScrollGesturesEnabled(bool scrollGesturesEnabled)
    {
        _controller.SetScrollGesturesEnabled(scrollGesturesEnabled);
    }
    
    public void UpdateTiltGesturesEnabled(bool tiltGesturesEnabled)
    {
        _controller.SetTiltGesturesEnabled(tiltGesturesEnabled);
    }
    
    public void UpdateTrackCameraPosition(bool trackCameraPosition)
    {
        _controller.SetTrackCameraPosition(trackCameraPosition);
    }
    
    public void UpdateZoomGesturesEnabled(bool zoomGesturesEnabled)
    {
        _controller.SetZoomGesturesEnabled(zoomGesturesEnabled);
    }
    
    public void UpdateMyLocationEnabled(bool myLocationEnabled)
    {
        _controller.SetMyLocationEnabled(myLocationEnabled);
    }
    
    public void UpdateMyLocationTrackingMode(int myLocationTrackingMode)
    {
        _controller.SetMyLocationTrackingMode(myLocationTrackingMode);
    }
    
    public void UpdateMyLocationRenderMode(int myLocationRenderMode)
    {
        _controller.SetMyLocationRenderMode(myLocationRenderMode);
    }

    public void UpdateLogoViewMargins(int?[]? margin)
    {
        if (margin == null) return;
        var x = margin[0];
        var y = margin[1];
        if (x == null || y == null) return;
        _controller.SetLogoViewMargins((int) x, (int) y);
    }
    
    public void UpdateCompassGravity(int compassGravity)
    {
        _controller.SetCompassGravity(compassGravity);
    }
    
    public void UpdateCompassViewMargins(int?[]? margin)
    {
        if (margin == null) return;
        var x = margin[0];
        var y = margin[1];
        if (x == null || y == null) return;
        _controller.SetCompassViewMargins((int)x, (int)y);
    }
    
    public void UpdateAttributionButtonGravity(int attributionButtonGravity)
    {
        _controller.SetAttributionButtonGravity(attributionButtonGravity);
    }
    
    public void UpdateAttributionButtonMargins(int?[]? margin)
    {
        if (margin == null) return;
        var x = margin[0];
        var y = margin[1];
        if (x == null || y == null) return;
        _controller.SetAttributionButtonMargins((int)x, (int)y);
    }
}
