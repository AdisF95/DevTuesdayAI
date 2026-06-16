using MediatR;
using Microsoft.EntityFrameworkCore;
using WorldCuppy.Infrastructure.Persistence;

namespace WorldCuppy.Features.Matches;

/// <summary>Query that retrieves full match detail — including extended scores, goals, and bookings — by match id.</summary>
public record GetMatchDetailQuery(Guid MatchId) : IRequest<MatchDetailResponse?>;

/// <summary>Handles <see cref="GetMatchDetailQuery" />.</summary>
public class GetMatchDetailHandler(WorldCuppyDbContext db)
    : IRequestHandler<GetMatchDetailQuery, MatchDetailResponse?>
{
    /// <summary>Loads the match with its events and maps to <see cref="MatchDetailResponse" />.</summary>
    public async Task<MatchDetailResponse?> Handle(GetMatchDetailQuery request, CancellationToken cancellationToken)
    {
        var match = await db.Matches
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .Include(m => m.GoalEvents)
            .Include(m => m.BookingEvents)
            .FirstOrDefaultAsync(m => m.Id == request.MatchId, cancellationToken);

        if (match is null)
        {
            return null;
        }

        return new MatchDetailResponse(
            match.Id,
            match.HomeTeam.Name,
            match.HomeTeam.Code,
            match.HomeTeam.CrestUrl,
            match.AwayTeam.Name,
            match.AwayTeam.Code,
            match.AwayTeam.CrestUrl,
            match.KickoffUtc,
            match.GameDay,
            match.Round?.ToString(),
            match.Status.ToString(),
            match.Group,
            match.Venue,
            match.HomeScore,
            match.AwayScore,
            match.HalfTimeHomeScore,
            match.HalfTimeAwayScore,
            match.ExtraTimeHomeScore,
            match.ExtraTimeAwayScore,
            match.PenaltyHomeScore,
            match.PenaltyAwayScore,
            match.MatchDuration,
            match.GoalEvents
                .OrderBy(g => g.Minute)
                .Select(g => new GoalEventResponse(g.Minute, g.ScorerName, g.AssistName, g.Type, g.TeamName, g.IsHomeTeam))
                .ToList(),
            match.BookingEvents
                .OrderBy(b => b.Minute)
                .Select(b => new BookingEventResponse(b.Minute, b.PlayerName, b.CardType, b.IsHomeTeam))
                .ToList());
    }
}
