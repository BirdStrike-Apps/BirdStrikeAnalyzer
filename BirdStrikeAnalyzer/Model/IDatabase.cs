namespace BirdStrikeAnalyzer.Model;

public interface IDatabase
{
    Task<IReadOnlyList<SampleItem>> GetSampleItemsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BirdStrikeSubmission>> GetBirdStrikeSubmissionsAsync(CancellationToken cancellationToken = default);
    Task SignInAsync(string email, string password, CancellationToken cancellationToken = default);
}
