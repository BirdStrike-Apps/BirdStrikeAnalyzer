using BirdStrikeAnalyzer;

namespace BirdStrikeAnalyzer.Model;

/// <summary>Business rules and application operations. UI pages depend on this class, not on the Supabase client.</summary>
public sealed class BusinessLogic(IDatabase database)
{
    public bool IsAuthenticated => database.IsAuthenticated;
    public Task<IReadOnlyList<SampleItem>> GetSampleItemsAsync(CancellationToken cancellationToken = default) =>
        database.GetSampleItemsAsync(cancellationToken);

    public Task SignInAsync(string email, string password, CancellationToken cancellationToken = default) =>
        database.SignInAsync(email, password, cancellationToken);

    public async Task<IReadOnlyList<BirdStrikeSubmission>> GetBirdStrikeSubmissionsAsync(
        DateTime? start, DateTime? end, string? reporterId, string? notes,
        CancellationToken cancellationToken = default)
    {
        var submissions = await database.GetBirdStrikeSubmissionsAsync(cancellationToken);
        var reporterQuery = reporterId?.Trim();
        var notesQuery = notes?.Trim();

        return submissions
            .Where(item => start is null || item.ReportDate.Date + item.ReportTime >= start.Value)
            .Where(item => end is null || item.ReportDate.Date + item.ReportTime <= end.Value)
            .Where(item => string.IsNullOrWhiteSpace(reporterQuery) || (item.ReporterId?.ToString() ?? string.Empty).Contains(reporterQuery, StringComparison.OrdinalIgnoreCase))
            .Where(item => string.IsNullOrWhiteSpace(notesQuery) || (item.ReporterNotes ?? string.Empty).Contains(notesQuery, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(item => item.ReportDate.Date + item.ReportTime)
            .ToList();
    }

    public async Task<IReadOnlyList<AnalyzerReport>> GetAnalyzerReportsAsync(CancellationToken cancellationToken = default)
    {
        var submissions = await database.GetBirdStrikeSubmissionsAsync(cancellationToken);
        var doors = await database.GetBuildingDoorsAsync(cancellationToken);
        var doorsById = doors.ToDictionary(door => door.BuildingDoorId);

        return submissions.Select(submission =>
        {
            doorsById.TryGetValue(submission.BuildingDoorId, out var door);
            var reportedAt = submission.ReportDate.Date + submission.ReportTime;
            return new AnalyzerReport
            {
                Id = submission.SubmissionId,
                Reporter = submission.ReporterId is { } reporterId ? reporterId.ToString("N")[..8] : "Anonymous",
                Building = door?.BuildingName ?? "Unknown building",
                Door = door is null ? "Unknown door" : $"Door {door.Door}",
                ReportedAt = new DateTimeOffset(DateTime.SpecifyKind(reportedAt, DateTimeKind.Local)),
                Notes = submission.ReporterNotes ?? string.Empty,
                IsConfirmed = submission.IsConfirmed
            };
        }).OrderByDescending(report => report.ReportedAt).ToList();
    }

    public Task ConfirmReportAsync(Guid reportId, CancellationToken cancellationToken = default) =>
        database.ConfirmBirdStrikeSubmissionAsync(reportId, cancellationToken);

    public async Task<IReadOnlyList<BuildingReportCount>> GetBuildingHotspotsAsync(CancellationToken cancellationToken = default)
    {
        var submissions = await database.GetBirdStrikeSubmissionsAsync(cancellationToken);
        var doors = await database.GetBuildingDoorsAsync(cancellationToken);
        var counts = submissions.GroupBy(item => item.BuildingDoorId)
            .ToDictionary(group => group.Key, group => group.Count());
        var located = doors.Where(door => door.Latitude is >= -90 and <= 90 && door.Longitude is >= -180 and <= 180)
            .GroupBy(door => door.BuildingName)
            .Select(group => new
            {
                Name = group.Key,
                Latitude = group.Average(door => door.Latitude),
                Longitude = group.Average(door => door.Longitude),
                Count = group.Sum(door => counts.GetValueOrDefault(door.BuildingDoorId))
            }).ToList();

        if (located.Count == 0) return Array.Empty<BuildingReportCount>();
        var minLat = located.Min(item => item.Latitude);
        var maxLat = located.Max(item => item.Latitude);
        var minLon = located.Min(item => item.Longitude);
        var maxLon = located.Max(item => item.Longitude);
        const float inset = 0.08f;
        return located.Select(item => new BuildingReportCount(
            item.Name,
            Normalize(item.Longitude, minLon, maxLon, inset),
            1f - Normalize(item.Latitude, minLat, maxLat, inset),
            item.Count)).ToList();
    }


    // Retrieve all unique building names from the database.
    public async Task<IReadOnlyList<string>> GetBuildingsAsync(
        CancellationToken cancellationToken = default)
    {
        var doors = await database.GetBuildingDoorsAsync(cancellationToken);

        return doors
            .Select(door => door.BuildingName)
            .Distinct()
            .OrderBy(name => name)
            .ToList();
    }

    public async Task AddBuildingAsync(string buildingName,string door,double latitude,double longitude,CancellationToken cancellationToken = default)
    {
        // Validate building name.
        if (string.IsNullOrWhiteSpace(buildingName))
            throw new ArgumentException("Building name is required.");

        // Validate door number.
        if (string.IsNullOrWhiteSpace(door))
            throw new ArgumentException("Door number is required.");

        // Validate coordinates.
        if (!double.IsFinite(latitude) || latitude < -90 || latitude > 90)
            throw new ArgumentException("Invalid latitude.");

        if (!double.IsFinite(longitude) || longitude < -180 || longitude > 180)
            throw new ArgumentException("Invalid longitude.");

        // Check whether the building already exists.
        var existingBuildings = await GetBuildingsAsync(cancellationToken);

        if (existingBuildings.Any(name =>
            string.Equals(name.Trim(), buildingName.Trim(),
                StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Building already exists.");
        }

        // Create a new building with its first door.
        var buildingDoor = new BuildingDoors
        {
            BuildingDoorId = Guid.NewGuid(),
            BuildingName = buildingName.Trim(),
            Door = door.Trim(),
            Latitude = latitude,
            Longitude = longitude
        };

        // Save to Supabase through the database layer.
        await database.AddBuildingDoorAsync(
            buildingDoor, cancellationToken);
    }
    public async Task AddDoorAsync(
    string buildingName,
    string door,
    double latitude,
    double longitude,
    CancellationToken cancellationToken = default)
{
    // Validate input.
    if (string.IsNullOrWhiteSpace(buildingName))
        throw new ArgumentException("Building name is required.");

    if (string.IsNullOrWhiteSpace(door))
        throw new ArgumentException("Door number is required.");

    if (!double.IsFinite(latitude) || latitude < -90 || latitude > 90)
        throw new ArgumentException("Invalid latitude.");

    if (!double.IsFinite(longitude) || longitude < -180 || longitude > 180)
        throw new ArgumentException("Invalid longitude.");

    var existingDoors = await database.GetBuildingDoorsAsync(
        cancellationToken);

    // Verify that the building exists.
    var building = existingDoors.FirstOrDefault(item =>
        string.Equals(item.BuildingName, buildingName.Trim(),
            StringComparison.OrdinalIgnoreCase));

    if (building is null)
        throw new InvalidOperationException("Building does not exist.");

    // Prevent duplicate doors.
    if (existingDoors.Any(item =>
        string.Equals(item.BuildingName, building.BuildingName,
            StringComparison.OrdinalIgnoreCase) &&
        string.Equals(item.Door, door.Trim(),
            StringComparison.OrdinalIgnoreCase)))
    {
        throw new InvalidOperationException("Door already exists.");
    }

    // Create the new door.
    var newDoor = new BuildingDoors
    {
        BuildingDoorId = Guid.NewGuid(),
        BuildingName = building.BuildingName,
        Door = door.Trim(),
        Latitude = latitude,
        Longitude = longitude
    };

    // Save to Supabase.
    await database.AddBuildingDoorAsync(
        newDoor, cancellationToken);
}
public async Task<IReadOnlyList<BuildingDoors>> GetDoorsAsync(
    string buildingName,
    CancellationToken cancellationToken = default)
{
    var doors = await database.GetBuildingDoorsAsync(
        cancellationToken);

    return doors
        .Where(door => string.Equals(
            door.BuildingName,
            buildingName,
            StringComparison.OrdinalIgnoreCase))
        .OrderBy(door => door.Door)
        .ToList();
}


    private static float Normalize(double value, double min, double max, float inset) =>
        max == min ? 0.5f : inset + (float)((value - min) / (max - min)) * (1f - 2f * inset);






}
