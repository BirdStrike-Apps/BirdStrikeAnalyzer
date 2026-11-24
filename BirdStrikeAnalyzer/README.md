# BirdStrikeAnalyzer

A .NET 10 MAUI application using Supabase for bird-strike reports and Geoapify for campus maps.

## Supabase startup and configuration

- The Supabase client is initialized automatically when the Home page first appears. No Connect button or `sample_items` table is used.
- The app reads the existing `public.reporter_submissions`, `public.bird_strike_confirmations`, and `public.building_doors` tables.
- `bird_strike_confirmations.submission_id` joins to `reporter_submissions.submission_id`; the resulting `building_door_id` joins to `building_doors.building_door_id`.
- The project URL and publishable (or legacy anon) key are configured in `Model/SupabaseSettings.cs`. Never put a Supabase secret/service-role key in a client app.
- Supabase Row Level Security must permit the app's current role to SELECT the required rows. If the policies require an authenticated user, sign in through the Bird Strikes page; the app does not bypass RLS.
- No SQL scripts or additional tables are required by the app. The existing three tables and their appropriate RLS policies must already be present in Supabase.

## Map hotspots

The Home and Map View pages load live building data. Each circle represents one building; circle radius and color intensity increase with the number of rows in `bird_strike_confirmations` linked through `submission_id` to that building. The largest counts are red. Errors are shown in the page status rather than replaced with sample map data.

Geoapify is configured through the MAUI preference named `GeoapifyApiKey`. The map reads the configured preference at runtime.

## Build

For Windows, build with:

```powershell
dotnet build BirdStrikeAnalyzer.csproj -f net10.0-windows10.0.19041.0
```
