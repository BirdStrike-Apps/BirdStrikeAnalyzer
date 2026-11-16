using Supabase;

namespace BirdStrikeAnalyzer.Model;

/// <summary>Supabase-backed data access. The client is initialized only when a database operation is requested.</summary>
public sealed class Database : IDatabase
{
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private Client? _client;

    public async Task<IReadOnlyList<SampleItem>> GetSampleItemsAsync(CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(cancellationToken);
        var response = await client.From<SampleItem>().Get(cancellationToken);
        return response.Models;
    }

    private async Task<Client> GetClientAsync(CancellationToken cancellationToken)
    {
        if (_client is not null) return _client;
        if (SupabaseSettings.ProjectUrl.Contains("YOUR_PROJECT_REF", StringComparison.Ordinal) ||
            SupabaseSettings.PublishableKey.Contains("YOUR_SUPABASE", StringComparison.Ordinal))
            throw new InvalidOperationException("Set the Supabase project URL and publishable key in Model/SupabaseSettings.cs.");

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
