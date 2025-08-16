using System.Diagnostics;
using System.Text.Json;
using GuardianApplication.Services;
using Newtonsoft.Json.Linq;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;

namespace GuardianApplication
{
    public partial class MainPage : ContentPage
    {
        int count = 0;
        private string fastestRoute = string.Empty;
        private const string ORS_API_KEY = "eyJvcmciOiI1YjNjZTM1OTc4NTExMTAwMDFjZjYyNDgiLCJpZCI6ImE4Y2RhNzBjNjliYzQwYTk4ZDY3MmEzZTU3M2EwZTZlIiwiaCI6Im11cm11cjY0In0="; // 🔑 Replace this

        public MainPage()
        {
            InitializeComponent();
         //   _ = LoadMapWithRouteAsync();
        }

        private async void OnStartRecordingClicked(object sender, EventArgs e)
        {
            StatusLabel.Text = "Listening...";
            var transcription = await VoiceRecognitionService.GetTranscribedTextAsync();
            TranscriptLabel.Text = transcription;
            StatusLabel.Text = "Finished Listening";

            // Auto-dispatch if emergency keyword detected
            if (transcription.ToLower().Contains("help") || transcription.ToLower().Contains("emergency") || transcription.ToLower().Contains("danger") || transcription.ToLower().Contains("send"))
            {
                StatusLabel.Text = "Emergency keyword detected. Dispatching...";
                OnSendEmergencyClicked(this, EventArgs.Empty);
            }
        }
        private async void OnSendEmergencyClicked(object sender, EventArgs e)
        {
            var transcript = TranscriptLabel.Text;
            string userLocation = await GetCurrentLocationAsync();
            //fastestRoute = userLocation;
            string severity = await LanguageAnalysisService.EstimateSeverityAsync(transcript);


            string dispatchMessage = $"[DISPATCH] EMERGENCY ALERT\n"
                + $"Location: {userLocation}\n"
                + $"Severity: {severity}\n"
                + $"Description: {transcript}\n"
                + $"Fastest distance Route: {fastestRoute}";

            Debug.WriteLine(dispatchMessage);
            StatusLabel.Text = "Emergency request sent! Dispatcher alerted.";
        }
        private async Task<string> GetCurrentLocationAsync()
        {
            try
            {
                var location = await Geolocation.GetLastKnownLocationAsync();

                if (location == null)
                    location = await Geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium));

                if (location != null)
                {
                    // Example destination (Cape Town)
                    double destLat = -33.9249;
                    double destLon = 18.4241;

                    // Get fastest route from OpenRouteService
                    string fastestRoute = await GetOpenRouteAsync(location.Latitude, location.Longitude, destLat, destLon);

                    return $"GPS: Latitude:{location.Latitude}, Longitude:{location.Longitude}\nFastest Route: {fastestRoute}";
                }
                else
                {
                    return "Location unavailable";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Location error: {ex.Message}");
                return "Error getting location";
            }
        }

        public async Task<string> GetOpenRouteAsync(double startLat, double startLon, double endLat, double endLon)
        {
            
                string url = $"https://api.openrouteservice.org/v2/directions/driving-car?api_key={ORS_API_KEY}&start={startLat},{startLon}&end={endLat},{endLon}";

            try
            {
                using var httpClient = new HttpClient();
                var response = await httpClient.GetStringAsync(url);

                var json = JsonDocument.Parse(response);
                var summary = json.RootElement
                                  .GetProperty("features")[0]
                                  .GetProperty("properties")
                                  .GetProperty("summary");

                double distanceKm = summary.GetProperty("distance").GetDouble() / 1000; // meters → km
                double durationMin = summary.GetProperty("duration").GetDouble() / 60;  // sec → min

                return $"{distanceKm:F1} km, {durationMin:F0} min";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Route error: {ex.Message}");
                return "Route unavailable";
            }
        }
        /*private async Task LoadMapWithRouteAsync()
        {
            try
            {
                // ✅ Get current location
                var location = await Geolocation.GetLastKnownLocationAsync();
                if (location == null)
                    location = await Geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.High));

                if (location == null)
                {
                    await DisplayAlert("Error", "Unable to get location", "OK");
                    return;
                }

                var start = new Location(location.Latitude, location.Longitude);

                // ✅ Example destination (Cape Town)
                var destination = new Location(-33.9249, 18.4241);

                // ✅ Move camera
               
             //   MainMap.MoveToRegion(MapSpan.FromCenterAndRadius(start, Distance.FromKilometers(500)));

                // ✅ Add pins
             //   MainMap.Pins.Add(new Pin { Label = "You", Location = start, Type = PinType.Place });
             //   MainMap.Pins.Add(new Pin { Label = "Destination", Location = destination, Type = PinType.Place });

                // ✅ Fetch route from ORS
                var routeCoords = await GetRouteFromORSAsync(start, destination);

                if (routeCoords != null && routeCoords.Any())
                {
                    var routeLine = new Polyline
                    {
                        StrokeColor = Colors.Blue,
                        StrokeWidth = 5
                    };

                    foreach (var coord in routeCoords)
                    {
                        routeLine.Geopath.Add(coord);
                    }

                   // MainMap.MapElements.Add(routeLine);
                }
                else
                {
                    await DisplayAlert("Route", "Could not fetch route.", "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Map error: {ex.Message}");
            }
        }*/
       /* private async Task<List<Location>> GetRouteFromORSAsync(Location start, Location end)
        {
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("Authorization", ORS_API_KEY);

                var url = $"https://api.openrouteservice.org/v2/directions/driving-car?start={start.Longitude},{start.Latitude}&end={end.Longitude},{end.Latitude}";
                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                    return null;

                var json = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(json);
                var coords = doc.RootElement
                    .GetProperty("features")[0]
                    .GetProperty("geometry")
                    .GetProperty("coordinates");

                var routePoints = new List<Location>();

                foreach (var point in coords.EnumerateArray())
                {
                    double lon = point[0].GetDouble();
                    double lat = point[1].GetDouble();
                    routePoints.Add(new Location(lat, lon));
                }

                return routePoints;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Route error: {ex.Message}");
                return null;
            }
        }*/
    }
}
    

