using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Controls;

namespace WearCar.Services
{
	public class ParkingDetectorService : IAsyncDisposable
	{
		CancellationTokenSource? _cts;
		Task? _loopTask;
		Location? _prevLocation;
		private DateTimeOffset _prevTime;
		private bool _aboveThreshold;
		private DateTimeOffset _aboveSince;
		private bool _wasMoving;

		private const double MphThreshold = 5.0;
		readonly TimeSpan AboveDuration = TimeSpan.FromSeconds(30);
		private static readonly TimeSpan timeSpan = TimeSpan.FromSeconds(10);
		readonly TimeSpan BelowDuration = timeSpan;

		public event EventHandler<Location> ParkedLocationSaved;

		public void Start()
		{
			if (_loopTask != null)
				return;  

			_cts = new CancellationTokenSource();
			_loopTask = Task.Run(() => LoopAsync(_cts.Token));
		}

		public async Task StopAsync()
		{
			if (_cts == null)
				return;

			_cts.Cancel();
			try { await _loopTask; } catch { }
			_cts.Dispose();
			_cts = null;
			_loopTask = null;
		}

		async Task LoopAsync(CancellationToken token)
		{
			var request = new GeolocationRequest(GeolocationAccuracy.Best);
			while (!token.IsCancellationRequested)
			{
				try
				{
					var loc = await Geolocation.GetLocationAsync(request, token);
					if (loc != null)
					{
						var now = DateTimeOffset.UtcNow;
						double speedMph = await CalculateSpeedMph(loc, now);

						if (speedMph >= MphThreshold)
						{
							if (!_aboveThreshold)
							{
								_aboveThreshold = true;
								_aboveSince = now;
							}
							else
							{
								if (!_wasMoving && now - _aboveSince >= AboveDuration)
									_wasMoving = true;
							}
						}
						else
						{
							if (_aboveThreshold)
							{
								// device was above threshold previously; confirm it stays below for BelowDuration
								if (_wasMoving)
								{
									var lowStart = DateTimeOffset.UtcNow;
									bool remainedBelow = true;
									Location latest = loc;
									while (!token.IsCancellationRequested && (DateTimeOffset.UtcNow - lowStart) < BelowDuration)
									{
										await Task.Delay(1000, token);
										var l2 = await Geolocation.GetLocationAsync(request, token);
										if (l2 == null)
										{
											remainedBelow = false; break;
										}
										double s2 = await CalculateSpeedMph(l2, DateTimeOffset.UtcNow);
										if (s2 >= MphThreshold)
										{
											remainedBelow = false; break;
										}
										latest = l2;
									}

									if (remainedBelow)
								{
									SaveParked(latest);
								}
								}

								_aboveThreshold = false;
								_wasMoving = false;
							}
						}

						_prevLocation = loc;
						_prevTime = now;
					}
				}
				catch (OperationCanceledException)
				{
					break;
				}
				catch (Exception) { }

				try { await Task.Delay(1000, token); } catch { }
			}
		}

		async Task<double> CalculateSpeedMph(Location loc, DateTimeOffset now)
		{
			if (loc.Speed.HasValue)
				return loc.Speed.Value * 2.23693629; // m/s to mph

			if (_prevLocation != null && _prevTime != default)
			{
				var dt = (now - _prevTime).TotalSeconds;
				if (dt > 0)
				{
					var meters = HaversineInMeters(_prevLocation.Latitude, _prevLocation.Longitude, loc.Latitude, loc.Longitude);
					var mps = meters / dt;
					return mps * 2.23693629;
				}
			}
			return 0.0;
		}

		double HaversineInMeters(double lat1, double lon1, double lat2, double lon2)
		{
			const double R = 6371000; // meters
			double dLat = ToRad(lat2 - lat1);
			double dLon = ToRad(lon2 - lon1);
			double a = Math.Sin(dLat/2) * Math.Sin(dLat/2) + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) * Math.Sin(dLon/2) * Math.Sin(dLon/2);
			double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1-a));
			return R * c;
		}

		double ToRad(double deg) => deg * (Math.PI / 180);

		void SaveParked(Location loc)
		{
			try
			{
				Preferences.Set("ParkedLatitude", loc.Latitude);
				Preferences.Set("ParkedLongitude", loc.Longitude);
				Preferences.Set("ParkedTimestamp", DateTimeOffset.UtcNow.ToString("o"));

				ParkedLocationSaved?.Invoke(this, loc);

				MainThread.BeginInvokeOnMainThread(async () =>
				{
					try
					{
						if (Application.Current?.MainPage != null)
						{
							await Application.Current.MainPage.DisplayAlert("Parked Car Saved", $"Location saved at {loc.Latitude:F6}, {loc.Longitude:F6}", "OK");
						}
					}
					catch { }
				});
			}
			catch { }
		}

		public async ValueTask DisposeAsync()
		{
			await StopAsync();
		}
	}
}
