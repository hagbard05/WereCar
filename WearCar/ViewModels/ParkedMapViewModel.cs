using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Devices.Sensors;
using System.Threading.Tasks;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using WearCar.Services;

namespace WearCar.ViewModels
{
    public class ParkedMapViewModel : INotifyPropertyChanged
    {
        readonly ParkingDetectorService _detector;

        double? _parkedLatitude;
        double? _parkedLongitude;

        public event PropertyChangedEventHandler PropertyChanged;

        public double? ParkedLatitude
        {
            get => _parkedLatitude;
            private set { _parkedLatitude = value; OnPropertyChanged(); }
        }

        public double? ParkedLongitude
        {
            get => _parkedLongitude;
            private set { _parkedLongitude = value; OnPropertyChanged(); }
        }

        public ParkedMapViewModel(ParkingDetectorService detector)
        {
            _detector = detector ?? throw new ArgumentNullException(nameof(detector));
            _detector.ParkedLocationSaved += OnParkedLocationSaved;

            // try to populate from saved preferences at startup
            try
            {
                var lat = Preferences.Get("ParkedLatitude", double.NaN);
                var lon = Preferences.Get("ParkedLongitude", double.NaN);
                if (!double.IsNaN(lat) && !double.IsNaN(lon))
                {
                    ParkedLatitude = lat;
                    ParkedLongitude = lon;
                }
                else
                {
                    // no parked location saved -- ask the user if we should use current location
                    MainThread.BeginInvokeOnMainThread(() => _ = InitializeInitialParkedAsync());
                }
            }
            catch { }
        }

        async Task InitializeInitialParkedAsync()
        {
            try
            {
                var page = Application.Current?.MainPage;
                if (page == null)
                    return;

                bool use = await page.DisplayAlert("Set parked location?", "No parked location found. Use current location as parked location?", "Yes", "No");
                if (!use)
                    return;

                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

                if (status == PermissionStatus.Granted)
                {
                    var request = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(10));
                    var loc = await Geolocation.GetLocationAsync(request);
                    if (loc != null)
                    {
                        Preferences.Set("ParkedLatitude", loc.Latitude);
                        Preferences.Set("ParkedLongitude", loc.Longitude);
                        Preferences.Set("ParkedTimestamp", DateTimeOffset.UtcNow.ToString("o"));

                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            ParkedLatitude = loc.Latitude;
                            ParkedLongitude = loc.Longitude;
                        });
                    }
                }
                else
                {
                    await page.DisplayAlert("Permission required", "Location permission is required to set the initial parked location.", "OK");
                }
            }
            catch { }
        }

        void OnParkedLocationSaved(object? sender, Microsoft.Maui.Devices.Sensors.Location loc)
        {
            if (loc == null) return;
            ParkedLatitude = loc.Latitude;
            ParkedLongitude = loc.Longitude;
        }

        void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
