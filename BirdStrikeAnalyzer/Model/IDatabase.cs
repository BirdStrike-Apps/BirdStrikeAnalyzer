namespace BirdStrikeAnalyzer.Model;

public interface IDatabase
{
    Task<IReadOnlyList<SampleItem>> GetSampleItemsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BirdStrikeSubmission>> GetBirdStrikeSubmissionsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BuildingDoors>> GetBuildingDoorsAsync(CancellationToken cancellationToken = default);
    Task ConfirmBirdStrikeSubmissionAsync(Guid submissionId, CancellationToken cancellationToken = default);
    bool IsAuthenticated { get; }
    Task SignInAsync(string email, string password, CancellationToken cancellationToken = default);
    Task AddBuildingDoorAsync(BuildingDoors buildingDoor,CancellationToken cancellationToken = default);
}
