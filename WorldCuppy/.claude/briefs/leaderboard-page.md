# Brief: Leaderboard Page

## Context
WorldCuppy is a prediction game where users earn points for correctly predicting match scorelines (3 pts exact, 1 pt correct result, 0 pts wrong). The nav bar already links to `/leaderboard` but the page does not exist yet. This feature builds the public user prediction leaderboard — anyone can see who is leading without needing to log in.

## Scope

### Commands / Queries
- `GetUserLeaderboardQuery` — joins `Users`, `Predictions`, and finished `Matches` to calculate total prediction points per user; returns a ranked list. Scoring: exact scoreline = 3 pts, correct result (win/draw/loss outcome matches) = 1 pt, wrong result = 0 pts. Delegate the per-prediction scoring logic to an `internal static` `UserLeaderboardCalculator` class so it is unit-testable.

### API Endpoints
- `GET /api/v1/leaderboard/users` — returns `[ { rank, username, totalPoints, predictionsCount, exactScores, correctResults } ]` ordered by `totalPoints desc`, then `username asc`. No authentication required. Register under the existing `LeaderboardEndpoints.MapEndpoints`.

### Blazor UI
- Page at `/leaderboard` — renders a `MudTable` with columns: **Rank**, **Player**, **Points**, **Exact Scores**, **Correct Results**, **Predictions**. No auth gate — the page is public. Show a loading skeleton while fetching. Show an empty state (`MudText`) if no predictions exist yet. Highlight the logged-in user's row with `Color.Primary` background if they appear in the list (use `AuthenticationStateProvider` to get the current username, but do not require login).

### Domain / DB changes
- No new entities or migrations. Query reads from `Users`, `Predictions`, and `Matches` tables that already exist.

## Acceptance Criteria
- [ ] `GET /api/v1/leaderboard/users` returns `200 OK` with a ranked list (unauthenticated request succeeds)
- [ ] Exact scoreline prediction scores 3 pts (unit test via `UserLeaderboardCalculator`)
- [ ] Correct result (non-exact) scores 1 pt (unit test)
- [ ] Wrong result scores 0 pts (unit test)
- [ ] Ties broken by `username asc` (unit test)
- [ ] Handler integration test: seeds users + predictions + a finished match; verifies ranking and point totals
- [ ] Handler integration test: returns empty list when no predictions exist
- [ ] Blazor page builds without errors (`dotnet build` passes)

## Out of Scope
- Real-time score updates
- Pagination (show all users for now)
- Per-match breakdown / drill-down view
- Admin controls or filtering
- The existing team standings leaderboard (`GET /api/v1/leaderboard`) — leave it unchanged

## Notes
- `Prediction.UserId` is a bare `Guid` with no FK to `Users` yet — join via `db.Users.Where(u => u.Id == p.UserId)` or use a left-join; users with no matching `User` row should be skipped gracefully
- `Match.HomeScore` / `AwayScore` are nullable — only count predictions where the match has both scores set (i.e. finished)
- Scoring logic lives in `UserLeaderboardCalculator` (internal static, same pattern as `LeaderboardCalculator`) — extract before writing the handler so you can unit-test it independently
- The `/leaderboard` nav link already exists in `MainLayout.razor` — no nav changes needed
- Reuse `AuthenticationStateProvider` pattern from `Predictions.razor` for the optional row highlight
- Add the new endpoint to `LeaderboardEndpoints.MapEndpoints` rather than creating a second endpoints class
