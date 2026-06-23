namespace WorldCuppy.Features.Leaderboard;

/// <summary>A single ranked user entry in the user prediction leaderboard.</summary>
public record UserLeaderboardEntryResponse(
    int Rank,
    string Username,
    int TotalPoints,
    int PredictionsCount,
    int ExactScores,
    int CorrectResults
);
