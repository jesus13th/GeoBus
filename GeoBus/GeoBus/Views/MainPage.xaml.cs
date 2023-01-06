using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using GeoBus.Models;
using GeoBus.Services;

using Xamarin.CommunityToolkit.Extensions;
using Xamarin.CommunityToolkit.UI.Views.Options;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.GoogleMaps;
using Xamarin.Forms.Xaml;

using static GeoBus.Services.OSRMRouteService;
using static Xamarin.Forms.GoogleMaps.BitmapDescriptorFactory;

using Polyline = Xamarin.Forms.GoogleMaps.Polyline;

namespace GeoBus.Views {
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MainPage : ContentPage {
        #region Fields & Constructor
        private const string City = "Tepic";
        private Geocoder geocoder = new Geocoder();
        private OSRMRouteService osrm = new OSRMRouteService();
        private Pin currentLocationPin, destiantionPin, busStopPin, destinationStopPin = new Pin();
        private Polyline polylineToDestination, polylineToMe = new Polyline();
        public List<Route> Routes;
        private Route currentRoute;

        public MainPage() {
            InitializeComponent();
            BindingContext = this;
            aiLayout.IsVisible = true;
            Create();
        }
        #endregion

        #region UI Methods
        private async void searchBtn_Clicked(object sender, EventArgs e) {
            aiLayout.IsVisible = true;
            if (searchEntry.Text != string.Empty) {
                Location location = new Location();
                try {
                    location = (await Geocoding.GetLocationsAsync($"{ searchEntry.Text } { City }")).FirstOrDefault();
                } catch (Exception ex) {
                    SnackBarOptions snackBarOptions = new SnackBarOptions() { BackgroundColor = Color.Red, Duration = TimeSpan.FromMilliseconds(2000), MessageOptions = new MessageOptions() { Message = $"Error: {ex.Message}", Foreground = Color.White } };
                    await this.DisplayToastAsync("ha ocurrido un error y no se pudo realizar la busqueda :(, reintentalo");
                    aiLayout.IsVisible = false;
                    return;
                }
                string possibleAdress = (await geocoder.GetAddressesForPositionAsync(new Position(location.Latitude, location.Longitude))).FirstOrDefault();
                searchEntry.Text = possibleAdress.Split(',')[0];
                map.MoveToRegion(MapSpan.FromCenterAndRadius(new Position(location.Latitude, location.Longitude), Distance.FromKilometers(1)));
                map.Pins.Remove(destiantionPin);
                destiantionPin = new Pin() { Address = "Desitno", Label = "Destination", Position = new Position(location.Latitude, location.Longitude), Icon = FromBundle("pin_destination.png") };
                map.Pins.Add(destiantionPin);
                await GetNearestRoute((location.Latitude, location.Longitude));
            } else {
                routesList.IsVisible = false;
            }
            aiLayout.IsVisible = false;
        }
        private void viewRouteBtn_Clicked(object sender, EventArgs e) {
            if (busPicker.SelectedItem == null) return;
            DisplayRoute((string)busPicker.SelectedItem, Color.Black, true);
        }
        private void DisplayRoute(string bus, Color color, bool removePrevs) {
            var route = Routes.FirstOrDefault(r => r.RouteName == bus);
            if (removePrevs) {
                map.Polylines.Clear();
                map.MoveToRegion(MapSpan.FromCenterAndRadius(route.Center.ToPosition(), Distance.FromKilometers(1)));
            }
            var polyLine = new Polyline() { StrokeColor = color, StrokeWidth = 5 };

            foreach (var n in route.Nodes.Select((value, i) => new { i, value })) {
                polyLine.Positions.Add(n.value.ToPosition);
            }
            map.Polylines.Add(polyLine);
            polyLine.IsClickable = true;
            polyLine.Clicked += (s, e) => { map.Polylines.Remove(s as Polyline); };
        }
        private void routesList_ItemTapped(object sender, ItemTappedEventArgs e) {
            var route = e.Item as RouteItem;
            DisplayRoute(route.RouteName, route.RouteColor, true);
            GetBusStopNearToMe(route.RouteName);
            GetBusStopNearToDestination(route.RouteName);
        }
        #endregion

        #region Own Methods
        private async void Create() {
            do {
                try {
                    Routes = App.Instance.databaseRoutes.ReadAll().ToList();
                } catch (Exception ex) {
                    var snackBarOptions = new SnackBarOptions() { BackgroundColor = Color.Red, Duration = TimeSpan.FromMilliseconds(2000), MessageOptions = new MessageOptions() { Message = $"Error: {ex.Message}", Foreground = Color.White } };
                    await this.DisplaySnackBarAsync(snackBarOptions);
                    await this.DisplayAlert("Error", $"La aplicacion no se ha podido conectar a la red, verifica la conexion", "Ok");
                    return;
                }
            }
            while (Routes == null);

            busPicker.ItemsSource = Routes.Select(r => r.RouteName).ToList();

            var location = await Geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Default, TimeSpan.FromSeconds(5)));
            map.MoveToRegion(MapSpan.FromCenterAndRadius(new Position(location.Latitude, location.Longitude), Distance.FromKilometers(1)), false);
            if (currentLocationPin != null) map.Pins.Remove(currentLocationPin);
            currentLocationPin = new Pin() { Label = "Mi ubicacion", Icon = FromBundle("location_person.png"), Position = new Position(location.Latitude, location.Longitude), IsDraggable = false };
            map.Pins.Add(currentLocationPin);
            Device.StartTimer(TimeSpan.FromSeconds(3), () => { UpdateLocation(); return true; });
            aiLayout.IsVisible = false;
        }
        private async void UpdateLocation() {
            if (!App.Instance.IsSleep) {
                try {
                    Console.WriteLine("Actualiza posicion");
                    Location location = await Geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Default, TimeSpan.FromSeconds(5)));
                    currentLocationPin.Position = new Position(location.Latitude, location.Longitude);
                } catch (Exception ex) {
                    var snackBarOptions = new SnackBarOptions() { BackgroundColor = Color.Red, Duration = TimeSpan.FromMilliseconds(2000), MessageOptions = new MessageOptions() { Message = $"Error: {ex.Message}", Foreground = Color.White } };
                    await this.DisplaySnackBarAsync(snackBarOptions);
                }
            }
        }
        private async Task GetNearestRoute((double lat, double lon) location) {
            Dictionary<string, (double lat, double lon)> nearestRoutes = new Dictionary<string, (double lat, double lon)>();
            (double lat, double lon) nearestNode = default;
            const int routesCount = 2;
            var colors = new Color[] { Color.Red, Color.Green, Color.Blue };
            List<RouteItem> routeItems = new List<RouteItem>();

            map.Polylines.Clear();
            routesList.IsVisible = true;

            foreach (Route route in Routes) {
                double nearestDistance = double.MaxValue;
                foreach (RouteNode node in route.Nodes) {
                    var distance = HaversineDistance((location.lat, location.lon), (node.Latitude, node.Longitude));
                    if (distance < nearestDistance) {
                        nearestDistance = distance;
                        nearestNode = (node.Latitude, node.Longitude);
                    }
                }
                nearestRoutes.Add(route.RouteName, nearestNode);
            }
            var ordered = nearestRoutes.OrderBy(x => HaversineDistance((location.lat, location.lon), (x.Value.lat, x.Value.lon))).ToDictionary(x => x.Key, x => x.Value);
            Console.WriteLine(ordered);
            foreach (var route in ordered.Keys.Take(routesCount).Select((value, i) => (value, i))) {
                var durationToBusStop = (await GetRoute_OSRM(currentLocationPin, GetBusStopNear(route.value, currentLocationPin, out int startIndex).nearestNode.ToPosition().PositionToTuple())).duration;
                var durationToDestination = (await GetRoute_OSRM(destiantionPin, GetBusStopNear(route.value, destiantionPin, out int endIndex).nearestNode.ToPosition().PositionToTuple())).duration;
                var timeAutobus = Routes.FirstOrDefault(r => r.RouteName == route.value).Nodes.GetRange(startIndex, (endIndex < startIndex ? Routes.FirstOrDefault(r => r.RouteName == route.value).Nodes.Count : endIndex) - startIndex).Sum(r => r.Time.TotalSeconds);

                if (endIndex < startIndex)
                    timeAutobus += Routes[route.i].Nodes.GetRange(0, endIndex + 1).Sum(n => n.Time.TotalSeconds);

                routeItems.Add(new RouteItem() { RouteName = route.value, RouteColor = colors[route.i], TimeWalking = TimeSpan.FromSeconds(durationToBusStop + durationToDestination).StripMilliseconds(), TimeAutobus = TimeSpan.FromSeconds(timeAutobus)});
            }
            routesList.ItemsSource = routeItems;
            map.MoveToRegion(MapSpan.FromCenterAndRadius(location.ToPosition(), Distance.FromKilometers(1)));
        }
        private (Route route, (double, double) nearestNode) GetBusStopNear(string to, Pin pinTo, out int index) {
            var toLocation = pinTo.Position;
            (double lat, double lon) nearestNode = default;
            Route route = Routes.FirstOrDefault(r => r.RouteName == to);
            double nearestDistance = double.MaxValue;
            index = default;

            foreach (var node in route.Nodes.Select((value, i) =>  (value, i))) {
                var dist = HaversineDistance(toLocation.PositionToTuple(), (node.value.Latitude, node.value.Longitude));
                if (dist < nearestDistance) {
                    nearestDistance = dist;
                    nearestNode = (node.value.Latitude, node.value.Longitude);
                    index = node.i;
                }
            }
            return (route, nearestNode);
        }
        private async Task<(double duration, List<LatLong> locations)> GetRoute_OSRM(Pin from, (double lat, double lon) to) {
            var result = await osrm.GetDirectionResponseAsync(from.Position.PositionToTuple(), to);
            const double humanSpeed = 5;// 5Km/h
            const double velocity = (humanSpeed * 1000) / 3600;// meters/segs
            var duration = result.routes[0].distance / velocity;

            return (duration, DecodePolylinePoints(result.routes[0].geometry));
        }
        private async void GetBusStopNearToMe(string route) {
            var busStop = GetBusStopNear(route, currentLocationPin, out _);

            map.Pins.Remove(busStopPin);
            busStopPin = new Pin() { Label = $"Parada de autobus: {busStop.route.RouteName}.", Position = busStop.nearestNode.ToPosition(), Icon = FromBundle("BusStop.png") };
            map.Pins.Add(busStopPin);

            var osrm = await GetRoute_OSRM(currentLocationPin, busStopPin.Position.PositionToTuple());
            var _route = routesList.ItemsSource.Cast<RouteItem>().FirstOrDefault(r => r.RouteName == route);
            _route.TimeWalking = new TimeSpan(0, 5, 5);
            map.Polylines.Remove(polylineToMe);
            polylineToMe = new Polyline() { StrokeColor = Color.Black, StrokeWidth = 5 };
            osrm.locations.ForEach(l => polylineToMe.Positions.Add(new Position(l.Lat, l.Long)));
            map.Polylines.Add(polylineToMe);
        }
        private async void GetBusStopNearToDestination(string route) {
            var busStop = GetBusStopNear(route, destiantionPin, out _);

            map.Pins.Remove(destinationStopPin);
            destinationStopPin = new Pin() { Label = $"Bajada de autobus: {busStop.route.RouteName}.", Position = busStop.nearestNode.ToPosition(), Icon = FromBundle("Bus End.png") };
            map.Pins.Add(destinationStopPin);

            var osrm = await GetRoute_OSRM(destiantionPin, destinationStopPin.Position.PositionToTuple());
            map.Polylines.Remove(polylineToDestination);
            polylineToDestination = new Polyline() { StrokeColor = Color.Black, StrokeWidth = 5 };
            osrm.locations.ForEach(l => polylineToDestination.Positions.Add(new Position(l.Lat, l.Long)));
            map.Polylines.Add(polylineToDestination);
        }
        #endregion

        #region Developer
        Polyline dev_polyLineRegister;
        private async void RegisterBtn_Clicked(object sender, EventArgs e) {
            if (currentRoute == null) {
                currentRoute = new Route(RouteNameEntry.Text);
                RegisterBtn.Text = "Registrar Nodo";
                dev_polyLineRegister = new Polyline() { StrokeColor = Color.Red, StrokeWidth = 5 };
                return;
            }
            var location = await Geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Default, TimeSpan.FromMinutes(1)));//= currentLocationPin.Position;
            dev_polyLineRegister.Positions.Add(new Position(location.Latitude, location.Longitude));
            if (dev_polyLineRegister.Positions.Count >= 2 && !map.Polylines.Contains(dev_polyLineRegister))
                map.Polylines.Add(dev_polyLineRegister);

            currentRoute.InsertNode((location.Latitude, location.Longitude), out var time);
            LatitudEntry.Text = location.Latitude.ToString();
            LongitudEntry.Text = location.Longitude.ToString();
            TimeEntry.Text = time.ToString();
        }
        private async void FinishBtn_Clicked(object sender, EventArgs e) {
            await App.Instance.databaseRoutes.Create(currentRoute);
            await this.DisplayToastAsync($"Se registro el la ruta: { RouteNameEntry.Text }.", 1000);
            LatitudEntry.Text = "Latitud";
            LongitudEntry.Text = "Longitud";
            TimeEntry.Text = "Time";
            RegisterBtn.Text = "Registrar";
            RouteNameEntry.Text = "Nombre de la ruta";
            map.Polylines.Remove(dev_polyLineRegister);
            currentRoute = null;
        }
        #endregion
    }
}