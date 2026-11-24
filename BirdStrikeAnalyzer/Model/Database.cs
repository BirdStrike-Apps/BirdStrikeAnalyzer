using Supabase;

namespace BirdStrikeAnalyzer.Model;

/// <summary>Supabase-backed data access. The client is initialized only when a database operation is requested.</summary>
public sealed class Database : IDatabase
{
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private Client? _client;

    public bool IsAuthenticated => _client?.Auth.CurrentSession is not null;

    public async Task<IReadOnlyList<BirdStrikeSubmission>> GetBirdStrikeSubmissionsAsync(CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(cancellationToken);
        var response = await client.From<BirdStrikeSubmission>().Get(cancellationToken);
        return response.Models;
    }

    public async Task<IReadOnlyList<BuildingDoors>> GetBuildingDoorsAsync(CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(cancellationToken);
        var response = await client.From<BuildingDoors>().Get(cancellationToken);
        return response.Models;
    }


    // Insert a new building and door into Supabase.
    public async Task AddBuildingDoorAsync(
        BuildingDoors buildingDoor,
        CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(cancellationToken);

        await client.From<BuildingDoors>()
            .Insert(buildingDoor, cancellationToken: cancellationToken);
    }


    public async Task<IReadOnlyList<BirdStrikeConfirmation>> GetBirdStrikeConfirmationsAsync(CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(cancellationToken);
        var response = await client.From<BirdStrikeConfirmation>().Get(cancellationToken);
        return response.Models;
    }

    public async Task<IReadOnlyList<Reporter>> GetReportersAsync(CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(cancellationToken);
        var response = await client.From<Reporter>().Get(cancellationToken);
        return response.Models;
    }

    public async Task ConfirmBirdStrikeSubmissionAsync(Guid submissionId, CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(cancellationToken);
        var existing = await client.From<BirdStrikeConfirmation>()
            .Where(confirmation => confirmation.SubmissionId == submissionId)
            .Get(cancellationToken);
        if (existing.Models.Count > 0) return;

        await client.From<BirdStrikeConfirmation>().Insert(new BirdStrikeConfirmation
        {
            SubmissionId = submissionId,
            EntryDate = DateTime.Today,
            EntryTime = DateTime.Now.TimeOfDay,
            EntryConfirmed = true
        }, cancellationToken: cancellationToken);
    }

    public async Task SignInAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(cancellationToken);
        await client.Auth.SignIn(email, password);
    }

    private async Task<Client> GetClientAsync(CancellationToken cancellationToken)
    {
        if (_client is not null) return _client;
        if (SupabaseSettings.ProjectUrl.Contains("YOUR_PROJECT_REF", StringComparison.Ordinal) ||
            SupabaseSettings.PublishableKey.Contains("YOUR_SUPABASE", StringComparison.Ordinal))
            throw new InvalidOperationException("Set the Supabase publishable key in Model/SupabaseSettings.cs.");

        await _initializationLock.WaitAsync(cancellationToken);
        try
        {
            if (_client is null)
            {
                var client = new Client(SupabaseSettings.ProjectUrl, SupabaseSettings.PublishableKey,
                    new SupabaseOptions { AutoRefreshToken = true, AutoConnectRealtime = false });
                await client.InitializeAsync();
                _client = client;
            }
            return _client;
        }
        finally
        {
            _initializationLock.Release();
        }
    }
}
