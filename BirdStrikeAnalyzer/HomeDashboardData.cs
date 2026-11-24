namespace BirdStrikeAnalyzer.Model;

/// <summary>One building's confirmed-strike count and geographic position.</summary>
public sealed record BuildingReportCount(string Name, float X, float Y, int Count, double? Latitude = null, double? Longitude = null);

/// <summary>Monthly report totals grouped by building for the dashboard chart.</summary>
public sealed record MonthlySeries(string Building, string Color, int[] Values);
