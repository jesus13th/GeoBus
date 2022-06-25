using System;

using Xamarin.Forms;
using Xamarin.Forms.GoogleMaps;

namespace GeoBus.Services {
    public static class Extensions {
        public static TimeSpan StripMilliseconds(this TimeSpan time) => new TimeSpan(time.Days, time.Hours, time.Minutes, time.Seconds);
        public static Position ToPosition(this (double lat, double lon) pos) => new Position(pos.lat, pos.lon);
        public static (double lat, double lon) PositionToTuple(this Position pos) => (pos.Latitude, pos.Longitude);
        public static Color Blend(this Color color, Color backColor, double amount) {
            byte r = (byte)(color.R * amount + backColor.R * (1 - amount));
            byte g = (byte)(color.G * amount + backColor.G * (1 - amount));
            byte b = (byte)(color.B * amount + backColor.B * (1 - amount));
            return Color.FromRgb(r, g, b);
        }
    }
}