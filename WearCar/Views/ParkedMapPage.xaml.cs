using System;
using System.ComponentModel;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Essentials;
using Microsoft.Maui.Devices.Sensors;
using WearCar.ViewModels;

namespace WearCar.Views
{
    public partial class ParkedMapPage : ContentPage
    {
        readonly ParkedMapViewModel _vm;
        CancellationTokenSource _cts = new CancellationTokenSource();

        bool _webViewLoaded = false;

        const string HtmlTemplate = @"<!DOCTYPE html>
<html>
<head>
  <meta name='viewport' content='width=device-width, initial-scale=1.0' />
  <link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.3/dist/leaflet.css'/>
  <style>html,body,#map{height:100%;margin:0;padding:0}</style>
</head>
<body>
<div id='map'></div>
<script src='https://unpkg.com/leaflet@1.9.3/dist/leaflet.js'></script>
<script>
  var map = L.map('map').setView([0,0],2);
  L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', { maxZoom: 19 }).addTo(map);
  var markers = {};
  function addMarker(id, lat, lon, label) {
    if (markers[id]) { markers[id].setLatLng([lat,lon]); }
    else { markers[id] = L.marker([lat,lon]).addTo(map).bindPopup(label); }
  }
  function setView(lat, lon, zoom) { map.setView([lat,lon], zoom); }
</script>
</body>
</html>";

        public ParkedMapPage(ParkedMapViewModel vm)
        {
            InitializeComponent();
            BindingContext = _vm = vm;
            _vm.PropertyChanged += Vm_PropertyChanged;

            MapWebView.Source = new HtmlWebViewSource { Html = HtmlTemplate };
            MapWebView.Navigated += MapWebView_Navigated;

            StartUserLocationLoop(_cts.Token);

            if (_webViewLoaded && _vm.ParkedLatitude.HasValue && _vm.ParkedLongitude.HasValue)
            {
                _ = AddOrUpdateParkedAsync(_vm.ParkedLatitude.Value, _vm.ParkedLongitude.Value);
            }
        }

        void Vm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_vm.ParkedLatitude) || e.PropertyName == nameof(_vm.ParkedLongitude))
            {
                if (_vm.ParkedLatitude.HasValue && _vm.ParkedLongitude.HasValue && _webViewLoaded)
                {
                    _ = AddOrUpdateParkedAsync(_vm.ParkedLatitude.Value, _vm.ParkedLongitude.Value);
                }
            }
        }

        void MapWebView_Navigated(object? sender, WebNavigatedEventArgs e)
        {
            _webViewLoaded = true;
            if (_vm.ParkedLatitude.HasValue && _vm.ParkedLongitude.HasValue)
            {
                _ = AddOrUpdateParkedAsync(_vm.ParkedLatitude.Value, _vm.ParkedLongitude.Value);
            }
        }

        async Task AddOrUpdateParkedAsync(double lat, double lon)
        {
            try
            {
                var sLat = lat.ToString(CultureInfo.InvariantCulture);
                var sLon = lon.ToString(CultureInfo.InvariantCulture);
                await MapWebView.EvaluateJavaScriptAsync($"addMarker('parked',{sLat},{sLon},'Parked Car'); setView({sLat},{sLon},16);");
            }
            catch { }
        }

        async Task AddOrUpdateUserAsync(double lat, double lon)
        {
            try
            {
                var sLat = lat.ToString(CultureInfo.InvariantCulture);
                var sLon = lon.ToString(CultureInfo.InvariantCulture);
                await MapWebView.EvaluateJavaScriptAsync($"addMarker('you',{sLat},{sLon},'You');");
            }
            catch { }
        }

        void StartUserLocationLoop(CancellationToken token)
        {
            _ = Task.Run(async () =>
            {
                var request = new GeolocationRequest(GeolocationAccuracy.Best);
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        var loc = await Geolocation.GetLocationAsync(request, token);
                        if (loc != null)
                        {
                            await MainThread.InvokeOnMainThreadAsync(async () =>
                            {
                                await AddOrUpdateUserAsync(loc.Latitude, loc.Longitude);

                                var parked = _vm.ParkedLatitude.HasValue && _vm.ParkedLongitude.HasValue;
                                if (!parked)
                                {
                                    var sLat = loc.Latitude.ToString(CultureInfo.InvariantCulture);
                                    var sLon = loc.Longitude.ToString(CultureInfo.InvariantCulture);
                                    await MapWebView.EvaluateJavaScriptAsync($"setView({sLat},{sLon},15);");
                                }
                            });
                        }
                    }
                    catch (OperationCanceledException) { break; }
                    catch { }

                    try { await Task.Delay(5000, token); } catch { break; }
                }
            }, token);
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            try { _cts.Cancel(); } catch { }
        }
    }
}
