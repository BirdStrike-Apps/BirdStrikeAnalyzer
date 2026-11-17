namespace BirdStrikeAnalyzer.Model;

/// <summary>A building's position on the campus map (0..1 of the map area) and its total bird report count.</summary>
public sealed record BuildingReportCount(string Name, float X, float Y, int Count);

public sealed record ReporterInfo(string Name, string Buildings);

/// <summary>One building's report counts for each month on the chart.</summary>
public sealed record MonthlySeries(string Building, string Color, int[] Values);

/// <summary>
/// Sample data for the Home page. Replace with values from BusinessLogic / Supabase when the
/// bird report tables exist.
/// </summary>
public static class HomeDashboardData
{
    public static IReadOnlyList<BuildingReportCount> Buildings { get; } =
    [
        new("Donner", 0.25f, 0.29f, 13),
        new("Webster", 0.40f, 0.43f, 6),
        new("Albee", 0.66f, 0.18f, 4),
        new("Reeve", 0.80f, 0.60f, 8),
        new("Taylor", 0.56f, 0.64f, 5),
        new("Kolf", 0.22f, 0.70f, 2),
        new("SRWC", 0.88f, 0.34f, 20),
    ];

    public static IReadOnlyList<ReporterInfo> Reporters { get; } =
    [
        new("James Anderson", "Swart, Halsey"),
        new("Bob Hawk", "Reeve, Polk"),
        new("Howard Smith", "Albee, Horizon, Kolf"),
        new("Jen Thomas", "SRWC, Parking Garage"),
        new("Leonard Williams", "Polk, SSC"),
    ];

    public static IReadOnlyList<(string Building, int Count)> BirdsPerMonth { get; } =
    [
        ("SRWC", 20),
        ("Horizon", 13),
        ("Halsey", 6),
        ("Polk", 5),
    ];

    public static IReadOnlyList<string> Months { get; } = ["Jun", "Jul", "Aug", "Sep", "Oct", "Nov"];

    public static IReadOnlyList<MonthlySeries> Series { get; } =
    [
        new("Halsey", "#1682D4", [1, 2, 5, 9, 7, 3]),
        new("Reeve", "#2FA38F", [3, 6, 4, 2, 6, 5]),
        new("Swart", "#F28B82", [2, 4, 6, 3, 8, 4]),
    ];
}
