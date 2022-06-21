using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Gms.Ads;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using GeoBus.Views;

using static Java.Util.ResourceBundle;

using Xamarin.Forms.Platform.Android;
using GeoBus.Droid;
using Xamarin.Forms;

[assembly: ExportRenderer(typeof(AdMobView), typeof(AdMobViewRenderer))]
namespace GeoBus.Droid {
	public class AdMobViewRenderer : ViewRenderer<AdMobView, AdView> {
		public AdMobViewRenderer(Context context) : base(context) { }
        protected override void OnElementChanged(ElementChangedEventArgs<AdMobView> e) {
			base.OnElementChanged(e);

			if (e.NewElement != null && Control == null)
				SetNativeControl(CreateAdView());
		}

		protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e) {
			base.OnElementPropertyChanged(sender, e);

			if (e.PropertyName == nameof(AdView.AdUnitId))
				Control.AdUnitId = Element.AdUnitId;
		}

		private AdView CreateAdView() {
			var adView = new AdView(Context) {
				AdSize = AdSize.SmartBanner,
				AdUnitId = Element.AdUnitId
			};

			adView.LayoutParameters = new LinearLayout.LayoutParams(LayoutParams.MatchParent, LayoutParams.MatchParent);

			adView.LoadAd(new AdRequest.Builder().Build());

			return adView;
		}
	}
}