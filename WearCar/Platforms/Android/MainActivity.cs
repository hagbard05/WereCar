using Android.App;
using Android.Content.PM;
using Android.OS;
using System.Runtime.Versioning;
using WearCar.Platforms.Android;

namespace WearCar
{
	[SupportedOSPlatform("android21.0")]
	[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
	public class MainActivity : MauiAppCompatActivity
	{
		protected override void OnCreate(Bundle? savedInstanceState)
		{
			base.OnCreate(savedInstanceState);

			StartParkingForegroundService();
		}

		private void StartParkingForegroundService()
		{
			try
			{
				var intent = new Android.Content.Intent(this, typeof(Platforms.Android.ParkingForegroundService));
				if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
				{
					StartForegroundService(intent);
				}
				else
				{
					StartService(intent);
				}
			}
			catch { }
		}
	}
}
