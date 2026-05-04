using Maui.MapLibre.Handlers.Geometry;

namespace Maui.MapLibre.Handlers;

public interface IMapLibreMapOptionsSink
{
    void SetCompassEnabled(bool compassEnabled);

    // TODO: styleString is not actually a part of options. consider moving
    void SetStyleString(string styleString);

    void SetMinMaxZoomPreference(double? min, double? max);

    void SetRotateGesturesEnabled(bool rotateGesturesEnabled);

    void SetScrollGesturesEnabled(bool scrollGesturesEnabled);

    void SetTiltGesturesEnabled(bool tiltGesturesEnabled);

    void SetTrackCameraPosition(bool trackCameraPosition);

    void SetZoomGesturesEnabled(bool zoomGesturesEnabled);

    void SetMyLocationEnabled(bool myLocationEnabled);

    void SetMyLocationTrackingMode(int myLocationTrackingMode);

    void SetMyLocationRenderMode(int myLocationRenderMode);

    void SetLogoViewMargins(int x, int y);

    void SetCompassGravity(int gravity);

    void SetCompassViewMargins(int x, int y);

    void SetAttributionButtonGravity(int gravity);

    void SetAttributionButtonMargins(int x, int y);

    //void SetLocationEngineProperties(LocationEngineRequest? locationEngineRequest);
}