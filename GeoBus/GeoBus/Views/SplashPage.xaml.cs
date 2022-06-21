using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace GeoBus.Views {
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SplashPage : ContentPage {
        public SplashPage() {
            InitializeComponent();
        }
        protected async override void OnAppearing() {
            base.OnAppearing();
            await Task.Delay(1000);
            await logoImg.TranslateTo(0, -100, 1500, Easing.BounceIn);
            await logoImg.TranslateTo(0, 0, 1000, Easing.BounceOut);
            nameText.IsVisible = true;
            await Task.Delay(1000);
            Application.Current.MainPage = new NavigationPage(new MainPage());
        }
    }
}