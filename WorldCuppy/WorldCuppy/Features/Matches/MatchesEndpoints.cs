using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace WorldCuppy.Features.Matches;

public static class MatchesEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/matches").WithTags("Matches");

        group.MapGet("/game-day/{date}", async (DateOnly date, ISender sender) =>
        {
            var matches = await sender.Send(new GetMatchesByGameDayQuery(date));
            return TypedResults.Ok(matches);
        })
        .WithName("GetMatchesByGameDay")
        .WithSummary("Get all matches scheduled on a given date");

        group.MapGet("/{id:guid}", async Task<Results<Ok<MatchResponse>, NotFound>> (Guid id, ISender sender) =>
        {
            var match = await sender.Send(new GetMatchByIdQuery(id));
            return match is null
                ? TypedResults.NotFound()
                : TypedResults.Ok(match);
        })
        .WithName("GetMatchById")
        .WithSummary("Get a single match by ID");
    }
}
