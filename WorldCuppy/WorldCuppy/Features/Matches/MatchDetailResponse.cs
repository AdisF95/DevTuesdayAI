namespace WorldCuppy.Features.Matches;

/// <summary>A goal scored during a match.</summary>
public record GoalEventResponse(
    int Minute,
    string? ScorerName,
    string? AssistName,
    string Type,
    string TeamName,
    bool IsHomeTeam);

/// <summary>A yellow or red card booking during a match.</summary>
public record BookingEventResponse(
    int Minute,
    string? PlayerName,
    string CardType,
    bool IsHomeTeam);

/// <summary>Full match detail including extended scores, goals, and bookings.</summary>
public record MatchDetailResponse(
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
    int? AwayScore,
    int? HalfTimeHomeScore,
    int? HalfTimeAwayScore,
    int? ExtraTimeHomeScore,
    int? ExtraTimeAwayScore,
    int? PenaltyHomeScore,
    int? PenaltyAwayScore,
    string? MatchDuration,
    IReadOnlyList<GoalEventResponse> Goals,
    IReadOnlyList<BookingEventResponse> Bookings);
