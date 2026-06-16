using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace WorldCuppy.Features.Sync;

/// <summary>Registers the admin sync endpoint.</summary>
public static class SyncEndpoints
{
    /// <summary>Maps sync endpoints onto <paramref name="app" />.</summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/admin").WithTags("Admin");

        // Returns Ok (not Created) because this is an admin action trigger, not a resource-creation endpoint.
        group.MapPost("/sync", async Task<Ok<SyncResult>> (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new SyncCommand(), ct);
            return TypedResults.Ok(result);
        })
        .WithName("SyncMatchData")
        .WithSummary("Pull the latest teams and match results from football-data.org into the database.");
    }
}
