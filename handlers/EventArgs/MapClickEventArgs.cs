using MapLibreNative.Maui.Handlers.Geometry;

namespace MapLibreNative.Maui.Handlers.EventArgs;

public class MapClickEventArgs : System.EventArgs
{
    public LatLng LatLng { get; set; }
    /// <summary>Physical screen X of the tap/click (set on platforms that support it).</summary>
    public double ScreenX { get; set; }
    /// <summary>Physical screen Y of the tap/click (set on platforms that support it).</summary>
    public double ScreenY { get; set; }
}