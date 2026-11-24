using BirdStrikeAnalyzer;

namespace BirdStrikeAnalyzer.Model;

/// <summary>Business rules and application operations. UI pages depend on this class, not on Supabase.</summary>
public sealed class BusinessLogic(IDatabase database)
{
    public bool IsAuthenticated => database.IsAuthenticated;

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
            .Where(item => start is null || (item.ReportDate ?? DateTime.MinValue).Date + (item.ReportTime ?? TimeSpan.Zero) >= start.Value)
            .Where(item => end is null || (item.ReportDate ?? DateTime.MinValue).Date + (item.ReportTime ?? TimeSpan.Zero) <= end.Value)
            .Where(item => string.IsNullOrWhiteSpace(reporterQuery) || (item.ReporterId?.ToString() ?? string.Empty).Contains(reporterQuery, StringComparison.OrdinalIgnoreCase))
            .Where(item => string.IsNullOrWhiteSpace(notesQuery) || (item.ReporterNotes ?? string.Empty).Contains(notesQuery, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(item => (item.ReportDate ?? DateTime.MinValue).Date + (item.ReportTime ?? TimeSpan.Zero)).ToList();
    }

    public async Task<IReadOnlyList<AnalyzerReport>> GetPendingReportsAsync(CancellationToken cancellationToken = default)
    {
        var submissions = await database.GetBirdStrikeSubmissionsAsync(cancellationToken);
        var confirmations = await database.GetBirdStrikeConfirmationsAsync(cancellationToken);
        var reporters = await database.GetReportersAsync(cancellationToken);
        var doors = await database.GetBuildingDoorsAsync(cancellationToken);
        var confirmedIds = confirmations.Select(item => item.SubmissionId).ToHashSet();
        var reporterNames = reporters.ToDictionary(item => item.ReporterId, item =>
            string.IsNullOrWhiteSpace(item.FullName) ? "Unnamed reporter" : item.FullName);
        var doorsById = doors.ToDictionary(item => item.BuildingDoorId);

        return submissions.Where(item => !confirmedIds.Contains(item.SubmissionId)).Select(submission =>
        {
            doorsById.TryGetValue(submission.BuildingDoorId, out var door);
            var reporter = submission.ReporterId is { } id && reporterNames.TryGetValue(id, out var name) ? name : "Unknown reporter";
            var dateTime = (submission.ReportDate ?? DateTime.MinValue).Date + (submission.ReportTime ?? TimeSpan.Zero);
            return new AnalyzerReport
            {
                Id = submission.SubmissionId,
                Reporter = reporter,
                Building = door?.BuildingName ?? "Unknown building",
                Door = door is null ? "Unknown door" : $"Door {door.Door}",
                ReportedAt = new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Local)),
                ReportDateText = submission.ReportDate?.ToString("MMM d, yyyy") ?? "Date not provided",
                ReportTimeText = submission.ReportTime is { } time ? DateTime.Today.Add(time).ToString("h:mm tt") : "Time not provided",
                Notes = submission.ReporterNotes ?? string.Empty,
                IsConfirmed = false
            };
        }).OrderByDescending(item => item.ReportedAt).ToList();
    }

    public async Task<IReadOnlyList<ReporterBuildingAssignment>> GetReporterAssignmentsAsync(CancellationToken cancellationToken = default)
    {
        var reporters = await database.GetReportersAsync(cancellationToken);
        var doors = await database.GetBuildingDoorsAsync(cancellationToken);
        var buildings = doors.Select(item => item.BuildingName).Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(_ => Random.Shared.Next()).ToList();
        return reporters.OrderBy(item => item.FirstName).ThenBy(item => item.LastName).Select((reporter, index) =>
            new ReporterBuildingAssignment(
                string.IsNullOrWhiteSpace(reporter.FullName) ? "Unnamed reporter" : reporter.FullName,
                index < buildings.Count ? buildings[index] : "Unassigned")).ToList();
    }

    public async Task<IReadOnlyList<BuildingConfirmationCount>> GetBuildingConfirmationsLast30DaysAsync(CancellationToken cancellationToken = default)
    {
        var since = DateTime.Today.AddDays(-30);
        var submissions = await database.GetBirdStrikeSubmissionsAsync(cancellationToken);
        var confirmations = await database.GetBirdStrikeConfirmationsAsync(cancellationToken);
        var doors = await database.GetBuildingDoorsAsync(cancellationToken);
        var submissionDoors = submissions.ToDictionary(item => item.SubmissionId, item => item.BuildingDoorId);
        var doorNames = doors.ToDictionary(item => item.BuildingDoorId, item => item.BuildingName);
        var counts = confirmations.Where(item => item.EntryDate.Date >= since && item.EntryDate.Date <= DateTime.Today)
            .Where(item => submissionDoors.ContainsKey(item.SubmissionId))
            .GroupBy(item => submissionDoors[item.SubmissionId])
            .ToDictionary(group => group.Key, group => group.Count());

        return doors.GroupBy(item => item.BuildingName).Select(group => new BuildingConfirmationCount(
            group.Key,
            group.Sum(door => counts.GetValueOrDefault(door.BuildingDoorId))))
            .OrderByDescending(item => item.Count).ThenBy(item => item.Building, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public async Task<MonthlyConfirmationData> GetMonthlyConfirmationsAsync(CancellationToken cancellationToken = default)
    {
        var confirmations = await database.GetBirdStrikeConfirmationsAsync(cancellationToken);
        var start = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-11);
        var months = Enumerable.Range(0, 12).Select(i => start.AddMonths(i)).ToList();
        var counts = confirmations.GroupBy(item => new DateTime(item.EntryDate.Year, item.EntryDate.Month, 1))
            .ToDictionary(group => group.Key, group => group.Count());
        return new MonthlyConfirmationData(months.Select(item => item.ToString("MMM yy")).ToList(),
            months.Select(item => counts.GetValueOrDefault(item)).ToList());
    }

    public Task ConfirmReportAsync(Guid reportId, CancellationToken cancellationToken = default) =>
        database.ConfirmBirdStrikeSubmissionAsync(reportId, cancellationToken);

    public async Task<IReadOnlyList<BuildingReportCount>> GetBuildingHotspotsAsync(CancellationToken cancellationToken = default)
    {
        var submissions = await database.GetBirdStrikeSubmissionsAsync(cancellationToken);
        var confirmations = await database.GetBirdStrikeConfirmationsAsync(cancellationToken);
        var doors = await database.GetBuildingDoorsAsync(cancellationToken);
        var confirmedIds = confirmations.Select(item => item.SubmissionId).ToHashSet();
        var confirmedSubmissions = submissions.Where(item => confirmedIds.Contains(item.SubmissionId)).ToList();
        var counts = confirmedSubmissions.GroupBy(item => item.BuildingDoorId).ToDictionary(group => group.Key, group => group.Count());
        var located = doors.Where(door => door.Latitude.HasValue && door.Longitude.HasValue && door.Latitude.Value is >= -90 and <= 90 && door.Longitude.Value is >= -180 and <= 180)
            .GroupBy(door => door.BuildingName).Select(group => new
            {
                Name = group.Key,
                Latitude = group.Average(door => door.Latitude!.Value),
                Longitude = group.Average(door => door.Longitude!.Value),
                Count = group.Sum(door => counts.GetValueOrDefault(door.BuildingDoorId))
            }).ToList();
        if (located.Count == 0) return Array.Empty<BuildingReportCount>();
        var minLat = located.Min(item => item.Latitude); var maxLat = located.Max(item => item.Latitude);
        var minLon = located.Min(item => item.Longitude); var maxLon = located.Max(item => item.Longitude);
        const float inset = 0.08f;
        return located.Select(item => new BuildingReportCount(item.Name,
            Normalize(item.Longitude, minLon, maxLon, inset), 1f - Normalize(item.Latitude, minLat, maxLat, inset), item.Count, item.Latitude, item.Longitude)).ToList();
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

    public async Task AddBuildingAsync(string buildingName, string door, double latitude, double longitude, CancellationToken cancellationToken = default)
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


    public async Task EditDoorAsync(
        Guid doorId,
        string doorNumber,
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default)
    {
        // Validate input.
        if (string.IsNullOrWhiteSpace(doorNumber))
            throw new ArgumentException("Door number is required.");

        if (!double.IsFinite(latitude) || latitude < -90 || latitude > 90)
            throw new ArgumentException("Invalid latitude.");

        if (!double.IsFinite(longitude) || longitude < -180 || longitude > 180)
            throw new ArgumentException("Invalid longitude.");

        var doors = await database.GetBuildingDoorsAsync(
            cancellationToken);

        // Find the existing door.
        var existingDoor = doors.FirstOrDefault(
            door => door.BuildingDoorId == doorId);

        if (existingDoor is null)
            throw new InvalidOperationException("Door not found.");

        // Prevent duplicate door numbers.
        if (doors.Any(door =>
            door.BuildingDoorId != doorId &&
            string.Equals(
                door.BuildingName,
                existingDoor.BuildingName,
                StringComparison.OrdinalIgnoreCase) &&
            string.Equals(
                door.Door,
                doorNumber.Trim(),
                StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Door already exists.");
        }

        // Update the existing door.
        existingDoor.Door = doorNumber.Trim();
        existingDoor.Latitude = latitude;
        existingDoor.Longitude = longitude;

        await database.UpdateBuildingDoorAsync(
            existingDoor, cancellationToken);
    }

    private static float Normalize(double value, double min, double max, float inset) =>
        max == min ? 0.5f : inset + (float)((value - min) / (max - min)) * (1f - 2f * inset);






}

public sealed record ReporterBuildingAssignment(string Reporter, string Building);
public sealed record BuildingConfirmationCount(string Building, int Count);
public sealed record MonthlyConfirmationData(IReadOnlyList<string> Months, IReadOnlyList<int> Counts);
