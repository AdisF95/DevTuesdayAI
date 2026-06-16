---
name: scoring-rule
description: Implements the WorldCuppy points-awarding logic — AwardPointsCommand, scoring calculation (exact=3pts, correct result=1pt, wrong=0), idempotency guard, and EF bulk update. Use this skill whenever the user asks to implement scoring, award points, calculate predictions results, process match results, or says things like "score the predictions for match X", "award points when a match finishes", "implement the scoring system".
---

# Implement Scoring Rule

You are implementing the core prediction-scoring mechanic in WorldCuppy. Points are awarded per the domain rules:

| Outcome | Points |
|---|---|
| Exact scoreline (home AND away score match) | 3 |
| Correct result (win/draw/loss), wrong score | 1 |
| Wrong result | 0 |

Scoring is **idempotent**: running it twice for the same match must not double-award points.

## Step 1: Check the Prediction entity for a points field

Open `WorldCuppy/Domain/Prediction.cs`. If it does not have `PointsAwarded` and `PointsCalculatedAtUtc` properties, add them now:

```csharp
/// <summary>Points awarded for this prediction; null means scoring has not run yet.</summary>
public int? PointsAwarded { get; set; }

/// <summary>UTC timestamp when points were calculated; null if not yet scored.</summary>
public DateTimeOffset? PointsCalculatedAtUtc { get; set; }
```

Then update `PredictionConfiguration.cs` to map them:

```csharp
builder.Property(p => p.PointsAwarded);           // nullable int — no special config needed
builder.Property(p => p.PointsCalculatedAtUtc);   // nullable DateTimeOffset
```

Create a migration:

```powershell
dotnet ef migrations add AddPredictionScoring --project WorldCuppy/WorldCuppy.csproj
```

## Step 2: Create the feature slice

File: `WorldCuppy/Features/Scoring/AwardPointsCommand.cs`

```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using WorldCuppy.Domain;
using WorldCuppy.Infrastructure.Persistence;

namespace WorldCuppy.Features.Scoring;

/// <summary>
/// Awards points to all predictions for a finished match.
/// Safe to call multiple times — predictions that already have points are skipped.
/// </summary>
/// <param name="MatchId">The finished match to score.</param>
public record AwardPointsCommand(Guid MatchId) : IRequest<AwardPointsResult>;

/// <summary>Summary of the scoring run.</summary>
/// <param name="PredictionsScored">Number of predictions that received points in this run.</param>
/// <param name="PredictionsSkipped">Number of predictions already scored (idempotency guard).</param>
public record AwardPointsResult(int PredictionsScored, int PredictionsSkipped);

/// <summary>Handles <see cref="AwardPointsCommand" />.</summary>
public class AwardPointsHandler(WorldCuppyDbContext db)
    : IRequestHandler<AwardPointsCommand, AwardPointsResult>
{
    /// <summary>
    /// Loads the match and all unscored predictions, calculates points per the domain
    /// scoring table, and persists the results in a single SaveChanges call.
    /// </summary>
    public async Task<AwardPointsResult> Handle(
        AwardPointsCommand request,
        CancellationToken cancellationToken)
    {
        var match = await db.Matches
            .FirstOrDefaultAsync(m => m.Id == request.MatchId, cancellationToken)
            ?? throw new InvalidOperationException($"Match {request.MatchId} not found.");

        if (match.Status != MatchStatus.Finished)
            throw new InvalidOperationException(
                $"Cannot score match {request.MatchId} — status is {match.Status}, expected Finished.");

        if (match.HomeScore is null || match.AwayScore is null)
            throw new InvalidOperationException(
                $"Match {request.MatchId} has no final score recorded.");

        // Load only predictions that have not yet been scored (idempotency guard)
        var unscored = await db.Predictions
            .Where(p => p.MatchId == request.MatchId && p.PointsCalculatedAtUtc == null)
            .ToListAsync(cancellationToken);

        var alreadyScored = await db.Predictions
            .CountAsync(p => p.MatchId == request.MatchId && p.PointsCalculatedAtUtc != null,
                cancellationToken);

        var now = DateTimeOffset.UtcNow;

        foreach (var prediction in unscored)
        {
            prediction.PointsAwarded = CalculatePoints(
                prediction.PredictedHomeScore,
                prediction.PredictedAwayScore,
                match.HomeScore.Value,
                match.AwayScore.Value);

            prediction.PointsCalculatedAtUtc = now;
        }

        await db.SaveChangesAsync(cancellationToken);

        return new AwardPointsResult(unscored.Count, alreadyScored);
    }

    /// <summary>
    /// Applies the WorldCuppy scoring table: exact scoreline = 3 pts,
    /// correct result (win/draw/loss) = 1 pt, wrong result = 0 pts.
    /// </summary>
    private static int CalculatePoints(
        int predictedHome, int predictedAway,
        int actualHome, int actualAway)
    {
        // Exact scoreline
        if (predictedHome == actualHome && predictedAway == actualAway)
            return 3;

        // Correct match result (same winner/draw outcome)
        var predictedResult = Math.Sign(predictedHome - predictedAway);
        var actualResult = Math.Sign(actualHome - actualAway);
        if (predictedResult == actualResult)
            return 1;

        return 0;
    }
}
```

## Step 3: Create the validator

File: `WorldCuppy/Features/Scoring/AwardPointsValidator.cs`

```csharp
using FluentValidation;

namespace WorldCuppy.Features.Scoring;

/// <summary>Validates <see cref="AwardPointsCommand" /> input.</summary>
public class AwardPointsValidator : AbstractValidator<AwardPointsCommand>
{
    /// <summary>Ensures the MatchId is not empty.</summary>
    public AwardPointsValidator()
    {
        RuleFor(x => x.MatchId).NotEmpty();
    }
}
```

## Step 4: Create the endpoint

File: `WorldCuppy/Features/Scoring/ScoringEndpoints.cs`

```csharp
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace WorldCuppy.Features.Scoring;

/// <summary>Registers scoring API routes.</summary>
public static class ScoringEndpoints
{
    /// <summary>Maps scoring endpoints onto <paramref name="app" />.</summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/scoring").WithTags("Scoring");

        group.MapPost("/matches/{matchId:guid}/award-points",
            async Task<Results<Ok<AwardPointsResult>, NotFound, BadRequest<string>>>
            (Guid matchId, ISender sender) =>
            {
                try
                {
                    var result = await sender.Send(new AwardPointsCommand(matchId));
                    return TypedResults.Ok(result);
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.BadRequest(ex.Message);
                }
            })
        .WithName("AwardPoints")
        .WithSummary("Award points to all predictions for a finished match. Idempotent.")
        .ProducesValidationProblem();
    }
}
```

## Step 5: Wire into EndpointExtensions.cs

Open `WorldCuppy/Infrastructure/Extensions/EndpointExtensions.cs` and add:

```csharp
using WorldCuppy.Features.Scoring;

// inside MapAllEndpoints:
ScoringEndpoints.MapEndpoints(app);
```

## Step 6: Verify

```powershell
dotnet build WorldCuppy/WorldCuppy.csproj
```

0 errors, 0 warnings.

## Idempotency design explained

The guard is `p.PointsCalculatedAtUtc == null` in the query. First scoring run sets this timestamp; second run finds 0 unscored records and returns `PredictionsScored = 0, PredictionsSkipped = N`. This means:
- Re-triggering after a result correction requires a reset step (set `PointsCalculatedAtUtc = null`) — that is intentional; points resets are a separate admin operation
- No locks needed because `SaveChangesAsync` is a single atomic write per run

## Optional: trigger scoring from a Hangfire job

If you want scoring to run automatically when a match finishes, create `ScoringJob` using the `hangfire-job` skill and dispatch `AwardPointsCommand` for each just-finished match.
