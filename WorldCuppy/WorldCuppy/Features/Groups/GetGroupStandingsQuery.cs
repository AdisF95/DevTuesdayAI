using MediatR;
using Microsoft.EntityFrameworkCore;
using WorldCuppy.Infrastructure.Persistence;

namespace WorldCuppy.Features.Groups;

/// <summary>One row in a group standings table.</summary>
public record GroupStandingResponse(
    string Group,
    int Position,
    Guid TeamId,
    string TeamName,
    string TeamCode,
    string? TeamCrest,
    int PlayedGames,
    int Won,
    int Draw,
    int Lost,
    int GoalsFor,
    int GoalsAgainst,
    int GoalDifference,
    int Points,
    string? Form);

/// <summary>Query that retrieves all group stage standings ordered by group then position.</summary>
public record GetGroupStandingsQuery : IRequest<List<GroupStandingResponse>>;

/// <summary>Handles <see cref="GetGroupStandingsQuery" />.</summary>
public class GetGroupStandingsHandler(WorldCuppyDbContext db)
    : IRequestHandler<GetGroupStandingsQuery, List<GroupStandingResponse>>
{
    /// <summary>Executes the query against the database.</summary>
    public Task<List<GroupStandingResponse>> Handle(GetGroupStandingsQuery request, CancellationToken cancellationToken) =>
        db.GroupStandings
            .OrderBy(s => s.Group)
            .ThenBy(s => s.Position)
            .Select(s => new GroupStandingResponse(
                s.Group,
                s.Position,
                s.TeamId,
                s.Team.Name,
                s.Team.Code,
                s.Team.CrestUrl,
                s.PlayedGames,
                s.Won,
                s.Draw,
                s.Lost,
                s.GoalsFor,
                s.GoalsAgainst,
                s.GoalDifference,
                s.Points,
                s.Form))
            .ToListAsync(cancellationToken);
}
