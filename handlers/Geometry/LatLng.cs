#if ANDROID
    using AndroidLatLng = Org.Maplibre.Android.Geometry.LatLng;
#elif IOS
    using CoreLocation;
    using Org.Maplibre.MaciOS;
#endif
    

    namespace Maui.MapLibre.Handlers.Geometry;

public class LatLng(double latitude, double longitude)
{
    public double Latitude { get; set; } = latitude;
    public double Longitude { get; set; } = longitude;

    public object ToPlatform()
    {
#if ANDROID
        return new AndroidLatLng(Latitude, Longitude) ;
#elif IOS
        return new CLLocationCoordinate2D(Latitude, Longitude);
#else
        return this;
#endif
    }
}