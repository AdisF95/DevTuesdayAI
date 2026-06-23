using MediatR;
using Microsoft.EntityFrameworkCore;
using WorldCuppy.Infrastructure.Persistence;

namespace WorldCuppy.Features.Leaderboard;

/// <summary>Query that retrieves the ranked prediction leaderboard for all users.</summary>
public record GetUserLeaderboardQuery : IRequest<List<UserLeaderboardEntryResponse>>;

/// <summary>Handles <see cref="GetUserLeaderboardQuery" />.</summary>
public class GetUserLeaderboardHandler(WorldCuppyDbContext db)
    : IRequestHandler<GetUserLeaderboardQuery, List<UserLeaderboardEntryResponse>>
{
    /// <summary>
    /// Loads all predictions for finished matches, joins with user records,
    /// scores each prediction via <see cref="UserLeaderboardCalculator" />, then ranks users.
    /// Predictions whose UserId has no matching User row are skipped.
    /// </summary>
    public async Task<List<UserLeaderboardEntryResponse>> Handle(
        GetUserLeaderboardQuery request,
        CancellationToken cancellationToken)
    {
        var finishedPredictions = await db.Predictions
            .Where(p => p.Match.HomeScore != null && p.Match.AwayScore != null)
            .Select(p => new
            {
                p.UserId,
                p.PredictedHomeScore,
                p.PredictedAwayScore,
                ActualHome = p.Match.HomeScore!.Value,
                ActualAway = p.Match.AwayScore!.Value,
            })
            .ToListAsync(cancellationToken);

        if (finishedPredictions.Count == 0)
        {
            return [];
        }

        var userIds = finishedPredictions.Select(p => p.UserId).Distinct().ToList();

        var userNames = await db.Users
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.Username })
            .ToDictionaryAsync(u => u.Id, u => u.Username, cancellationToken);

        var scored = finishedPredictions
            .Where(p => userNames.ContainsKey(p.UserId))
            .Select(p => new UserLeaderboardCalculator.ScoredPrediction(
                UserId: p.UserId,
                Username: userNames[p.UserId],
                Points: UserLeaderboardCalculator.CalculatePoints(
                    p.PredictedHomeScore, p.PredictedAwayScore,
                    p.ActualHome, p.ActualAway),
                IsExact: UserLeaderboardCalculator.CalculateOutcome(
                    p.PredictedHomeScore, p.PredictedAwayScore,
                    p.ActualHome, p.ActualAway) == "Exact",
                IsCorrect: UserLeaderboardCalculator.CalculateOutcome(
                    p.PredictedHomeScore, p.PredictedAwayScore,
                    p.ActualHome, p.ActualAway) == "Correct"));

        return UserLeaderboardCalculator.Rank(scored);
    }
}
