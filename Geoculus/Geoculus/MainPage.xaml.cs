using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Maps;
using Xamarin.Forms.PlatformConfiguration;

namespace Geoculus
{

    public partial class MainPage : ContentPage
    {
        private class ResultsClass
        {
            public List<ElevationInfo> Results { get; set; }
        }

        private class ElevationInfo
        {
            public double Latitude { get; set; }
            public double Longtitude { get; set; }
            public double Elevation { get; set; }
        }
        private int Freq;
        private int Frame;
        private List<double> xS;
        private Polyline route;
        public MainPage()
        {
            InitializeComponent();
            
            xS = new List<double>();
            if (OrientationSensor.IsMonitoring)
                ;
            else
                OrientationSensor.Start(SensorSpeed.Default);
            if (Accelerometer.IsMonitoring)
                ;
            else
                Accelerometer.Start(SensorSpeed.Default);
            if (Compass.IsMonitoring)
                ;
            else
                Compass.Start(SensorSpeed.Default);
            Compass.ReadingChanged += Compass_ReadingChanged;
            Accelerometer.ReadingChanged += Accelerometer_ReadingChanged;
            OrientationSensor.ReadingChanged += OrientationSensor_ReadingChanged;
            Device.StartTimer(TimeSpan.FromSeconds(10), Callback );
        }

        private void Barometer_ReadingChanged(object sender, BarometerChangedEventArgs e)
        {
            Butt.Text = e.Reading.PressureInHectopascals.ToString();
        }

        private bool Callback()
        {

            UpdateLocation();
            return true;
        }

        private async Task UpdateLocation()
        {
            var l = await Geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Best));
            
            l.AltitudeReferenceSystem = AltitudeReferenceSystem.Geoid;
            if (route == null)
            {
                route = new Polyline();
                map.MapElements.Add(route);

            }
             
            if (route.Geopath.Count() > 2)
            {
                var distance = LocationExtensions.CalculateDistance(l, route.Geopath.Last().Latitude,
                    route.Geopath.Last().Longitude, DistanceUnits.Kilometers) * 1000;
                if (distance > 30)
                {
                    l.Latitude = route.Geopath.Last().Latitude +
                                 30d / distance * (l.Latitude - route.Geopath.Last().Latitude);
                    l.Longitude = route.Geopath.Last().Longitude +
                                  30d / distance * (l.Longitude - route.Geopath.Last().Longitude);
                }
            }

            route.Geopath.Add(new Position(l.Latitude, l.Longitude));
            Gay.Text = l.ToString();
            Butt.Text = l.Altitude.ToString();

            map.MapElements.Add(new Circle(){Center = new Position(l.Latitude, l.Longitude), Radius = new Distance(l.Accuracy.Value)});
            map.MoveToRegion(new MapSpan(new Position(l.Latitude, l.Longitude), 0.001, 0.001));
            HttpClient client = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage();
            request.RequestUri = new Uri($"https://api.open-elevation.com/api/v1/lookup?locations={l.Latitude.ToString("F6", CultureInfo.InvariantCulture)},{l.Longitude.ToString("F6", CultureInfo.InvariantCulture)}");
            request.Method = HttpMethod.Get;
            request.Headers.Add("Accept", "application/json");
            var response = await client.SendAsync(request);
            var str = await response.Content.ReadAsStringAsync();
            
            
            
            var elevation = JsonConvert.DeserializeObject<ResultsClass>(str).Results[0].Elevation;

            if (l.Altitude-16 - elevation > 6)
            {
                Gay.ForegroundColor = Color.Red;
            }
            else
            {
                Gay.ForegroundColor = Color.White;

            }
            HttpClient client2 = new HttpClient();
            HttpRequestMessage request2 = new HttpRequestMessage();
            try
            {
                request2.RequestUri = new Uri(
                    $"http://192.168.159.50:4444/send?lat={l.Latitude.ToString("F6", CultureInfo.InvariantCulture)}&lon={l.Longitude.ToString("F6", CultureInfo.InvariantCulture)}&elev={((l.Altitude.HasValue? l.Altitude.Value:16) - 16.0).ToString("F6", CultureInfo.InvariantCulture)}");


            

            request2.Method = HttpMethod.Get;
            var res = await client2.SendAsync(request2);
            }
            catch (Exception e)
            {

            }
            Butt.Text = l.Altitude.ToString() + " " + elevation;
        }

        private void OrientationSensor_ReadingChanged(object sender, OrientationSensorChangedEventArgs e)
        {
            var data = e.Reading.Orientation;
            double x, y, z = 0;
            var coef = Math.Sin(Math.Acos(data.W));
            xS.Add(data.X/coef);
            var test = xS;
            x = data.X ;
            y = data.Y ;
            z = data.Z ;
            Trans.Text = $"X:{x:F3} Y:{y:F3} Z:{z:F3}";
        }

        private void Accelerometer_ReadingChanged(object sender, AccelerometerChangedEventArgs e)
        {
            var data = e.Reading;
            Suck.Text = "";
        }

        private async void MainPage_OnAppearing(object sender, EventArgs e)
        {
            var l = await Geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Best));

            
            var placemark = new Placemark();
            placemark.Location = l;
            
            Gay.Text = placemark.ToString();
        }

        void Compass_ReadingChanged(object sender, CompassChangedEventArgs e)
        {
            var data = e.Reading;
            Ass.Text = data.HeadingMagneticNorth.ToString() + "\n";
        }

        private async void Button_OnClicked(object sender, EventArgs e)
        {
            await UpdateLocation();
        }
    }

}
