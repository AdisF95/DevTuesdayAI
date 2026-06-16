namespace WorldCuppy.Infrastructure.FootballData;

// ---------------------------------------------------------------------------
// Response models for the football-data.org v4 API.
// Property names are PascalCase; deserialisation uses PropertyNameCaseInsensitive
// so they map correctly from the camelCase JSON returned by the API.
// ---------------------------------------------------------------------------

/// <summary>Response envelope for GET /v4/competitions/WC/teams.</summary>
public record TeamsResponse(IReadOnlyList<FdTeam> Teams);

/// <summary>A team entry from football-data.org.</summary>
public record FdTeam(int Id, string Name, string ShortName, string Tla, string? Crest);

/// <summary>Response envelope for GET /v4/competitions/WC/matches.</summary>
public record MatchesResponse(IReadOnlyList<FdMatch> Matches);

/// <summary>A match entry from football-data.org.</summary>
public record FdMatch(
    int Id,
    string UtcDate,
    string Status,
    string Stage,
    string? Group,
    string? Venue,
    FdTeamRef HomeTeam,
    FdTeamRef AwayTeam,
    FdScore Score);

/// <summary>Team reference inside a match — Id is null for undecided knockout slots.</summary>
public record FdTeamRef(int? Id, string? Name, string? ShortName, string? Tla, string? Crest);

/// <summary>Score wrapper.</summary>
public record FdScore(FdScoreDetail FullTime);

/// <summary>Goals scored in full time (null before the match is played).</summary>
public record FdScoreDetail(int? Home, int? Away);
