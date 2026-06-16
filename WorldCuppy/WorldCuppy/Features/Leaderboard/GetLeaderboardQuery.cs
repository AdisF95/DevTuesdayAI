using MediatR;
using Microsoft.EntityFrameworkCore;
using WorldCuppy.Infrastructure.Persistence;

namespace WorldCuppy.Features.Leaderboard;

public record GetLeaderboardQuery : IRequest<List<LeaderboardEntryResponse>>;

public class GetLeaderboardHandler(WorldCuppyDbContext db)
    : IRequestHandler<GetLeaderboardQuery, List<LeaderboardEntryResponse>>
{
    public async Task<List<LeaderboardEntryResponse>> Handle(GetLeaderboardQuery request, CancellationToken cancellationToken)
    {
        var teams = await db.Teams.ToListAsync(cancellationToken);

        var completedMatches = await db.Matches
            .Where(m => m.HomeScore != null && m.AwayScore != null)
            .Select(m => new
            {
                m.HomeTeamId,
                m.AwayTeamId,
                HomeScore = m.HomeScore!.Value,
                AwayScore = m.AwayScore!.Value
            })
            .ToListAsync(cancellationToken);

        var stats = teams.Select(team =>
        {
            var homeMatches = completedMatches.Where(m => m.HomeTeamId == team.Id).ToList();
            var awayMatches = completedMatches.Where(m => m.AwayTeamId == team.Id).ToList();

            var played = homeMatches.Count + awayMatches.Count;

            var won = homeMatches.Count(m => m.HomeScore > m.AwayScore)
                    + awayMatches.Count(m => m.AwayScore > m.HomeScore);

            var drawn = homeMatches.Count(m => m.HomeScore == m.AwayScore)
                      + awayMatches.Count(m => m.HomeScore == m.AwayScore);

            var lost = homeMatches.Count(m => m.HomeScore < m.AwayScore)
                     + awayMatches.Count(m => m.AwayScore < m.HomeScore);

            var goalsFor = homeMatches.Sum(m => m.HomeScore) + awayMatches.Sum(m => m.AwayScore);
            var goalsAgainst = homeMatches.Sum(m => m.AwayScore) + awayMatches.Sum(m => m.HomeScore);

            var points = (won * 3) + drawn;

            return new
            {
                team.Name,
                team.Code,
                Played = played,
                Won = won,
                Drawn = drawn,
                Lost = lost,
                GoalsFor = goalsFor,
                GoalsAgainst = goalsAgainst,
                GoalDifference = goalsFor - goalsAgainst,
                Points = points
            };
        })
        .OrderByDescending(s => s.Points)
        .ThenByDescending(s => s.GoalDifference)
        .ThenByDescending(s => s.GoalsFor)
        .ThenBy(s => s.Name)
        .ToList();

        return stats
            .Select((s, index) => new LeaderboardEntryResponse(
                Rank: index + 1,
                Team: s.Name,
                TeamCode: s.Code,
                Played: s.Played,
                Won: s.Won,
                Drawn: s.Drawn,
                Lost: s.Lost,
                GoalsFor: s.GoalsFor,
                GoalsAgainst: s.GoalsAgainst,
                GoalDifference: s.GoalDifference,
                Points: s.Points))
            .ToList();
    }
}
