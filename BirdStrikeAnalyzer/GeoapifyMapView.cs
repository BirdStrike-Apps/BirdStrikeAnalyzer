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
            <style>
              html,body,#map{height:100%;width:100%;margin:0}
              .leaflet-popup-content{font:14px sans-serif}
              .hotspot-legend{background:rgba(255,255,255,.96);padding:9px 11px;border-radius:8px;
                box-shadow:0 1px 6px rgba(0,0,0,.22);font:12px/1.5 sans-serif;color:#263238}
              .hotspot-legend .scale{display:flex;align-items:center;gap:7px;margin-top:4px}
              .hotspot-legend .gradient{width:105px;height:9px;border-radius:6px;
                background:linear-gradient(90deg,#2b83ba,#abdda4,#fdae61,#d7191c)}
            </style>
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
              const maxCount = Math.max(1, ...markers.map(item => Number(item.Count) || 0));
              markers.forEach(item => {
                const count = Math.max(0, Number(item.Count) || 0);
                const intensity = Math.sqrt(count / maxCount);
                const radius = 7 + intensity * 21;
                const color = hotspotColor(intensity);
                const marker = L.circleMarker([item.Latitude, item.Longitude], {
                  radius,
                  color,
                  weight: 2,
                  opacity: 0.95,
                  fillColor: color,
                  fillOpacity: count === 0 ? 0.18 : 0.55
                }).addTo(map);
                marker.bindPopup(`<strong>${escapeHtml(item.Name)}</strong><br>${count} confirmed bird-strike report(s)`);
                marker.bindTooltip(`${escapeHtml(item.Name)}: ${count}`, {direction:'top', offset:[0,-radius]});
                bounds.push([item.Latitude, item.Longitude]);
              });
              const legend = L.control({position:'bottomright'});
              legend.onAdd = () => {
                const div = L.DomUtil.create('div', 'hotspot-legend');
                div.innerHTML = '<strong>Confirmed bird strikes</strong><div class="scale"><span>Fewer</span><span class="gradient"></span><span>More</span></div><div>Circle size also increases with count</div>';
                return div;
              };
              legend.addTo(map);
              if (bounds.length > 1) map.fitBounds(bounds, {padding:[32,32], maxZoom:17});
              function hotspotColor(value) {
                const stops = [[43,131,186],[171,221,164],[253,174,97],[215,25,28]];
                const scaled = Math.max(0, Math.min(1, value)) * (stops.length - 1);
                const index = Math.min(stops.length - 2, Math.floor(scaled));
                const part = scaled - index;
                const rgb = stops[index].map((channel, i) => Math.round(channel + (stops[index + 1][i] - channel) * part));
                return `rgb(${rgb.join(',')})`;
              }
              function escapeHtml(value) { return String(value).replace(/[&<>"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c])); }
            </script></body></html>
            """;
    }

    private sealed record MapMarker(string Name, double Latitude, double Longitude, int Count);
}
