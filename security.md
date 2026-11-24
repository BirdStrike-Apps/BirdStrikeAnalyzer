# BirdStrikeAnalyzer security audit

**Reviewed:** 24 November 2026  
**Scope:** The source project and bundled Git history in `BirdStrikeAnalyzer`. This is a static source and configuration review; it did not modify the archive or test the live Supabase project.

## Assessment

I would **not release this build until the database access policies are reviewed and tested**. The client contains only a Supabase publishable key, which is appropriate for a client app, but that is safe only when every exposed table has the right grants and row-level security (RLS) policies. The ZIP does not contain enough database policy code to establish that. Supabase requires RLS and least-privilege grants on exposed tables; its guidance also recommends tests for anonymous and authenticated requests for each operation. [Supabase: securing data](https://supabase.com/docs/guides/database/secure-data), [Supabase: RLS](https://supabase.com/docs/guides/database/postgres/row-level-security)

## Findings

### P1 — Database authorization is not demonstrated for all tables and write operations

The app reads `reporter_submissions`, `bird_strike_confirmations`, `reporters`, and `building_doors`. It inserts confirmation rows and can insert or update building-door rows. However, the only included SQL policy, `supabase/analyst_read_policy.sql:5`, adds a **SELECT policy for `reporter_submissions` only**. It does not enable RLS, set grants, or define policies for the other three tables or for confirmation/building-door writes. The README says the remaining policies must already exist, but they are not included in the ZIP.

This does **not prove** the live database is open; its Supabase dashboard may have additional policies. It does mean the source archive cannot establish the security boundary. Because the publishable key ships with the client, anyone can call the Supabase Data API directly and bypass the app UI. Review and test policies for all four tables and each operation. In particular, allow broad reads and writes only to the analyst role, preserve reporter own-row access as needed, and deny anon access to reports and personal reporter data. The policy in the included SQL is permissive; another permissive policy can independently allow broader access.

Relevant code: `Model/Database.cs:13`, `Model/Database.cs:29`, `Model/Database.cs:67`, `Model/BusinessLogic.cs:28`, `Model/BusinessLogic.cs:142`.

Report: Rejected - Supabase has RLS enabled and policies added from the dashboard. However, We will keep an eye on this.

### P1 — Home queries sensitive data before the user signs in

Home calls `LoadDashboardAsync` as soon as it loads, then starts reads for pending reports, reporter assignments, building counts, monthly confirmations, and map hotspots. There is no authentication check before these calls. The explicit sign-in UI is on Bird Strikes, so the initial requests use the anonymous database role. `MainPage.cs:89`, `MainPage.cs:103`

With correct RLS, those anonymous requests should be denied and the dashboard will fail until an analyst signs in. If any table has an anonymous read grant/policy, report details or reporter names can be exposed before sign-in. Gate dashboard loading on an analyst session, while keeping RLS as the actual security enforcement.

Report: Approved - The app is currently designed solely for the client, however, a sign-in feature should be present at the beginning of the user's experience to ensure the data is secure.

### P1 — Building and door administration is available from the normal navigation

The Shell exposes **Buildings** and **Doors** without a sign-in or analyst-role gate. Those pages call add/edit operations in the database layer. `AppShell.cs:48`, `AppShell.cs:49`, `BuildingManagementPage.cs:194`, `DoorManagementPage.cs:315`

Hiding these screens from non-admin users improves the app’s behavior, but is not a security control by itself: an attacker can call the API directly. Enforce the admin/analyst role and field-level write permissions in PostgreSQL RLS/grants. Add database constraints for valid coordinates, required names, and unique doors so direct API calls cannot bypass client-side validation.

Report: Approved - The app is currently designed solely for the client, however, a sign-in feature should be present at the beginning of the user's experience to ensure the data is secure. This would allow database editing access to be removed from non-admin accounts.

### P2 — The app downloads broad datasets, including reporter details and notes

Database methods issue unprojected `.Get()` calls. The business layer downloads entire tables and joins them in memory, including reporter names and submission notes. `Model/Database.cs:13`, `Model/Database.cs:53`, `Model/Database.cs:60`, `Model/BusinessLogic.cs:28`

An analyst session can therefore retrieve much more data than a single page needs, and compromise of that session enables bulk extraction. Restrict returned columns, query and paginate on the server, and avoid returning notes or reporter identity to screens that do not need them. Confirm the analyst role is intentionally allowed to see all records.

Report: Rejected - The analyst should be able to see all records and all information within each record.

### P2 — Confirmation is a check-then-insert and does not record the confirming user

`ConfirmBirdStrikeSubmissionAsync` first checks whether a confirmation exists and then inserts one. Two concurrent requests can both pass the check. Whether that creates duplicate confirmations depends on a database uniqueness constraint that is not supplied. The insert also leaves `entry_confirmed_by` unset, so the data cannot identify the actor from this code. `Model/Database.cs:67`, `Model/BirdStrikeConfirmation.cs:9`

Use a database uniqueness constraint and an atomic upsert/RPC, and set the confirming user from the verified JWT (`auth.uid()` or a trusted database trigger), not from a client-supplied identity.

Report: Approved - The use of accounts will allow for `entry_confirmed_by` to be correctly populated. We will determine if duplicate confirmations are made when this is done.

### P2 — Map WebView executes Leaflet code from an unverified CDN

The map embeds remote CSS and JavaScript from `unpkg.com` without Subresource Integrity, a restrictive Content Security Policy, or local vendoring. `GeoapifyMapView.cs:57`, `GeoapifyMapView.cs:60`

That script runs in the map WebView and can read the embedded hotspot data and map key. A compromised or replaced CDN asset could exfiltrate that data or alter the map. Bundle a reviewed, pinned Leaflet copy locally; if remote assets are retained, add integrity checks and restrict WebView navigation and network origins. OWASP recommends SRI for third-party CDN assets. [OWASP ASVS dependency requirements](https://wiki.owasp.org/images/d/d4/OWASP_Application_Security_Verification_Standard_4.0-en.pdf)

Report: Approved - The app should check that the retrieved content is correct and unmodified.

### P2 — Geoapify key is shipped to every client and stored in Preferences

`MauiProgram.cs` writes a Geoapify key-shaped value to MAUI Preferences, and `GeoapifyMapView` injects it into page JavaScript and map-tile URLs. `MauiProgram.cs:27`, `GeoapifyMapView.cs:27`, `GeoapifyMapView.cs:38`, `GeoapifyMapView.cs:65`

Treat this as a public browser/client key: extracting it is expected. Restrict it to the required map APIs and quota/usage limits, monitor abuse, and rotate it if those restrictions cannot be applied. The value in this archive appears partially masked, so I could not verify whether it is operational. Do not move a private provider secret into Preferences; a client cannot keep it secret.

Report: Approved - The key needs to be kept secret and secure. The API access should also be capped to limit abuse.

### P2 — No sign-out, session lock, or idle timeout is visible

The app signs in through the Bird Strikes page but has no sign-out action or session lock. `AutoRefreshToken` is enabled in the shared Supabase client. A signed-in analyst who leaves the app open leaves that session available to the next person using the device. `BirdStrikesPage.xaml.cs:34`, `Model/Database.cs:102`

Add sign-out and an idle lock for shared desktops. The Supabase C# SDK documents that its default session handler does not persist sessions across process exit, which reduces at-rest exposure, but does not protect an unattended running app. [Supabase C# session persistence](https://github.com/supabase/supabase-csharp/blob/master/packages/Supabase/README.md)

Report: Approved - The user should be able to sign out of the app to be able to change accounts.

### P3 — Raw backend exception messages are shown to users

Sign-in, report loading, dashboard loading, and confirmation handlers display `ex.Message` directly. `BirdStrikesPage.xaml.cs:46`, `BirdStrikesPage.xaml.cs:109`, `MainPage.cs:130`, `MainPage.cs:222`

Backend messages can reveal table names, policy failures, or implementation details. Show users a generic message and keep diagnostic details in a protected, sanitized telemetry channel. Do not log passwords, tokens, or full report contents.

Report: Approved - These exceptions should be changed to not reveal potentially sensitive information to the user.

### P3 — Android app-data backup is enabled

The Android manifest explicitly sets `android:allowBackup="true"`. Android Auto Backup includes shared preferences by default. The current app stores a public map key there, and the reviewed Supabase C# client defaults to not persisting its session, so I did not find evidence that a private credential is currently being backed up. Still, exclude any future auth/session storage and private data from backup, or disable backup if the app does not need it. `Platforms/Android/AndroidManifest.xml:3` [Android Auto Backup](https://developer.android.com/identity/data/autobackup)

Report: Approved - Storing unnecessary data is dangerous, so we will update this.

### P2 — Dependency vulnerability status is unverified

The project pins Supabase 8.9.0 but has no checked-in lock file or dependency vulnerability workflow. I restored dependencies, but `dotnet list package --vulnerable --include-transitive` could not reach NuGet’s vulnerability feed in this environment. Therefore, this review does not certify the dependency tree as vulnerability-free. Add a CI vulnerability scan/lock file and resolve its findings before release. `BirdStrikeAnalyzer.csproj:69`

Report: Approved - The dependency tree needs to be checked to see if it is vulnerability-free.

## Items reviewed that are not findings

- The embedded Supabase key uses the `sb_publishable_` prefix. That key is designed to ship in client apps; it is **not** a database authorization control. It is acceptable only with strict RLS and least-privilege grants. Do not rotate it solely because it appears in source; rotate any secret/service-role key if one is ever found. [Supabase key guidance](https://supabase.com/docs/guides/database/secure-data)
- The Supabase URL uses HTTPS, and the app uses the publishable key rather than a service-role key in the checked source.
- Map marker names are JSON-serialized and escaped before being written into Leaflet popups; I did not find a direct user-controlled JavaScript/HTML injection path in that code.

## Release checklist

1. Inspect the live Supabase project’s RLS, grants, triggers, and constraints for all four tables. Test anon, normal authenticated, and analyst requests for read/insert/update/delete. Keep those tests as SQL under `supabase/tests/` and run them in CI.
2. Gate Home and administrative UI behind authentication/role checks; retain server-side RLS as the enforcement boundary.
3. Reduce returned columns and rows, make confirmations atomic and attributable, and add sign-out/idle locking.
4. Vendor or integrity-check Leaflet and constrain the Geoapify key.
5. Run the dependency vulnerability scan with network access and review every reported package.

The highest-priority uncertainty is the live database: it was not included in the ZIP, so its actual RLS and grants could not be verified. A source-only audit cannot establish whether production data is currently exposed.

