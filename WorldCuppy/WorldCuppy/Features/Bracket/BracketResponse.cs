namespace WorldCuppy.Features.Bracket;

/// <summary>Full knockout bracket grouped by round.</summary>
public record BracketResponse(List<BracketRoundDto> Rounds);

/// <summary>One knockout round and its matches.</summary>
public record BracketRoundDto(string Round, List<BracketMatchDto> Matches);

/// <summary>A single knockout fixture.</summary>
public record BracketMatchDto(
    Guid MatchId,
    DateTimeOffset KickoffUtc,
    BracketTeamDto HomeTeam,
    BracketTeamDto AwayTeam,
    string? Score,
    string Status);

/// <summary>A team in a bracket slot, or a TBD placeholder.</summary>
public record BracketTeamDto(string Name, string? Code, string? CrestUrl);
