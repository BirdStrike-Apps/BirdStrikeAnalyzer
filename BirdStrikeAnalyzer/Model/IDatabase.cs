namespace BirdStrikeAnalyzer.Model;

public interface IDatabase
{
    Task<IReadOnlyList<SampleItem>> GetSampleItemsAsync(CancellationToken cancellationToken = default);
}
