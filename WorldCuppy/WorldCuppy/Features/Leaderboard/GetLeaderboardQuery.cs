using MediatR;
using Microsoft.EntityFrameworkCore;
using WorldCuppy.Infrastructure.Persistence;

namespace WorldCuppy.Features.Leaderboard;

/// <summary>Query that retrieves the ranked leaderboard for all teams.</summary>
public record GetLeaderboardQuery : IRequest<List<LeaderboardEntryResponse>>;

/// <summary>Handles <see cref="GetLeaderboardQuery" />.</summary>
public class GetLeaderboardHandler(WorldCuppyDbContext db)
    : IRequestHandler<GetLeaderboardQuery, List<LeaderboardEntryResponse>>
{
    /// <summary>Loads teams and completed match results then delegates ranking to <see cref="LeaderboardCalculator" />.</summary>
    public async Task<List<LeaderboardEntryResponse>> Handle(GetLeaderboardQuery request, CancellationToken cancellationToken)
    {
        var teams = await db.Teams
            .Select(t => new LeaderboardCalculator.TeamEntry(t.Id, t.Name, t.Code))
            .ToListAsync(cancellationToken);

        var completedMatches = await db.Matches
            .Where(m => m.HomeScore != null && m.AwayScore != null)
            .Select(m => new LeaderboardCalculator.MatchResult(
                m.HomeTeamId,
                m.AwayTeamId,
                m.HomeScore!.Value,
                m.AwayScore!.Value))
            .ToListAsync(cancellationToken);

        return LeaderboardCalculator.Calculate(teams, completedMatches);
    }
}
