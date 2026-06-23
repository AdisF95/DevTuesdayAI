# Brief: Award Prediction Points

## Context
Players earn points when a match finishes: 3 pts for an exact scoreline, 1 pt for the correct result, 0 for wrong. Points are awarded for **every match** — group stage and knockout phase alike. Today points are calculated on-the-fly in `UserLeaderboardCalculator` — they are never persisted, so there is no record of *when* points were awarded or a reliable way to notify users. This feature persists earned points onto each `Prediction` row the moment the Sync job marks a match as `Finished`, triggered asynchronously via a MediatR domain event. The leaderboard is updated to read persisted points and must show **all users** with their total score.

---

## Scope

### Domain / DB changes

**`Prediction` entity — add two nullable columns:**
- `int? Points` — null until awarded; 0, 1, or 3 once set
- `DateTimeOffset? PointsAwardedAtUtc` — null until awarded; stamped when points are written

**New migration:** `AddPointsToPrediction`

**`IEntityTypeConfiguration<Prediction>` (`Infrastructure/Persistence/Configurations/PredictionConfiguration.cs`):** add `Points` and `PointsAwardedAtUtc` to the mapping (both nullable, no default).

### Domain event

**`MatchFinishedEvent`** (`Features/Sync/MatchFinishedEvent.cs`)
```csharp
record MatchFinishedEvent(Guid MatchId, int HomeScore, int AwayScore) : INotification;
```

### Notification handler

**`AwardPredictionPointsHandler : INotificationHandler<MatchFinishedEvent>`** (`Features/Predictions/AwardPredictionPointsHandler.cs`)

Logic:
1. Load all `Prediction` rows for `MatchId` where `PointsAwardedAtUtc == null` (idempotent guard — safe to re-publish).
2. For each prediction, call `PredictionPointsCalculator.Calculate(...)` (see below).
3. Set `prediction.Points` and `prediction.PointsAwardedAtUtc = DateTimeOffset.UtcNow`.
4. `SaveChangesAsync()`.

**`PredictionPointsCalculator`** (`Features/Predictions/PredictionPointsCalculator.cs`) — `internal static` class, purely testable:
```csharp
internal static class PredictionPointsCalculator
{
    internal static int Calculate(
        int predictedHome, int predictedAway,
        int actualHome,    int actualAway)
    {
        if (predictedHome == actualHome && predictedAway == actualAway)
            return 3;

        var predictedResult = Math.Sign(predictedHome - predictedAway);
        var actualResult    = Math.Sign(actualHome    - actualAway);

        return predictedResult == actualResult ? 1 : 0;
    }
}
```

### Sync integration

**File:** `Features/Sync/SyncCommand.cs` — in `SyncMatchesAsync`, after writing a match update to the DB, detect a Finished transition and publish the event:

```csharp
// Detect transition: previous status was not Finished; incoming status is Finished
bool justFinished = existingMatch.Status != MatchStatus.Finished
                 && incomingStatus == MatchStatus.Finished
                 && incomingHomeScore.HasValue
                 && incomingAwayScore.HasValue;

// ... apply all field updates to existingMatch ...

if (justFinished)
    pendingEvents.Add(new MatchFinishedEvent(
        existingMatch.Id,
        incomingHomeScore!.Value,
        incomingAwayScore!.Value));
```

After `await db.SaveChangesAsync()` (once, for the whole batch), publish all collected events:
```csharp
foreach (var evt in pendingEvents)
    await publisher.Publish(evt, cancellationToken);
```

Inject `IPublisher publisher` into the `SyncCommand` handler constructor (already has `WorldCuppyDbContext`).

> **Why collect then publish:** publishing *before* `SaveChangesAsync` would award points against unsaved scores. Publishing one-by-one inside the loop would also work but collecting and flushing after the save is cleaner.

### Leaderboard query update

**`GetUserLeaderboardQuery`:** rewrite the query to read `Prediction.Points` directly from the DB rather than recalculating via `UserLeaderboardCalculator`. The query must:
- Join `Users` → `Predictions` (left join so users with zero predictions still appear)
- Filter predictions to those with `PointsAwardedAtUtc != null`
- `SUM(p.Points)` per user (null predictions → 0 via `COALESCE` / default)
- Include **every registered user**, even those who have made no predictions (0 points, 0 predictions)
- Return `UserLeaderboardEntry` with: `Rank`, `Username`, `TotalPoints`, `ExactScoreCount`, `CorrectResultCount`, `PredictionCount`
- Order: TotalPoints DESC → Username ASC (consistent tie-break)

`UserLeaderboardCalculator` must remain in place — existing tests depend on it. The updated handler no longer calls it; it issues the DB query directly.

**`Leaderboard.razor`:** the existing page already renders `GetUserLeaderboardQuery` results. No layout changes are required — it will automatically show all users once the query returns them.

---

## Acceptance Criteria

- [ ] `Prediction.Points` and `Prediction.PointsAwardedAtUtc` columns exist after migration
- [ ] When SyncCommand sets a match to `Finished`, `AwardPredictionPointsHandler` fires and all predictions for that match get `Points` and `PointsAwardedAtUtc` populated
- [ ] Exact scoreline prediction receives 3 pts (unit test)
- [ ] Correct result (wrong scores, right winner/draw) receives 1 pt (unit test)
- [ ] Wrong result receives 0 pts (unit test)
- [ ] Handler is idempotent: re-publishing `MatchFinishedEvent` for the same match does not overwrite already-awarded points (unit or integration test)
- [ ] Handler only processes predictions with `PointsAwardedAtUtc == null`
- [ ] Points are awarded for group stage matches, not only knockout matches
- [ ] `GET /api/v1/leaderboard/users` returns **every registered user**, including those with no predictions (0 points)
- [ ] Leaderboard totals are read from persisted `Prediction.Points`, not recalculated on-the-fly
- [ ] `dotnet build` passes with zero warnings
- [ ] `dotnet test` passes — see test requirements below

---

## Test requirements

| Test class | Type | Covers |
|---|---|---|
| `PredictionPointsCalculatorTests` | Unit | Exact (3 pts); correct result home win, away win, draw (1 pt each); wrong result (0 pts) |
| `AwardPredictionPointsHandlerTests` | Integration | Happy path: multiple predictions for a finished match all receive correct points; idempotency: second event publish does not re-award; match with no predictions is a no-op; group-stage match awards points same as knockout |
| `GetUserLeaderboardQueryTests` (extend existing) | Integration | User with no predictions appears with 0 points; totals read from persisted `Points` column, not recalculated |

---

## Out of Scope
- Notifying users (push / email / in-app) that points were awarded
- Recalculating or adjusting points if a result is corrected post-sync (treat first Finished write as authoritative)
- Exposing awarded points via a dedicated endpoint beyond what the leaderboard already surfaces

---

## Notes & Gotchas

- `SyncCommand.SyncMatchesAsync` currently **skips** rows where `existingMatch.Status == MatchStatus.Finished` (`continue` guard at the top of the loop). The transition detection must happen **before** that guard fires — i.e. if the match is freshly becoming Finished this cycle, do not skip it, detect the transition, then continue normally.
- `PredictionPointsCalculator.Calculate` must mirror the logic in `UserLeaderboardCalculator` exactly — both use `Math.Sign` to determine result outcome.
- `IPublisher` is MediatR's fire-and-forget publisher for `INotification`; use it, never `ISender`, per CLAUDE.md.
- `InternalsVisibleTo("WorldCuppy.Tests")` is already set — `internal static` `PredictionPointsCalculator` is directly testable from the test project.
- Bogus username safety: `name[..Math.Min(n, name.Length)]` when generating test data.
- EF Core version pinning: if the test project adds new packages, pin `Microsoft.EntityFrameworkCore` and `Npgsql.EntityFrameworkCore.PostgreSQL` to the versions already in `WorldCuppy.Tests.csproj`.
