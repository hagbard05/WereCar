using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Essentials;
using WearCar.Services;

namespace WearCar.ViewModels
{
    public class CompassViewModel : INotifyPropertyChanged, IAsyncDisposable
    {
        readonly ParkingDetectorService _detector;
        CancellationTokenSource _cts = new CancellationTokenSource();

        double? _targetLat;
        double? _targetLon;
        double _arrowRotation; // degrees to rotate arrow (0 = north)
        string _distanceText = "--";
        double _arrowLength = 60; // UI length in device-independent pixels (will scale logarithmically with distance)
        string _currentSpeedText = "-- mph";

        // Speed smoothing for debug display
        private readonly Queue<double> _speedHistory = new Queue<double>(3);
        private const int SpeedHistorySize = 3;

        public event PropertyChangedEventHandler PropertyChanged;

        public string CurrentSpeedText
        {
            get => _currentSpeedText;
            private set { _currentSpeedText = value; OnPropertyChanged(); }
        }

        public double ArrowRotation
        {
            get => _arrowRotation;
            private set { _arrowRotation = value; OnPropertyChanged(); }
        }

        public double ArrowLength
        {
            get => _arrowLength;
            private set { _arrowLength = value; OnPropertyChanged(); }
        }

        public string DistanceText
        {
            get => _distanceText;
            private set { _distanceText = value; OnPropertyChanged(); }
        }

        public CompassViewModel(ParkingDetectorService detector)
        {
            _detector = detector ?? throw new ArgumentNullException(nameof(detector));
            _detector.ParkedLocationSaved += OnParkedLocationSaved;

            // populate from saved preferences at startup if present
            try
            {
                var lat = Preferences.Get("ParkedLatitude", double.NaN);
                var lon = Preferences.Get("ParkedLongitude", double.NaN);
                if (!double.IsNaN(lat) && !double.IsNaN(lon))
                {
                    _targetLat = lat;
                    _targetLon = lon;
                }
            }
            catch { }

            StartLoop(_cts.Token);
        }

        void OnParkedLocationSaved(object? sender, Microsoft.Maui.Devices.Sensors.Location loc)
        {
            if (loc == null) return;
            _targetLat = loc.Latitude;
            _targetLon = loc.Longitude;
        }

        public async Task EnsurePermissionsAsync()
        {
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.LocationAlways>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.LocationAlways>();
                }

                if (status != PermissionStatus.Granted)
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        if (Application.Current?.MainPage != null)
                        {
                            await Application.Current.MainPage.DisplayAlert("Permission required", "Background location access is required for continuous parking detection and compass pointer. Please enable Location (Always) in system settings.", "OK");
                        }
                    });
                }
            }
            catch { }
        }

        void StartLoop(CancellationToken token)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    Compass.ReadingChanged += Compass_ReadingChanged;
                    Compass.Start(SensorSpeed.UI);

                    var request = new GeolocationRequest(GeolocationAccuracy.Best);
                    while (!token.IsCancellationRequested)
                    {
                        try
                        {
                            var loc = await Geolocation.GetLocationAsync(request, token);
                            if (loc != null && _targetLat.HasValue && _targetLon.HasValue)
                            {
                                UpdateBearingAndDistance(loc.Latitude, loc.Longitude, _targetLat.Value, _targetLon.Value);
                            }
                        }
                        catch (OperationCanceledException) { break; }
                        catch { }

                        try { await Task.Delay(1500, token); } catch { break; }
                    }
                }
                finally
                {
                    try { Compass.Stop(); Compass.ReadingChanged -= Compass_ReadingChanged; } catch { }
                }
            }, token);
        }

        void Compass_ReadingChanged(object? sender, CompassChangedEventArgs e)
        {
            // heading in degrees relative to magnetic north
            var heading = e.Reading.HeadingMagneticNorth;
            _latestHeading = heading;
            if (_currentBearing.HasValue)
            {
                var rot = _currentBearing.Value - _latestHeading;
                rot = (rot + 360) % 360;
                ArrowRotation = rot;
            }
        }

        double _latestHeading = 0.0;
        double? _currentBearing = null;
        dynamic _lastLocation = null;
        DateTimeOffset _lastTime = default;

        void UpdateBearingAndDistance(double curLat, double curLon, double tgtLat, double tgtLon)
        {
            var bearing = BearingDegrees(curLat, curLon, tgtLat, tgtLon);
            _currentBearing = bearing;
            var distMeters = HaversineInMeters(curLat, curLon, tgtLat, tgtLon);
            var distText = distMeters >= 1000 ? (distMeters / 1000.0).ToString("0.0") + " km" : Math.Round(distMeters).ToString() + " m";
            DistanceText = distText;

            double speedMph = 0.0;
            if (_lastLocation != null && _lastTime != default)
            {
                var dt = (DateTimeOffset.UtcNow - _lastTime).TotalSeconds;
                if (dt > 0)
                {
                    var meters = HaversineInMeters(_lastLocation.Latitude, _lastLocation.Longitude, curLat, curLon);
                    var mps = meters / dt;
                    speedMph = mps * 2.23693629;
                }
            }

            // Apply moving average smoothing to reduce GPS noise
            _speedHistory.Enqueue(speedMph);
            if (_speedHistory.Count > SpeedHistorySize)
                _speedHistory.Dequeue();

            double smoothedSpeed = _speedHistory.Average();
            CurrentSpeedText = smoothedSpeed.ToString("0.0") + " mph";
            _lastLocation = new { Latitude = curLat, Longitude = curLon };
            _lastTime = DateTimeOffset.UtcNow;

            // Compute arrow length on a logarithmic scale so it grows quickly at short ranges then tapers off
            try
            {
                const double minLen = 40.0; // min arrow length (px)
                const double maxLen = 240.0; // max arrow length (px)
                // Use log10(dist + 10) so distances under ~10m still show growth and avoid negative/zero logs
                double valueForLog = Math.Max(0.0, distMeters);
                double len = minLen;
                if (valueForLog > 0.0)
                {
                    // scaleFactor controls how quickly the length grows; tuned empirically
                    double scaleFactor = 28.0;
                    len = minLen + Math.Log10(valueForLog + 10.0) * scaleFactor;
                }
                if (len < minLen) len = minLen;
                if (len > maxLen) len = maxLen;
                ArrowLength = len;
            }
            catch { }

            var rot = bearing - _latestHeading;
            rot = (rot + 360) % 360;
            ArrowRotation = rot;
        }

        double BearingDegrees(double lat1, double lon1, double lat2, double lon2)
        {
            var dLon = ToRad(lon2 - lon1);
            var y = Math.Sin(dLon) * Math.Cos(ToRad(lat2));
            var x = Math.Cos(ToRad(lat1)) * Math.Sin(ToRad(lat2)) - Math.Sin(ToRad(lat1)) * Math.Cos(ToRad(lat2)) * Math.Cos(dLon);
            var brng = Math.Atan2(y, x);
            brng = ToDeg(brng);
            return (brng + 360) % 360;
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
        double ToDeg(double rad) => rad * (180 / Math.PI);

        void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public async ValueTask DisposeAsync()
        {
            try { _cts.Cancel(); } catch { }
            await Task.CompletedTask;
        }
    }
}
