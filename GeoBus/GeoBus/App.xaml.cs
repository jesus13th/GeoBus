using GeoBus.Models;
using GeoBus.Services;
using GeoBus.Views;

using Xamarin.Forms;

[assembly: ExportFont("Heni Panjaitan.ttf", Alias = "CustomFont")]
namespace GeoBus {
    public partial class App : Application {
        public static App Instance { get; private set; }
        public FirebaseDatabaseService<Route> databaseRoutes = new FirebaseDatabaseService<Route>("Routes");
        public bool IsSleep { get; set; }

        public App() {
            InitializeComponent();
            Instance = this;
            MainPage = new NavigationPage(new SplashPage());
        }
        protected override void OnStart() => IsSleep = true;
        protected override void OnSleep() => IsSleep = false;
        protected override void OnResume() => IsSleep = true;
    }
}