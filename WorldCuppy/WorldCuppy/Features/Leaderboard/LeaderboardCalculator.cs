namespace WorldCuppy.Features.Leaderboard;

/// <summary>Pure in-memory calculation of leaderboard standings from completed match results.</summary>
internal static class LeaderboardCalculator
{
    /// <summary>A completed match result used as input to the standings calculation.</summary>
    internal record MatchResult(Guid HomeTeamId, Guid AwayTeamId, int HomeScore, int AwayScore);

    /// <summary>A team entry used as input to the standings calculation.</summary>
    internal record TeamEntry(Guid Id, string Name, string Code);

    /// <summary>
    /// Calculates ranked leaderboard entries from a list of teams and their completed match results.
    /// Ordering: points desc → goal difference desc → goals for desc → name asc.
    /// </summary>
    internal static List<LeaderboardEntryResponse> Calculate(
        IReadOnlyList<TeamEntry> teams,
        IReadOnlyList<MatchResult> completedMatches)
    {
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
                Points = (won * 3) + drawn,
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
