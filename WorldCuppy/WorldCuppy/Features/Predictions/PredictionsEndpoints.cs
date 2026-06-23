using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace WorldCuppy.Features.Predictions;

/// <summary>Registers all Predictions API routes.</summary>
public static class PredictionsEndpoints
{
    /// <summary>Maps Predictions endpoints onto <paramref name="app" />.</summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/predictions").WithTags("Predictions");

        group.MapGet("/user/{userId:guid}", async (Guid userId, ISender sender) =>
        {
            var result = await sender.Send(new GetPredictionsByUserQuery(userId));
            return TypedResults.Ok(result);
        })
        .WithName("GetPredictionsByUser")
        .WithSummary("Get all predictions submitted by a specific user");

        group.MapGet("/upcoming/{userId:guid}", async (Guid userId, ISender sender) =>
        {
            var result = await sender.Send(new GetUpcomingMatchesWithPredictionsQuery(userId));
            return TypedResults.Ok(result);
        })
        .WithName("GetUpcomingMatchesWithPredictions")
        .WithSummary("Get all scheduled matches with the user's predictions (null when not yet predicted)");

        group.MapPost("/", async Task<Results<Created<PredictionResponse>, ValidationProblem, NotFound>> (
            CreatePredictionCommand command,
            ISender sender) =>
        {
            try
            {
                var result = await sender.Send(command);
                return TypedResults.Created($"/api/v1/predictions/{result.Id}", result);
            }
            catch (ValidationException ex)
            {
                var errors = ex.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray());
                return TypedResults.ValidationProblem(errors);
            }
            catch (InvalidOperationException)
            {
                return TypedResults.NotFound();
            }
        })
        .WithName("CreatePrediction")
        .WithSummary("Submit a score prediction for a match")
        .ProducesValidationProblem();

        group.MapPut("/{id:guid}", async Task<Results<Ok<PredictionResponse>, NotFound, BadRequest<string>>> (
            Guid id,
            UpdatePredictionCommand command,
            ISender sender) =>
        {
            if (id != command.PredictionId)
            {
                return TypedResults.BadRequest("Route id does not match command PredictionId.");
            }

            try
            {
                var result = await sender.Send(command);
                return TypedResults.Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return TypedResults.NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return TypedResults.BadRequest(ex.Message);
            }
        })
        .WithName("UpdatePrediction")
        .WithSummary("Update the predicted scores for an existing prediction");
    }
}
