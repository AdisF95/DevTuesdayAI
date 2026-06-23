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
    /// Loads every registered user with a left join to their awarded predictions,
    /// aggregates persisted <see cref="WorldCuppy.Domain.Prediction.Points" /> values,
    /// and returns a ranked list ordered by total points descending, then username ascending.
    /// Users with no predictions or no awarded points appear with zero totals.
    /// </summary>
    public async Task<List<UserLeaderboardEntryResponse>> Handle(
        GetUserLeaderboardQuery request,
        CancellationToken cancellationToken)
    {
        var rows = await db.Users
            .OrderBy(u => u.Username)
            .Select(u => new
            {
                u.Username,
                TotalPoints = db.Predictions
                    .Where(p => p.UserId == u.Id && p.PointsAwardedAtUtc != null)
                    .Sum(p => (int?)p.Points) ?? 0,
                PredictionsCount = db.Predictions
                    .Count(p => p.UserId == u.Id && p.PointsAwardedAtUtc != null),
                ExactScores = db.Predictions
                    .Count(p => p.UserId == u.Id && p.PointsAwardedAtUtc != null && p.Points == 3),
                CorrectResults = db.Predictions
                    .Count(p => p.UserId == u.Id && p.PointsAwardedAtUtc != null && p.Points == 1),
            })
            .ToListAsync(cancellationToken);

        return rows
            .OrderByDescending(r => r.TotalPoints)
            .ThenBy(r => r.Username)
            .Select((r, index) => new UserLeaderboardEntryResponse(
                Rank: index + 1,
                Username: r.Username,
                TotalPoints: r.TotalPoints,
                PredictionsCount: r.PredictionsCount,
                ExactScores: r.ExactScores,
                CorrectResults: r.CorrectResults))
            .ToList();
    }
}
