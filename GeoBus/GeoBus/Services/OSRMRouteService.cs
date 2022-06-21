using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

using GeoBus.Models;

using Newtonsoft.Json;

namespace GeoBus.Services {
    public class OSRMRouteService {
        private const double MilesToKilometers = 1.60934;
        private readonly string baseRouteURL = "http://router.project-osrm.org/route/v1/driving/";
        private HttpClient _client;
        public OSRMRouteService() {
            _client = new HttpClient();
        }
        public async Task<DirectionResponse> GetDirectionResponseAsync((double lat, double lon) origin, (double lat, double lon) destination) {
            string url = string.Format(baseRouteURL) + $"{origin.lon},{origin.lat};{destination.lon},{destination.lat}?overview=full&geometries=polyline&steps=false";
            var response = await _client.GetAsync(url);

            if (response.IsSuccessStatusCode) {
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<DirectionResponse>(json);
                return result;
            }
            return null;
        }

        public static double HaversineDistance((double lat, double lon) from, (double lat, double lon) to) {

            var R = 3958.8; // Radius of the Earth in miles
            var rlat1 = from.lat * (Math.PI / 180); // Convert degrees to radians
            var rlat2 = to.lat * (Math.PI / 180); // Convert degrees to radians
            var difflat = rlat2 - rlat1; // Radian difference (latitudes)
            var difflon = (to.lon - from.lon) * (Math.PI / 180); // Radian difference (longitudes)

            var d = 2 * R * Math.Asin(Math.Sqrt(Math.Sin(difflat / 2) * Math.Sin(difflat / 2) + Math.Cos(rlat1) * Math.Cos(rlat2) * Math.Sin(difflon / 2) * Math.Sin(difflon / 2)));
            return d * MilesToKilometers;
        }
        public static List<LatLong> DecodePolylinePoints(string encodedPoints) {
            if (encodedPoints == null || encodedPoints == "") return null;
            List<LatLong> poly = new List<LatLong>();
            char[] polylinechars = encodedPoints.ToCharArray();
            int index = 0;

            int currentLat = 0;
            int currentLng = 0;
            int next5bits;
            int sum;
            int shifter;

            try {
                while (index < polylinechars.Length) {
                    sum = 0;
                    shifter = 0;
                    do {
                        next5bits = (int)polylinechars[index++] - 63;
                        sum |= (next5bits & 31) << shifter;
                        shifter += 5;
                    } while (next5bits >= 32 && index < polylinechars.Length);

                    if (index >= polylinechars.Length)
                        break;

                    currentLat += (sum & 1) == 1 ? ~(sum >> 1) : (sum >> 1);

                    sum = 0;
                    shifter = 0;
                    do {
                        next5bits = (int)polylinechars[index++] - 63;
                        sum |= (next5bits & 31) << shifter;
                        shifter += 5;
                    } while (next5bits >= 32 && index < polylinechars.Length);

                    if (index >= polylinechars.Length && next5bits >= 32)
                        break;

                    currentLng += (sum & 1) == 1 ? ~(sum >> 1) : (sum >> 1);
                    LatLong p = new LatLong();
                    p.Lat = Convert.ToDouble(currentLat) / 100000.0;
                    p.Long = Convert.ToDouble(currentLng) / 100000.0;
                    poly.Add(p);
                }
            } catch { }
            return poly;
        }
    }
}