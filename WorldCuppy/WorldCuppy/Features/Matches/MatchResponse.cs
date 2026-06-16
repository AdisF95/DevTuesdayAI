namespace WorldCuppy.Features.Matches;

public record MatchResponse(
    Guid Id,
    string HomeTeam,
    string HomeTeamCode,
    string AwayTeam,
    string AwayTeamCode,
    DateTimeOffset KickoffUtc,
    DateOnly GameDay,
    string Round,
    string Venue,
    int? HomeScore,
    int? AwayScore
);
