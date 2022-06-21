using System;
using System.Collections.Generic;
using System.Linq;

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

namespace GeoBus.Views {
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MainPage : ContentPage {
        private const string City = "Tepic";
        private Geocoder geocoder = new Geocoder();
        private Pin currentLocationPin = new Pin();
        private Pin destiantionPin = new Pin();
        public List<Route> Routes;
        public List<Pin> tmpPins = new List<Pin>();
        private Route currentRoute;

        public MainPage() {
            InitializeComponent();
            BindingContext = this;
            aiLayout.IsVisible = true;
        }
        protected override async void OnAppearing() {
            base.OnAppearing();
            do {
                try {
                    Routes = (await App.Instance.databaseRoutes.ReadAll()).ToList();
                } catch (Exception ex) {
                    var snackBarOptions = new SnackBarOptions() { BackgroundColor = Color.Red, Duration = TimeSpan.FromMilliseconds(2000), MessageOptions = new MessageOptions() { Message = $"Error: {ex.Message}", Foreground = Color.White } };
                    await this.DisplaySnackBarAsync(snackBarOptions);
                    await this.DisplayAlert("Error", "Hubo un error al cargar la base de datos, reinicia la app", "Ok");
                    return;
                }
            }
            while (Routes == null);

            busPicker.ItemsSource = Routes.Select(r => r.RouteName).ToList();

            var location = await Geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Default, TimeSpan.FromMinutes(1)));
            map.MoveToRegion(MapSpan.FromCenterAndRadius(new Position(location.Latitude, location.Longitude), Distance.FromKilometers(1)), false);
            if (currentLocationPin != null)
                map.Pins.Remove(currentLocationPin);
            currentLocationPin = new Pin() { Label = "Mi ubicacion", Icon = FromBundle("location_person.png"), Position = new Position(location.Latitude, location.Longitude), IsDraggable = false };
            map.Pins.Add(currentLocationPin);
            Device.StartTimer(TimeSpan.FromSeconds(3), () => { UpdateLocation(); return true; });
            aiLayout.IsVisible = false;
        }
        private async void UpdateLocation() {
            if (!App.Instance.IsSleep) {
                try {
                    Location location = await Geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Default, TimeSpan.FromMinutes(1)));
                    currentLocationPin.Position = new Position(location.Latitude, location.Longitude);
                } catch (Exception ex) {
                    var snackBarOptions = new SnackBarOptions() { BackgroundColor = Color.Red, Duration = TimeSpan.FromMilliseconds(2000), MessageOptions = new MessageOptions() { Message = $"Error: {ex.Message}", Foreground = Color.White } };
                    await this.DisplaySnackBarAsync(snackBarOptions);
                }
            }
        }
        private async void searchBtn_Clicked(object sender, EventArgs e) {
            aiLayout.IsVisible = true;
            tmpPins.ForEach(p => map.Pins.Remove(p));
            if (searchEntry.Text != string.Empty) {
                Location location = new Location();
                try {
                    location = (await Geocoding.GetLocationsAsync($"{ searchEntry.Text } { City }")).FirstOrDefault();
                } catch (Exception ex) {
                    SnackBarOptions snackBarOptions = new SnackBarOptions() { BackgroundColor = Color.Red, Duration = TimeSpan.FromMilliseconds(2000), MessageOptions = new MessageOptions() { Message = $"Error: {ex.Message}", Foreground = Color.White } };
                    await this.DisplaySnackBarAsync(snackBarOptions);
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
                GetNearestRoute((location.Latitude, location.Longitude));
            }
            aiLayout.IsVisible = false;
        }
        private void GetNearestRoute((double lat, double lon) location) {
            Dictionary<string, (double lat, double lon)> nearestRoutes = new Dictionary<string, (double lat, double lon)>();
            (double lat, double lon) nearestNode = default;
            const int routesCount = 3;

            map.Polylines.Clear();
            tmpPins.ForEach(p => map.Pins.Remove(p));
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

            var colors = new Color[] { Color.Red, Color.Green, Color.Blue };
            var ordered = nearestRoutes.OrderBy(x => HaversineDistance((location.lat, location.lon), (x.Value.lat, x.Value.lon))).ToDictionary(x => x.Key, x => x.Value);
            List<RouteItem> routeItems = new List<RouteItem>();

            foreach (var route in ordered.Keys.Take(routesCount).Select((value, i) => (value, i))) {
                DisplayRoute(route.value, colors[route.i], false);
                routeItems.Add(new RouteItem() { RouteName = route.value, RouteColor = colors[route.i] });
                var pin = new Pin() { Label = $"Bajada de {route.value} mas cercana a tu destino.", Position = new Position(nearestRoutes[route.value].lat, nearestRoutes[route.value].lon), Icon = FromBundle("Bus End.png") };
                tmpPins.Add(pin);
                map.Pins.Add(pin);
            }
            routeItems.ForEach(item => Console.WriteLine($"name: {item.RouteName}, Color: {item.RouteColor}"));
            routesList.ItemsSource = routeItems;
            map.MoveToRegion(MapSpan.FromCenterAndRadius(location.ToPosition(), Distance.FromKilometers(1)));
            GetBusStopNearest(ordered.Keys.Take(routesCount).ToList());
        }
        private void GetBusStopNearest(List<string> routes) {
            var myCurrenLocation = currentLocationPin.Position;
            (double lat, double lon) nearestNode = default;

            foreach (Route route in Routes.Where(x => routes.Contains(x.RouteName))) {
                double nearestDistance = double.MaxValue;
                foreach (RouteNode node in route.Nodes) {
                    var distance = HaversineDistance((myCurrenLocation.Latitude, myCurrenLocation.Longitude), (node.Latitude, node.Longitude));
                    if (distance < nearestDistance) {
                        nearestDistance = distance;
                        nearestNode = (node.Latitude, node.Longitude);
                    }
                }
                Pin pin = new Pin() { Label = $"Para de autobus: {route.RouteName}.", Position = nearestNode.ToPosition(), Icon = FromBundle("BusStop.png") };
                tmpPins.Add(pin);
                map.Pins.Add(pin);
            }
        }
        private void viewRouteBtn_Clicked(object sender, EventArgs e) {
            tmpPins.ForEach(p => map.Pins.Remove(p));
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
            route.Nodes.ForEach(n => polyLine.Positions.Add(n.ToPosition));
            map.Polylines.Add(polyLine);
            polyLine.IsClickable = true;
            polyLine.Clicked += (s, e) => { map.Polylines.Remove(s as Polyline); };
        }
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