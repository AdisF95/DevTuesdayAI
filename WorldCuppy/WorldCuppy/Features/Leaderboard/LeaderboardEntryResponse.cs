namespace WorldCuppy.Features.Leaderboard;

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
