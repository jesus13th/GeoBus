using System;

using Xamarin.Forms.GoogleMaps;

namespace GeoBus.Services {
    public static class Extensions {
        public static TimeSpan StripMilliseconds(this TimeSpan time) {
            return new TimeSpan(time.Days, time.Hours, time.Minutes, time.Seconds);
        }
        public static Position ToPosition(this (double lat, double lon) pos) => new Position(pos.lat, pos.lon);
    }
}