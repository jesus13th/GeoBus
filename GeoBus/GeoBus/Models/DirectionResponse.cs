using System.Collections.Generic;

using Xamarin.Forms.GoogleMaps;

namespace GeoBus.Models {
    public class DirectionResponse {
        public string code { get; set; }
        public List<OtherRoute> routes { get; set; }
        public List<Waypoint> waypoints { get; set; }
    }
    public class OtherRoute {
        public string geometry { get; set; }
        public List<Leg> legs { get; set; }
        public string weight_name { get; set; }
        public double weight { get; set; }
        public double duration { get; set; }
        public double distance { get; set; }
    }
    public class Waypoint {
        public string hint { get; set; }
        public double distance { get; set; }
        public string name { get; set; }
        public List<double> location { get; set; }
    }
    public class Leg {
        public List<object> steps { get; set; }
        public string summary { get; set; }
        public double weight { get; set; }
        public double duration { get; set; }
        public double distance { get; set; }
    }
    public class LatLong {
        public double Lat { get; set; }
        public double Long { get; set; }
        public override string ToString() => $"Lat: {Lat}, Long: {Long}.";
        public Position GetPosition => new Position(Lat, Long);
    }
}