using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using System.Runtime.Versioning;
using WearCar.Services;

namespace WearCar.Platforms.Android
{
	[SupportedOSPlatform("android21.0")]
	[Service(ForegroundServiceType = ForegroundService.TypeLocation, Exported = false)]
	public class ParkingForegroundService : Service
	{
		private const int NotificationId = 1001;
		private const string ChannelId = "wearcar_parking_detector_channel";

		public override IBinder? OnBind(Intent? intent) => null;

		public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
		{
			CreateNotificationChannel();

			var notificationIntent = new Intent(this, typeof(MainActivity));
			var pendingIntent = PendingIntent.GetActivity(
				this,
				0,
				notificationIntent,
				PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

			var builder = new NotificationCompat.Builder(this, ChannelId)
				.SetContentTitle("Dude, Find My Car Active")
				.SetContentText("Monitoring location in background for automatic parking detection.")
				.SetSmallIcon(Resource.Mipmap.appicon)
				.SetOngoing(true)
				.SetContentIntent(pendingIntent);

			var notification = builder.Build();

			if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
			{
				StartForeground(NotificationId, notification, ForegroundService.TypeLocation);
			}
			else
			{
				StartForeground(NotificationId, notification);
			}

			// Ensure ParkingDetectorService is running
			try
			{
				var detector = IPlatformApplication.Current?.Services?.GetService<ParkingDetectorService>();
				detector?.Start();
			}
			catch { }

			return StartCommandResult.Sticky;
		}

		private void CreateNotificationChannel()
		{
			if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
			{
				var channelName = "Dude, Find My Car Parking Detector";
				var channel = new NotificationChannel(ChannelId, channelName, NotificationImportance.Low)
				{
					Description = "Keeps Dude, Find My Car parking detector running continuously in the background."
				};

				var manager = (NotificationManager?)GetSystemService(NotificationService);
				manager?.CreateNotificationChannel(channel);
			}
		}
	}
}
