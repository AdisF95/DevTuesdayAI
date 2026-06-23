# Brief: Predictions Page

## Context
Logged-in users predict upcoming match scores to earn points. Scoring: exact scoreline = 3 pts, correct result = 1 pt, wrong = 0 pts. Predictions are only meaningful for matches not yet kicked off. This brief covers the missing update command, a combined upcoming-matches-with-predictions query, and the Blazor page at `/predictions` (the nav link already exists but the page file was never created).

---

## Scope

### Existing code — reuse or extend (do not replace)

| Item | Action |
|---|---|
| `Prediction` entity | Keep as-is — no schema changes needed |
| `CreatePredictionCommand` | Fix: add `Status == MatchStatus.Scheduled` guard in the handler (returns 400 if match is Live/Finished/Postponed/Cancelled) |
| `CreatePredictionValidator` | Fix: add `LessThanOrEqualTo(20)` to both score rules (football max) |
| `GetPredictionsByUserQuery` | Keep as-is — still used by the existing API endpoint |
| `PredictionsEndpoints.cs` | Extend with two new endpoint registrations (see below) |

### Commands / Queries to add

**`GetUpcomingMatchesWithPredictionsQuery`**
- Input: `Guid UserId`
- Filter: `Status == MatchStatus.Scheduled` only — excludes Live, Finished, Postponed, Cancelled
- Returns: all scheduled matches left-joined with the user's prediction, ordered by `KickoffUtc` ascending
- Response DTO: `UpcomingMatchPredictionResponse` (new — see below); place in `Features/Predictions/`
- Note: use `.Select()` projection — no `.Include()`

**`UpdatePredictionCommand`**
- Input: `Guid PredictionId`, `Guid UserId`, `int PredictedHomeScore`, `int PredictedAwayScore`
- Validates: prediction exists; prediction belongs to `UserId` (return 404 if not); match is still `Scheduled` (return 400 otherwise); scores 0–20
- Returns: `PredictionResponse` (existing DTO)
- Validator: `UpdatePredictionValidator` — same score rules as the fixed `CreatePredictionValidator`

### New response DTO

```csharp
// Features/Predictions/UpcomingMatchPredictionResponse.cs
record UpcomingMatchPredictionResponse(
    Guid      MatchId,
    string    HomeTeamName,
    string    HomeTeamCode,
    string?   HomeTeamCrestUrl,
    string    AwayTeamName,
    string    AwayTeamCode,
    string?   AwayTeamCrestUrl,
    DateTime  KickoffUtc,
    int       GameDay,
    // null when the user has no prediction for this match yet
    Guid?     PredictionId,
    int?      PredictedHomeScore,
    int?      PredictedAwayScore
);
```

### API Endpoints (add to `PredictionsEndpoints.cs`)
- `GET /api/v1/predictions/upcoming/{userId}` → `GetUpcomingMatchesWithPredictionsQuery` → 200 `UpcomingMatchPredictionResponse[]`
- `PUT /api/v1/predictions/{id}` → `UpdatePredictionCommand` → 200 `PredictionResponse` (404 / 400 on failure)

### Blazor UI

**File:** `WorldCuppy/Components/Pages/Predictions.razor`
> This file does not exist yet. The nav link in the layout already points to `/predictions`.

- `@page "/predictions"`
- `@rendermode InteractiveServer` (user submits and updates predictions)
- Requires authentication — wrap entire content in `<AuthorizeView>` and show a "Please log in" prompt with a login link for unauthenticated users; do not use `[Authorize]` attribute (Blazor Server auth flows through `AuthorizeView`)
- Read `UserId` from auth state: inject `[CascadingParameter] Task<AuthenticationState> AuthState` and parse `ClaimTypes.NameIdentifier` — this is the first page in the app to read auth claims directly, establish the pattern clearly

**Page layout:**
1. Heading: "My Predictions"
2. Loading state while `GetUpcomingMatchesWithPredictionsQuery` is in-flight
3. Empty state (`MudText`) if no scheduled matches are returned
4. Matches grouped by `GameDay` (use `MudText` as a day heading) and ordered by `KickoffUtc`
5. Each match row / card shows:
   - Home team crest (`MudImage`, fallback to team code if `CrestUrl` is null) + name
   - Away team crest + name
   - Kickoff date and time (local browser time via `@DateTime.ToLocalTime()`)
   - Two `MudNumericField<int>` inputs: home score and away score (`Min="0"` `Max="20"`)
   - If prediction exists: inputs pre-filled; button label "Update" → calls `UpdatePredictionCommand`
   - If no prediction: inputs at 0; button label "Predict" → calls `CreatePredictionCommand`
6. `MudSnackbar` on success: "Prediction saved ✓"
7. `MudSnackbar` on error: "Something went wrong, please try again"
8. After a successful save/update, refresh the list (re-run the query) so the button flips from "Predict" to "Update" and the inputs reflect the saved values

**Component split guidance:** if the per-match card (team names, crests, inputs, button) exceeds ~30 lines inline, extract it to `Components/Predictions/PredictionCard.razor` accepting the `UpcomingMatchPredictionResponse` as a `[Parameter]` and `EventCallback<(int home, int away)> OnSave`.

---

## Acceptance Criteria

- [ ] `GET /api/v1/predictions/upcoming/{userId}` returns only `Scheduled` matches, ordered by kickoff ascending
- [ ] Each row has `PredictionId`/`PredictedHomeScore`/`PredictedAwayScore` populated when a prediction exists; all three are `null` otherwise
- [ ] `POST /api/v1/predictions` returns 400 when the match is not `Scheduled`
- [ ] `PUT /api/v1/predictions/{id}` returns 404 when prediction not found or does not belong to the user
- [ ] `PUT /api/v1/predictions/{id}` returns 400 when the match is no longer `Scheduled`
- [ ] Score < 0 or score > 20 fails FluentValidation on both create and update
- [ ] `/predictions` redirects/prompts login for unauthenticated users
- [ ] Page pre-fills existing predictions correctly
- [ ] "Predict" button becomes "Update" after a successful save without a full page reload
- [ ] Success and error snackbars appear correctly
- [ ] `dotnet build` passes with zero warnings
- [ ] `dotnet test` passes — see test requirements below

---

## Test requirements

| Test class | Type | Covers |
|---|---|---|
| `UpdatePredictionValidatorTests` | Unit | Score range 0–20; empty `PredictionId`; empty `UserId` |
| `GetUpcomingMatchesWithPredictionsQueryTests` | Integration | Scheduled match with prediction; scheduled match without prediction; Live/Finished matches excluded; result order by kickoff |
| `UpdatePredictionCommandTests` | Integration | Happy path; wrong owner returns not-found; match no longer Scheduled returns error; score out of range |
| `CreatePredictionCommandTests` (extend existing) | Integration | Add: match not Scheduled returns error |

---

## Out of Scope
- Deleting predictions
- Predicting Live (in-progress) matches
- Showing earned points or leaderboard position
- Real-time score updates via SignalR
- Enforcing a prediction cutoff deadline beyond match `Status`

---

## Notes & Gotchas

- `UserId` in `Prediction` is a bare `Guid` — there is intentionally no FK to the `Users` table; keep it that way
- Claims: `ClaimTypes.NameIdentifier` in the auth cookie holds `user.Id.ToString()` (set in `ClaimsPrincipalFactory.cs`); parse with `Guid.Parse()`
- Match.Id is `Guid` — confirmed from `CreatePredictionCommand` which accepts `Guid MatchId`
- `MatchStatus` enum is at `WorldCuppy/Domain/MatchStatus.cs`
- The existing `CreatePredictionCommand` does not check match status — fix this as part of this brief
- `MudNumericField` HTML attributes must be lowercase to avoid MUD0002 build error
