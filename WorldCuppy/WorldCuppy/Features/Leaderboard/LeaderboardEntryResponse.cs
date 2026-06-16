namespace WorldCuppy.Features.Leaderboard;

/// <summary>A single ranked team entry in the leaderboard.</summary>
public record LeaderboardEntryResponse(
    int Rank,
    string Team,
    string TeamCode,
    int Played,
    int Won,
    int Drawn,
    int Lost,
    int GoalsFor,
    int GoalsAgainst,
    int GoalDifference,
    int Points
);
