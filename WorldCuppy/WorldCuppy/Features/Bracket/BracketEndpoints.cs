using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace WorldCuppy.Features.Bracket;

/// <summary>Registers all Bracket API routes.</summary>
public static class BracketEndpoints
{
    /// <summary>Maps Bracket endpoints onto <paramref name="app" />.</summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/bracket").WithTags("Bracket");

        group.MapGet("/", async Task<Ok<BracketResponse>> (ISender sender) =>
        {
            var result = await sender.Send(new GetBracketQuery());
            return TypedResults.Ok(result);
        })
        .WithName("GetBracket")
        .WithSummary("Get the full knockout bracket grouped by round");
    }
}
