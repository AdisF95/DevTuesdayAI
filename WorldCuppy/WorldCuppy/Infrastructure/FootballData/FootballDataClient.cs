using System.Net;
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

    // football-data.org free tier allows ~10 requests/minute.
    // When 429 is received we wait one full minute before retrying.
    private static readonly TimeSpan RateLimitRetryDelay = TimeSpan.FromSeconds(65);
    private const int MaxRetries = 3;

    /// <summary>Fetches all teams registered for the 2026 FIFA World Cup.</summary>
    public async Task<IReadOnlyList<FdTeam>> GetTeamsAsync(CancellationToken cancellationToken = default)
    {
        var response = await GetJsonWithRetryAsync<TeamsResponse>("competitions/WC/teams", cancellationToken);
        return response?.Teams ?? [];
    }

    /// <summary>
    /// Fetches matches for the 2026 FIFA World Cup.
    /// Supply <paramref name="dateFrom" /> / <paramref name="dateTo" /> to scope the call to an active window
    /// and avoid re-fetching the growing tail of already-finished matches.
    /// </summary>
    public async Task<IReadOnlyList<FdMatch>> GetMatchesAsync(
        DateOnly? dateFrom = null,
        DateOnly? dateTo = null,
        CancellationToken cancellationToken = default)
    {
        var url = "competitions/WC/matches";
        if (dateFrom.HasValue || dateTo.HasValue)
        {
            var from = (dateFrom ?? DateOnly.MinValue).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            var to   = (dateTo   ?? DateOnly.MaxValue).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            url += $"?dateFrom={from}&dateTo={to}";
        }

        var response = await GetJsonWithRetryAsync<MatchesResponse>(url, cancellationToken);
        return response?.Matches ?? [];
    }

    /// <summary>
    /// Fetches full detail for a single match including goals and bookings.
    /// Does NOT retry on 429 — callers that process batches should break on
    /// <see cref="HttpStatusCode.TooManyRequests" /> and let the next sync cycle continue.
    /// </summary>
    public async Task<MatchDetailApiResponse?> GetMatchDetailAsync(int externalMatchId, CancellationToken cancellationToken = default)
    {
        return await http.GetFromJsonAsync<MatchDetailApiResponse>(
            $"matches/{externalMatchId}", JsonOptions, cancellationToken);
    }

    /// <summary>Fetches group stage standings for the 2026 FIFA World Cup.</summary>
    public async Task<IReadOnlyList<FdStandingsGroup>> GetStandingsAsync(CancellationToken cancellationToken = default)
    {
        var response = await GetJsonWithRetryAsync<StandingsResponse>("competitions/WC/standings", cancellationToken);
        return response?.Standings ?? [];
    }

    /// <summary>
    /// GET helper with automatic retry on HTTP 429 Too Many Requests.
    /// Waits <see cref="RateLimitRetryDelay" /> between attempts, up to <see cref="MaxRetries" /> retries.
    /// </summary>
    private async Task<T?> GetJsonWithRetryAsync<T>(string url, CancellationToken ct)
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                return await http.GetFromJsonAsync<T>(url, JsonOptions, ct);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests && attempt < MaxRetries)
            {
                await Task.Delay(RateLimitRetryDelay, ct);
            }
        }
    }
}
