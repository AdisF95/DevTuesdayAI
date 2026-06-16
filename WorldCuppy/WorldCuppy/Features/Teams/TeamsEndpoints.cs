using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace WorldCuppy.Features.Teams;

/// <summary>Registers all Teams API routes.</summary>
public static class TeamsEndpoints
{
    /// <summary>Maps Teams endpoints onto <paramref name="app" />.</summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/teams").WithTags("Teams");

        group.MapGet("/", async (ISender sender) =>
        {
            var result = await sender.Send(new GetTeamsQuery());
            return TypedResults.Ok(result);
        })
        .WithName("GetTeams")
        .WithSummary("Get all teams");

        group.MapGet("/{code}", async Task<Results<Ok<TeamResponse>, NotFound>> (string code, ISender sender) =>
        {
            var result = await sender.Send(new GetTeamByCodeQuery(code));
            return result is null
                ? TypedResults.NotFound()
                : TypedResults.Ok(result);
        })
        .WithName("GetTeamByCode")
        .WithSummary("Get a single team by its 3-letter code (e.g. ENG)");
    }
}
