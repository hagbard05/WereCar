using Android.App;
using Android.Content;
using Android.OS;

using System.Runtime.Versioning;

namespace WearCar.Platforms.Android
{
	[SupportedOSPlatform("android21.0")]
	[BroadcastReceiver(Enabled = true, Exported = true, DirectBootAware = true)]
	[IntentFilter(new[] { Intent.ActionBootCompleted, "android.intent.action.QUICKBOOT_POWERON" })]
	public class BootReceiver : BroadcastReceiver
	{
		public override void OnReceive(Context? context, Intent? intent)
		{
			if (context == null) return;

			if (intent?.Action == Intent.ActionBootCompleted || intent?.Action == "android.intent.action.QUICKBOOT_POWERON")
			{
				try
				{
					var serviceIntent = new Intent(context, typeof(ParkingForegroundService));
					if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
					{
						context.StartForegroundService(serviceIntent);
					}
					else
					{
						context.StartService(serviceIntent);
					}
				}
				catch { }
			}
		}
	}
}
