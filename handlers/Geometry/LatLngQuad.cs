#if ANDROID
    using AndroidLatLng = Org.Maplibre.Android.Geometry.LatLng;
    using AndroidLatLngQuad = Org.Maplibre.Android.Geometry.LatLngQuad;
#elif IOS
using CoreLocation;
using Org.Maplibre.MaciOS;
#endif

namespace Maui.MapLibre.Handlers.Geometry;

public class LatLngQuad(LatLng topRight, LatLng topLeft, LatLng bottomRight, LatLng bottomLeft)
{
    public LatLng TopRight { get; set; } = topRight;
    public LatLng TopLeft { get; set; } = topLeft;
    public LatLng BottomRight { get; set; } = bottomRight;
    public LatLng BottomLeft { get; set; } = bottomLeft;

    public object? ToPlatform()
    {
#if ANDROID
        var topRight = (AndroidLatLng?) TopRight.ToPlatform();
        var topLeft = (AndroidLatLng?) TopLeft.ToPlatform();
        var bottomRight = (AndroidLatLng?) BottomRight.ToPlatform();
        var bottomLeft = (AndroidLatLng?) BottomLeft.ToPlatform();
        if (topRight == null || topLeft == null || bottomRight == null || bottomLeft == null) return null;
        return new AndroidLatLngQuad(topLeft, topRight, bottomRight, bottomLeft);
#elif IOS
        return CFunctions.MLNCoordinateQuadMake((CLLocationCoordinate2D)TopLeft.ToPlatform(), (CLLocationCoordinate2D)TopRight.ToPlatform(), (CLLocationCoordinate2D)BottomRight.ToPlatform(), (CLLocationCoordinate2D)BottomLeft.ToPlatform());
#else
        return null;
#endif
    }
}