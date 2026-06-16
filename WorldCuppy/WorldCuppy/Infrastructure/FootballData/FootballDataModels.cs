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

/// <summary>Score wrapper; Duration describes how the match concluded.</summary>
public record FdScore(
    string? Duration,
    FdScoreDetail FullTime,
    FdScoreDetail? HalfTime,
    FdScoreDetail? ExtraTime,
    FdScoreDetail? Penalties);

/// <summary>Goals scored in a period (null before the period is played).</summary>
public record FdScoreDetail(int? Home, int? Away);

// ---------------------------------------------------------------------------
// Match detail — GET /v4/matches/{id}
// ---------------------------------------------------------------------------

/// <summary>Full detail for a single match including goals and bookings.</summary>
public record MatchDetailApiResponse(
    int Id,
    FdScore Score,
    FdTeamRef HomeTeam,
    FdTeamRef AwayTeam,
    IReadOnlyList<FdGoal>? Goals,
    IReadOnlyList<FdBooking>? Bookings);

/// <summary>A goal event within a match.</summary>
public record FdGoal(
    int? Minute,
    string? Type,
    FdTeamRef? Team,
    FdPlayerRef? Scorer,
    FdPlayerRef? Assist);

/// <summary>A booking (card) event within a match.</summary>
public record FdBooking(
    int? Minute,
    FdTeamRef? Team,
    FdPlayerRef? Player,
    string? Card);

/// <summary>A player reference used in goals and bookings.</summary>
public record FdPlayerRef(int? Id, string? Name);

// ---------------------------------------------------------------------------
// Standings — GET /v4/competitions/WC/standings
// ---------------------------------------------------------------------------

/// <summary>Response envelope for GET /v4/competitions/WC/standings.</summary>
public record StandingsResponse(IReadOnlyList<FdStandingsGroup> Standings);

/// <summary>Standing table for a single group or stage.</summary>
public record FdStandingsGroup(
    string Stage,
    string? Group,
    IReadOnlyList<FdTableEntry> Table);

/// <summary>One row in a group standings table.</summary>
public record FdTableEntry(
    int Position,
    FdTeamRef Team,
    int PlayedGames,
    int Won,
    int Draw,
    int Lost,
    int Points,
    int GoalsFor,
    int GoalsAgainst,
    int GoalDifference,
    string? Form);
