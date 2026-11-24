# BirdStrikeAnalyzer

A .NET 10 MAUI application with a Supabase-backed bird strike reports page.

## One-project structure

- `AppShell.cs` — Shell navigation.
- `BirdStrikesPage.xaml` and `BirdStrikesPage.xaml.cs` — report list, filtering, refresh, and confirmation UI.
- `Model/BusinessLogic.cs` — report filtering and application operations.
- `Model/Database.cs`, `Model/IDatabase.cs` — Supabase data access and interface.
- `Model/BirdStrikeSubmission.cs` — mapping for the existing `public.reporter_submissions` columns.
- `Model/SupabaseSettings.cs` — project URL and publishable key settings.

All layers are folders/namespaces within `BirdStrikeAnalyzer.csproj`; there are no separate layer projects. The app initializes Supabase only when a database operation is requested, so startup and navigation remain available before credentials or a backend are configured.

## Supabase setup

1. `Model/BirdStrikeSubmission.cs` maps `submission_id`, `date`, `time`, `reporter_id`, and `reporter_notes` from `public.reporter_submissions`.
2. The current RLS policy only allows authenticated users to see rows where `reporter_id = auth.uid()`. For the analyst page to see all submissions, assign `app_metadata.role = 'analyst'` to the authorized analyst Auth user and run `supabase/analyst_read_policy.sql` in the SQL Editor. The existing reporter policy stays in place; do not grant this access to `anon`.
3. Set the project URL and a **publishable** (or legacy anon) key in `Model/SupabaseSettings.cs`. The project URL is prefilled; the key is intentionally left as a placeholder. Never put a secret/service-role key in the client app.
4. Open **Bird Strikes**, sign in with an authorized Supabase Auth account, and the page will load reports allowed by RLS. Analyst-tagged accounts can see all submissions; other authenticated users see only their own rows. Filter by date/time, reporter ID, or notes; use **Refresh** to reload.

Do not put a Supabase secret/service-role key in a client application.

## Navigation

The Shell tab bar switches between Home and About. The Home page opens a registered Details route, with Shell back navigation returning to Home.

Build for Windows with `dotnet build BirdStrikeAnalyzer.csproj -f net10.0-windows10.0.19041.0` after restoring NuGet packages.

## Geoapify maps

The Home (`MainPage`) and expanded Map View (`MapViewPage`) now use the shared
`GeoapifyMapView`, which renders Geoapify raster tiles with Leaflet. Hotspot
coordinates from `building_doors.latitude` / `building_doors.longitude` are
preserved by `GetBuildingHotspotsAsync` and displayed as clickable building
markers. If a sample or older Supabase building row has no coordinates, the map
uses Geoapify forward geocoding against its known campus address (or the building
name plus the UW-Oshkosh campus) before placing the marker. The map uses Leaflet
circle markers rather than its default image icons so markers render reliably in
the Windows WebView2 control. The small status label at the bottom of the map
reports how many building locations were plotted and whether any failed.

Create an API key in your Geoapify account, then add it to the MAUI preferences
before opening either page:

```csharp
Preferences.Set(GeoapifyMapView.ApiKeyPreference, "YOUR_GEOAPIFY_API_KEY");
```

For example, this can be placed in the app's startup initialization in
`MauiProgram.CreateMauiApp()`. Do not commit a real API key to source control;
restrict the key to the app's expected usage in the Geoapify dashboard.
