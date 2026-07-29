using Microsoft.Extensions.Logging;
using Plugin.AdMob;
using WearCar.Services;
using WearCar.ViewModels;
using WearCar.Views;

namespace WearCar
{
	public static class MauiProgram
	{
		public static MauiApp CreateMauiApp()
		{
			var builder = MauiApp.CreateBuilder();
			builder
				.UseMauiApp<App>()
				.UseAdMob()
				.ConfigureFonts(fonts =>
				{
					fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
					fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
				});

#if DEBUG
			builder.Logging.AddDebug();
#endif

			// register background parking detector service and start it when app builds
			builder.Services.AddSingleton<ParkingDetectorService>(sp =>
			{
				var svc = new ParkingDetectorService();
				svc.Start();
				return svc;
			});

			// viewmodel and page registrations
			builder.Services.AddSingleton<ParkedMapViewModel>();
			builder.Services.AddTransient<ParkedMapPage>();
			// compass (new default startup view)			builder.Services.AddSingleton<CompassViewModel>();
			builder.Services.AddTransient<CompassPage>();

			return builder.Build();
		}
	}
}
