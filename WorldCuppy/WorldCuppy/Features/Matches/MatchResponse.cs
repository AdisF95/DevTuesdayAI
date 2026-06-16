namespace WorldCuppy.Features.Matches;

/// <summary>Response payload for a single match fixture.</summary>
public record MatchResponse(
    Guid Id,
    string HomeTeam,
    string HomeTeamCode,
    string? HomeTeamCrest,
    string AwayTeam,
    string AwayTeamCode,
    string? AwayTeamCrest,
    DateTimeOffset KickoffUtc,
    DateOnly GameDay,
    string? Round,
    string Status,
    string? Group,
    string Venue,
    int? HomeScore,
    int? AwayScore
);
