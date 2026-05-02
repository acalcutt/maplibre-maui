#if ANDROID
    using AndroidLatLng = Org.Maplibre.Android.Geometry.LatLng;
    using AndroidLatLngBounds = Org.Maplibre.Android.Geometry.LatLngBounds;
#elif IOS
    using CoreLocation;
    using Org.Maplibre.MaciOS;
#endif

namespace Maui.MapLibre.Handlers.Geometry;

public class LatLngBounds(LatLng ne, LatLng sw)
{
    public LatLng NorthEast { get; set; } = ne;
    public LatLng SouthWest { get; set; } = sw;

    public object ToPlatform()
    {
#if ANDROID
        var ne = (AndroidLatLng) NorthEast.ToPlatform();
        var sw = (AndroidLatLng) SouthWest.ToPlatform();
        var bounds = new AndroidLatLngBounds.Builder().Build();
        bounds.Include(ne);
        bounds.Include(sw);
        return bounds;
#elif IOS
        return CFunctions.MLNCoordinateBoundsMake((CLLocationCoordinate2D)NorthEast.ToPlatform(), (CLLocationCoordinate2D)SouthWest.ToPlatform());
#else
        return this;
#endif
    }
}