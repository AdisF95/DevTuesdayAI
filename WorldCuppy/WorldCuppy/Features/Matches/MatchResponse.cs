namespace WorldCuppy.Features.Matches;

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
