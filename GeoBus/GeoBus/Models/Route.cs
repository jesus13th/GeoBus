using System;
using System.Collections.Generic;
using System.Linq;

using GeoBus.Services;

using Xamarin.Forms;
using Xamarin.Forms.GoogleMaps;

namespace GeoBus.Models {
    public class Route {
        public string RouteName { get; set; }
        public List<RouteNode> Nodes { get; set; }
        private TimeSpan startTime;
        public Route(string routeName) {
            RouteName = routeName;
            Nodes = new List<RouteNode>();
        }
        public void InsertNode((double latitude, double longitude) location, out TimeSpan time) {
            if (Nodes.Count == 0)
                startTime = DateTime.Now.TimeOfDay;
            time = (DateTime.Now.TimeOfDay - startTime).StripMilliseconds();
            startTime = DateTime.Now.TimeOfDay;
            Nodes.Add(new RouteNode() { Latitude = location.latitude, Longitude = location.longitude, Time = time });
        }

        public (double lat, double lon) Center => (Nodes.Select(n => n.Latitude).Average(), Nodes.Select(n => n.Longitude).Average());
    }
    public class RouteNode {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public TimeSpan Time { get; set; }
        public Position ToPosition => new Position(Latitude, Longitude);
    }
    public class RouteItem {
        public string RouteName { get; set; }
        public Color RouteColor { get; set; }
    }
}