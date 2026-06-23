using MediatR;
using Microsoft.EntityFrameworkCore;
using WorldCuppy.Domain;
using WorldCuppy.Infrastructure.Persistence;

namespace WorldCuppy.Features.Bracket;

/// <summary>Query that retrieves the full knockout bracket grouped by round.</summary>
public record GetBracketQuery : IRequest<BracketResponse>;

/// <summary>Handles <see cref="GetBracketQuery" />.</summary>
public class GetBracketHandler(WorldCuppyDbContext db)
    : IRequestHandler<GetBracketQuery, BracketResponse>
{
    /// <summary>
    /// Fetches all knockout matches (where Round is not null), groups them by round
    /// in fixed enum order, and returns the bracket with formatted scores.
    /// </summary>
    public async Task<BracketResponse> Handle(GetBracketQuery request, CancellationToken cancellationToken)
    {
        var matches = await db.Matches
            .Where(m => m.Round != null)
            .OrderBy(m => m.Round)
            .ThenBy(m => m.KickoffUtc)
            .Select(m => new
            {
                m.Id,
                m.KickoffUtc,
                m.Round,
                m.Status,
                m.MatchDuration,
                m.HomeScore,
                m.AwayScore,
                m.ExtraTimeHomeScore,
                m.ExtraTimeAwayScore,
                m.PenaltyHomeScore,
                m.PenaltyAwayScore,
                HomeTeamName = (string?)m.HomeTeam.Name,
                HomeTeamCode = (string?)m.HomeTeam.Code,
                HomeTeamCrestUrl = m.HomeTeam.CrestUrl,
                AwayTeamName = (string?)m.AwayTeam.Name,
                AwayTeamCode = (string?)m.AwayTeam.Code,
                AwayTeamCrestUrl = m.AwayTeam.CrestUrl,
            })
            .ToListAsync(cancellationToken);

        var rounds = matches
            .GroupBy(m => m.Round!.Value)
            .OrderBy(g => g.Key)
            .Select(g => new BracketRoundDto(
                Round: g.Key.ToString(),
                Matches: g.Select(m => new BracketMatchDto(
                    MatchId: m.Id,
                    KickoffUtc: m.KickoffUtc,
                    HomeTeam: ToTeamDto(m.HomeTeamName, m.HomeTeamCode, m.HomeTeamCrestUrl),
                    AwayTeam: ToTeamDto(m.AwayTeamName, m.AwayTeamCode, m.AwayTeamCrestUrl),
                    Score: ScoreFormatter.Format(
                        m.Status,
                        m.MatchDuration,
                        m.HomeScore, m.AwayScore,
                        m.ExtraTimeHomeScore, m.ExtraTimeAwayScore,
                        m.PenaltyHomeScore, m.PenaltyAwayScore),
                    Status: m.Status.ToString()))
                .ToList()))
            .ToList();

        return new BracketResponse(rounds);
    }

    /// <summary>
    /// Maps projected team fields to a <see cref="BracketTeamDto" />.
    /// Returns a TBD placeholder when the team name is null or empty.
    /// </summary>
    private static BracketTeamDto ToTeamDto(string? name, string? code, string? crestUrl)
    {
        if (string.IsNullOrEmpty(name))
        {
            return new BracketTeamDto("TBD", null, null);
        }

        return new BracketTeamDto(name, code, crestUrl);
    }
}
