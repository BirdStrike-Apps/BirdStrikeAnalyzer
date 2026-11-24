using System.Text.Json;
using BirdStrikeAnalyzer.Model;

namespace BirdStrikeAnalyzer;

/// <summary>Shared Leaflet map surface backed by Geoapify map tiles.</summary>
public sealed class GeoapifyMapView : ContentView
{
    // Set this preference once during app setup (Preferences.Set("GeoapifyApiKey", "your-key")).
    public const string ApiKeyPreference = "GeoapifyApiKey";

    private readonly WebView _webView = new()
    {
        HorizontalOptions = LayoutOptions.Fill,
        VerticalOptions = LayoutOptions.Fill
    };

    public GeoapifyMapView()
    {
        Content = _webView;
        BackgroundColor = Colors.White;
        Show(Array.Empty<BuildingReportCount>());
    }

    public void Show(IReadOnlyList<BuildingReportCount> buildings)
    {
        var apiKey = Microsoft.Maui.Storage.Preferences.Get(ApiKeyPreference, string.Empty)?.Trim() ?? string.Empty;
        _webView.Source = new HtmlWebViewSource { Html = BuildHtml(apiKey, buildings) };
    }

    private static string BuildHtml(string apiKey, IReadOnlyList<BuildingReportCount> buildings)
    {
        var markers = buildings
            .Where(item => item.Latitude.HasValue && item.Longitude.HasValue)
            .Select(item => new MapMarker(item.Name, item.Latitude!.Value, item.Longitude!.Value, item.Count))
            .ToList();
        var markerJson = JsonSerializer.Serialize(markers);
        var keyJson = JsonSerializer.Serialize(apiKey);
        var hasKey = !string.IsNullOrWhiteSpace(apiKey);
        var centerLat = markers.Count > 0 ? markers.Average(item => item.Latitude) : 44.0244;
        var centerLon = markers.Count > 0 ? markers.Average(item => item.Longitude) : -88.5513;
        var zoom = markers.Count > 1 ? 16 : 15;

        if (!hasKey)
        {
            return """
                <!doctype html><html><head><meta name="viewport" content="width=device-width, initial-scale=1"></head>
                <body style="margin:0;height:100vh;display:grid;place-items:center;background:#f8fafc;font:14px sans-serif;color:#475569;text-align:center;padding:20px;box-sizing:border-box">
                <div><strong>Geoapify map is not configured.</strong><p>Add your API key to the app preference named <code>GeoapifyApiKey</code>.</p></div>
                </body></html>
                """;
        }

        return $$"""
            <!doctype html>
            <html><head><meta name="viewport" content="width=device-width, initial-scale=1, maximum-scale=1">
            <link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.4/dist/leaflet.css">
            <style>html,body,#map{height:100%;width:100%;margin:0} .leaflet-popup-content{font:14px sans-serif}</style>
            </head><body><div id="map"></div>
            <script src="https://unpkg.com/leaflet@1.9.4/dist/leaflet.js"></script>
            <script>
              const apiKey = {{keyJson}};
              const markers = {{markerJson}};
              const map = L.map('map').setView([{{centerLat}}, {{centerLon}}], {{zoom}});
              L.tileLayer(`https://maps.geoapify.com/v1/tile/osm-carto/{z}/{x}/{y}.png?&apiKey=${apiKey}`, {
                attribution: '© <a href="https://www.geoapify.com/">Geoapify</a> | © OpenStreetMap contributors',
                maxZoom: 20
              }).addTo(map);
              const bounds = [];
              const maxCount = Math.max(1, ...markers.map(item => item.Count));
              markers.forEach(item => {
                // Scale circle radius and color by all-time report volume.
                const intensity = Math.sqrt(Math.max(0, item.Count) / maxCount);
                const radius = 7 + intensity * 15;
                const color = intensity >= 0.70 ? '#dc2626' : intensity >= 0.35 ? '#f59e0b' : '#16a34a';
                const marker = L.circleMarker([item.Latitude, item.Longitude], {
                  radius,
                  color,
                  weight: 2,
                  opacity: 0.95,
                  fillColor: color,
                  fillOpacity: 0.38
                }).addTo(map);
                marker.bindPopup(`<strong>${escapeHtml(item.Name)}</strong><br>${item.Count} all-time report(s)`);
                bounds.push([item.Latitude, item.Longitude]);
              });
              if (bounds.length > 1) map.fitBounds(bounds, {padding:[24,24], maxZoom:17});
              function escapeHtml(value) { return String(value).replace(/[&<>"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c])); }
            </script></body></html>
            """;
    }

    private sealed record MapMarker(string Name, double Latitude, double Longitude, int Count);
}
