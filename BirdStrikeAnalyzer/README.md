# ThreeTierMauiSample

A minimal .NET 10 MAUI application. It uses the structure of the supplied example's `Model` folder, without recreating its pages or domain workflow.

## One-project structure

- `MainPage.cs`, `AboutPage.cs`, `DetailsPage.cs`, `AppShell.cs` — UI and Shell navigation.
- `Model/BusinessLogic.cs` — application/business layer.
- `Model/Database.cs`, `Model/IDatabase.cs` — Supabase data-access implementation and interface.
- `Model/SampleItem.cs`, `Model/ObservableBaseModel.cs`, `Model/SupabaseSettings.cs` — Supabase model, common observable base, and client settings.

All layers are folders/namespaces within `BirdStrikeAnalyzer.csproj`; there are no separate layer projects. The app does not initialize Supabase during startup, so the home screen and navigation work before credentials or a backend are configured. The Connect button performs a sample PostgREST read on demand.

## Supabase setup

1. Run `supabase/schema.sql` in the Supabase SQL editor.
2. Set the project URL and publishable (or legacy anon) key in `Model/SupabaseSettings.cs`.
3. Build and launch the MAUI app, then select **Connect to Supabase**.

Do not put a Supabase secret/service-role key in a client application.

## Navigation

The Shell tab bar switches between Home and About. The Home page opens a registered Details route, with Shell back navigation returning to Home.

Build for Windows with `dotnet build BirdStrikeAnalyzer.csproj -f net10.0-windows10.0.19041.0` after restoring NuGet packages.
