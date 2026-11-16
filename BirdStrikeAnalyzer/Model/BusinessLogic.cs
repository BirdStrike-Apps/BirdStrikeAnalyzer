namespace BirdStrikeAnalyzer.Model;

/// <summary>Business rules and application operations. UI pages depend on this class, not on the Supabase client.</summary>
public sealed class BusinessLogic(IDatabase database)
{
    public Task<IReadOnlyList<SampleItem>> GetSampleItemsAsync(CancellationToken cancellationToken = default) =>
        database.GetSampleItemsAsync(cancellationToken);
}
