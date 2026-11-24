using System.Text.Json;
using BirdStrikeAnalyzer.Model;

namespace BirdStrikeAnalyzer;

/// <summary>Shared Leaflet map surface backed by Geoapify map tiles and geocoding.</summary>
public sealed class GeoapifyMapView : ContentView
{
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
        Show(HomeDashboardData.Buildings);
    }

    public void Show(IReadOnlyList<BuildingReportCount> buildings)
    {
        var apiKey = Microsoft.Maui.Storage.Preferences.Get(ApiKeyPreference, string.Empty)?.Trim() ?? string.Empty;
        _webView.Source = new HtmlWebViewSource { Html = BuildHtml(apiKey, buildings) };
    }

    private static string BuildHtml(string apiKey, IReadOnlyList<BuildingReportCount> buildings)
    {
        // Keep rows even when coordinates are absent: sample rows and older Supabase rows
        // need geocoding before they can be plotted.
        var markers = buildings.Select(item => new MapMarker(
            item.Name,
            item.Latitude,
            item.Longitude,
            item.Count,
            GetKnownAddress(item.Name))).ToList();
        var markerJson = JsonSerializer.Serialize(markers);
        var keyJson = JsonSerializer.Serialize(apiKey);
        var hasKey = !string.IsNullOrWhiteSpace(apiKey);

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
              html,body,#map{height:100%;width:100%;margin:0;padding:0}
              .leaflet-popup-content{font:14px sans-serif}
              #map-status{position:absolute;z-index:1000;left:8px;bottom:8px;max-width:90%;padding:6px 9px;border-radius:4px;background:#fff;color:#334155;font:12px sans-serif;box-shadow:0 1px 5px #0003}
            </style></head>
            <body><div id="map"></div><div id="map-status">Loading building locations…</div>
            <script src="https://unpkg.com/leaflet@1.9.4/dist/leaflet.js"></script>
            <script>
              const apiKey = {{keyJson}};
              const buildings = {{markerJson}};
              const statusLabel = document.getElementById('map-status');
              const map = L.map('map').setView([44.0228, -88.5520], 16);
              L.tileLayer(`https://maps.geoapify.com/v1/tile/osm-carto/{z}/{x}/{y}.png?apiKey=${encodeURIComponent(apiKey)}`, {
                attribution: '© <a href="https://www.geoapify.com/">Geoapify</a> | © OpenStreetMap contributors',
                maxZoom: 20
              }).addTo(map);

              function escapeHtml(value) {
                return String(value ?? '').replace(/[&<>"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
              }

              async function resolveLocation(item) {
                const lat = Number(item.Latitude);
                const lon = Number(item.Longitude);
                if (Number.isFinite(lat) && Number.isFinite(lon) && Math.abs(lat) > 0.000001 && Math.abs(lon) > 0.000001) {
                  return [lat, lon];
                }

                // Demo data and older building_doors records may not yet contain coordinates.
                const searchText = item.Address || `${item.Name}, University of Wisconsin Oshkosh, Oshkosh, WI, USA`;
                const url = `https://api.geoapify.com/v1/geocode/search?text=${encodeURIComponent(searchText)}&limit=1&format=json&apiKey=${encodeURIComponent(apiKey)}`;
                const response = await fetch(url);
                if (!response.ok) throw new Error(`Geocoding returned HTTP ${response.status}`);
                const data = await response.json();
                const result = data.results && data.results[0];
                if (!result || !Number.isFinite(result.lat) || !Number.isFinite(result.lon)) return null;
                return [result.lat, result.lon];
              }

              async function addBuildingMarkers() {
                if (!Array.isArray(buildings) || buildings.length === 0) {
                  statusLabel.textContent = 'No building records were supplied to the map.';
                  return;
                }

                const bounds = [];
                let locatedCount = 0;
                let failedCount = 0;
                for (const item of buildings) {
                  try {
                    const point = await resolveLocation(item);
                    if (!point) { failedCount++; continue; }
                    // Circle markers avoid Leaflet's default PNG icon paths, which can fail
                    // when HTML is hosted in a Windows MAUI WebView.
                    L.circleMarker(point, {
                      radius: 9,
                      color: '#ffffff',
                      weight: 2,
                      fillColor: '#e34b4b',
                      fillOpacity: 0.95
                    }).addTo(map).bindPopup(`<strong>${escapeHtml(item.Name)}</strong><br>${Number(item.Count) || 0} report(s)`);
                    bounds.push(point);
                    locatedCount++;
                  } catch (error) {
                    console.error(`Unable to locate ${item.Name}`, error);
                    failedCount++;
                  }
                }

                if (bounds.length === 1) map.setView(bounds[0], 17);
                else if (bounds.length > 1) map.fitBounds(bounds, {padding:[28,28], maxZoom:17});

                if (locatedCount === 0) {
                  statusLabel.textContent = `No markers plotted (${buildings.length} building records). Check building coordinates or Geoapify geocoding access.`;
                } else {
                  statusLabel.textContent = `${locatedCount} building marker(s) shown${failedCount ? ` · ${failedCount} location(s) could not be resolved` : ''}.`;
                }
              }

              map.whenReady(() => setTimeout(() => map.invalidateSize(), 150));
              addBuildingMarkers();
            </script></body></html>
            """;
    }

    private static string? GetKnownAddress(string buildingName)
    {
        // Official UW-Oshkosh campus street addresses for the sample dashboard buildings.
        return buildingName.Trim().ToLowerInvariant() switch
        {
            "donner" => "820 High Ave, Oshkosh, WI 54901, USA",
            "webster" => "810 High Ave, Oshkosh, WI 54901, USA",
            "albee" => "776 Algoma Blvd, Oshkosh, WI 54901, USA",
            "reeve" => "748 Algoma Blvd, Oshkosh, WI 54901, USA",
            "taylor" => "780 High Ave, Oshkosh, WI 54901, USA",
            "kolf" => "785 High Ave, Oshkosh, WI 54901, USA",
            "srwc" => "735 Pearl Ave, Oshkosh, WI 54901, USA",
            _ => null
        };
    }

    private sealed record MapMarker(string Name, double? Latitude, double? Longitude, int Count, string? Address);
}
