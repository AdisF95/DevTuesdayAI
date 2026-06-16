using System.Net.Http.Json;
using System.Text.Json;

namespace WorldCuppy.Infrastructure.FootballData;

/// <summary>Typed HTTP client for the football-data.org v4 API.</summary>
public class FootballDataClient(HttpClient http)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>Fetches all teams registered for the 2026 FIFA World Cup.</summary>
    public async Task<IReadOnlyList<FdTeam>> GetTeamsAsync(CancellationToken cancellationToken = default)
    {
        var response = await http.GetFromJsonAsync<TeamsResponse>(
            "competitions/WC/teams", JsonOptions, cancellationToken);

        return response?.Teams ?? [];
    }

    /// <summary>Fetches all matches (group stage + knockout) for the 2026 FIFA World Cup.</summary>
    public async Task<IReadOnlyList<FdMatch>> GetMatchesAsync(CancellationToken cancellationToken = default)
    {
        var response = await http.GetFromJsonAsync<MatchesResponse>(
            "competitions/WC/matches", JsonOptions, cancellationToken);

        return response?.Matches ?? [];
    }
}
