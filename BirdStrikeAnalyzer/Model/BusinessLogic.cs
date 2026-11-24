namespace BirdStrikeAnalyzer.Model;

/// <summary>Business rules and application operations. UI pages depend on this class, not on the Supabase client.</summary>
public sealed class BusinessLogic(IDatabase database)
{
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
            .Where(item => string.IsNullOrWhiteSpace(reporterQuery) || item.ReporterId.ToString().Contains(reporterQuery, StringComparison.OrdinalIgnoreCase))
            .Where(item => string.IsNullOrWhiteSpace(notesQuery) || (item.ReporterNotes ?? string.Empty).Contains(notesQuery, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(item => item.ReportDate.Date + item.ReportTime)
            .ToList();
    }
}
