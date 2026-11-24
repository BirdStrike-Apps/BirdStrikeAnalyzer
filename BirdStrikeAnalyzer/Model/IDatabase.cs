namespace BirdStrikeAnalyzer.Model;

public interface IDatabase
{
    Task<IReadOnlyList<BirdStrikeSubmission>> GetBirdStrikeSubmissionsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BuildingDoors>> GetBuildingDoorsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BirdStrikeConfirmation>> GetBirdStrikeConfirmationsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Reporter>> GetReportersAsync(CancellationToken cancellationToken = default);
    Task ConfirmBirdStrikeSubmissionAsync(Guid submissionId, CancellationToken cancellationToken = default);
    bool IsAuthenticated { get; }
    Task SignInAsync(string email, string password, CancellationToken cancellationToken = default);
    Task AddBuildingDoorAsync(BuildingDoors buildingDoor,CancellationToken cancellationToken = default);
}
