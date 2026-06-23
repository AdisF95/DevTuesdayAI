# Brief: Knockout Bracket

## Context
WorldCuppy is a 2026 FIFA World Cup prediction game. The app already shows a flat
match schedule (`/matches`) and group standings (`/groups`), but there is no view of
the knockout stage as a bracket. This feature adds a public bracket visualisation
(Round of 32 → Final) backed by a new `GetBracketQuery`, so users can see the knockout
structure, who has advanced, and final scores (including extra time / penalties) at a glance.

---

## Scope

### Existing code — reuse, do not duplicate

| Item | Notes |
|---|---|
| `Match` entity | `Id` is `Guid`; `KickoffUtc` is `DateTimeOffset`; `Round` is `KnockoutRound?` (nullable enum, **null for group-stage matches**); `Status` is `MatchStatus`; `HomeTeam`/`AwayTeam` are `required` navigation props with `Guid` FK; `MatchDuration` is `string?` (`REGULAR` / `EXTRA_TIME` / `PENALTY_SHOOTOUT`) |
| `Team` entity | `Name` (string), `Code` (string), `CrestUrl` (`string?`) |
| `KnockoutRound` enum | `RoundOf32`, `RoundOf16`, `QuarterFinal`, `SemiFinal`, `Final` (`WorldCuppy/Domain/KnockoutRound.cs`) |
| `MatchStatus` enum | `Scheduled`, `Live`, `Finished`, `Postponed`, `Cancelled` |
| `GetAllMatchesQuery` | Reference pattern for projection-only queries — follow its `.Select()` style |
| `MatchesEndpoints.cs` | Reference pattern for `MapGroup` + `TypedResults` registration |
| `EndpointExtensions.cs` | Wire the new `BracketEndpoints.MapEndpoints` here |

### Commands / Queries to add

**`GetBracketQuery`** — `Features/Bracket/GetBracketQuery.cs`
- Input: none (parameterless record query)
- Returns: `BracketResponse`
- Filter: knockout matches only — `m.Round != null` (this is the reliable knockout filter; `Round` is null for all group-stage matches)
- Grouping: group matches by `Round`, emit one `BracketRoundDto` per round
- Round ordering: rounds must appear in fixed bracket order `RoundOf32 → RoundOf16 → QuarterFinal → SemiFinal → Final` (order by the `KnockoutRound` enum value, not alphabetically). Only emit rounds that have at least one match.
- Match ordering within a round: `KickoffUtc` ascending
- Projection only — no `.Include()` alongside `.Select()`
- Map with a static `ToResponse()` / projection; no AutoMapper

### Score formatting (extract for unit testing)

Extract an `internal static ScoreFormatter` class at `Features/Bracket/ScoreFormatter.cs` with a method:

```csharp
/// <summary>
/// Returns null when the match is not Finished; otherwise the formatted score string.
/// </summary>
internal static string? Format(
    MatchStatus status,
    string? matchDuration,
    int? homeScore, int? awayScore,
    int? extraTimeHomeScore, int? extraTimeAwayScore,
    int? penaltyHomeScore, int? penaltyAwayScore)
```

Rules (drive annotation off `MatchDuration`, not null-checks):
- `Status != Finished` → return `null` (no score shown for Scheduled / Live / Postponed / Cancelled)
- `MatchDuration == "PENALTY_SHOOTOUT"` → `"{HomeScore}–{AwayScore} ({PenaltyHomeScore}–{PenaltyAwayScore} pens)"` e.g. `"1–1 (4–2 pens)"`
- `MatchDuration == "EXTRA_TIME"` → `"{HomeScore}–{AwayScore} (aet)"` e.g. `"2–1 (aet)"`
- Otherwise (`REGULAR` or null) → `"{HomeScore}–{AwayScore}"` e.g. `"2–1"`
- Use an en-dash `–` (U+2013) as the separator, matching the examples in this brief.
- The full-time `HomeScore`/`AwayScore` are used as the base score in all three cases (the penalty parenthetical uses `PenaltyHomeScore`/`PenaltyAwayScore` for the pens score only).

### TBD slot handling

On the `Match` entity, `HomeTeam`/`AwayTeam` are `required` non-null navigations with non-nullable
`Guid` FKs, so a literal null team is unlikely in current data. Implement TBD defensively so
the page never crashes:
- If a team navigation is null, or its `Name` is null/empty after projection, emit `BracketTeamDto { Name = "TBD", Code = null, CrestUrl = null }`.
- Because projection (`.Select()`) reads `m.HomeTeam.Name` etc. directly, guard against null by projecting team fields as nullable and coalescing to `"TBD"` in the `ToResponse()` mapping. Do not throw on a missing team.

### Response DTOs — `Features/Bracket/BracketResponse.cs`

```csharp
/// <summary>Full knockout bracket grouped by round.</summary>
public record BracketResponse(List<BracketRoundDto> Rounds);

/// <summary>One knockout round and its matches.</summary>
public record BracketRoundDto(string Round, List<BracketMatchDto> Matches);

/// <summary>A single knockout fixture.</summary>
public record BracketMatchDto(
    Guid MatchId,
    DateTimeOffset KickoffUtc,
    BracketTeamDto HomeTeam,
    BracketTeamDto AwayTeam,
    string? Score,    // null if not Finished; "2–1" / "2–1 (aet)" / "1–1 (4–2 pens)"
    string Status);   // MatchStatus.ToString()

/// <summary>A team in a bracket slot, or a TBD placeholder.</summary>
public record BracketTeamDto(string Name, string? Code, string? CrestUrl);
```

Notes:
- `Match.Id` is `Guid` and `KickoffUtc` is `DateTimeOffset` — use those exact types in the DTO.
- `Round` is serialised as its enum name string (e.g. `"RoundOf16"`) via `.ToString()`.

### API Endpoints — `Features/Bracket/BracketEndpoints.cs`

- `GET /api/v1/bracket` → `GetBracketQuery` → `200 OK` `BracketResponse`
- No authentication required (public, same as `/matches`)
- Use `MapGroup("/api/v1/bracket").WithTags("Bracket")`, `ISender`, `TypedResults.Ok(...)`
- Register via `BracketEndpoints.MapEndpoints` wired through `Infrastructure/Extensions/EndpointExtensions.cs`
- Always returns 200 with `Rounds` (possibly an empty list) — no 404 for an empty bracket

### Blazor UI — `Components/Pages/Bracket.razor`

- `@page "/bracket"`
- Static SSR (do **not** add `@rendermode InteractiveServer` unless a loading state requires it — if the query is awaited in `OnInitializedAsync`, a brief loading state is acceptable under SSR)
- Public — **no** `AuthorizeView`, no auth required
- Send `GetBracketQuery` directly via injected `ISender` (keep the page thin)
- MudBlazor components only — no raw `<table>`/`<button>`/`<input>`. HTML attributes on Mud components must be lowercase (MUD0002 build error).

**Page layout (top to bottom):**
1. Page heading: "Knockout Bracket"
2. Loading state while the query is in-flight (e.g. `MudProgressCircular`)
3. Empty state (`MudText`) if `Rounds` is empty: "The knockout bracket isn't available yet."
4. One labelled section per round, in bracket order, with a human-readable round label:
   - `RoundOf32` → "Round of 32"
   - `RoundOf16` → "Round of 16"
   - `QuarterFinal` → "Quarter-finals"
   - `SemiFinal` → "Semi-finals"
   - `Final` → "Final"
5. Within each round, render each match as a `MudCard` / `MudPaper` showing:
   - Home team: crest (`MudImage`; if `CrestUrl` is null, omit the image) + name
   - Centre: `Score` string when non-null; otherwise the literal "vs"
   - Away team: crest (same null handling) + name
   - Kickoff date and time (display `KickoffUtc.ToLocalTime()`)
   - TBD teams (`Name == "TBD"`): show no crest and the label "TBD"
6. Use FIFA palette (`Color.Primary` green / `Color.Secondary` gold) consistent with other pages.

**Component split guidance:** if the per-match card exceeds ~30 lines inline, extract it to
`Components/Bracket/BracketMatchCard.razor` taking a `BracketMatchDto` as a `[Parameter]`.
Optionally extract a `Components/Bracket/BracketRoundSection.razor` mirroring the
`Matches/MatchDaySection.razor` pattern.

### Domain / DB changes

**None.** No new entities, no schema changes, no migration. The query reads existing
`Match` and `Team` rows only.

---

## Acceptance Criteria

- [ ] `GET /api/v1/bracket` returns `200 OK` with a `BracketResponse` for an unauthenticated request
- [ ] Only knockout matches (`Round != null`) are returned; group-stage matches are excluded (integration test)
- [ ] Rounds appear in fixed order `RoundOf32, RoundOf16, QuarterFinal, SemiFinal, Final`; rounds with no matches are omitted (integration test)
- [ ] Matches within a round are ordered by `KickoffUtc` ascending (integration test)
- [ ] A `Finished` regular-time match yields `Score == "2–1"` form (unit test on `ScoreFormatter`)
- [ ] A `Finished` extra-time match (`MatchDuration == "EXTRA_TIME"`) yields the `"(aet)"` annotation (unit test)
- [ ] A `Finished` penalty match (`MatchDuration == "PENALTY_SHOOTOUT"`) yields the `"(X–Y pens)"` annotation (unit test)
- [ ] A non-`Finished` match yields `Score == null` regardless of `MatchDuration` (unit test + integration test)
- [ ] A match with a missing/unknown team yields a `BracketTeamDto` with `Name == "TBD"`, `Code == null`, `CrestUrl == null`; the query does not throw (integration test)
- [ ] An empty bracket returns `200 OK` with an empty `Rounds` list (integration test)
- [ ] `/bracket` renders without errors and is reachable without logging in
- [ ] `.claude/rules/feature-index.md` updated with the Bracket feature row and the `/bracket` page row
- [ ] `dotnet build` passes with zero warnings (`TreatWarningsAsErrors=true`)
- [ ] `dotnet test` passes — see test requirements below

---

## Test requirements

| Test class | Type | What it covers |
|---|---|---|
| `ScoreFormatterTests` | Unit | Not-Finished → null; regular → "2–1"; extra time → "2–1 (aet)"; penalties → "1–1 (4–2 pens)"; en-dash separator used |
| `GetBracketQueryTests` | Integration | Round grouping; fixed round ordering; kickoff ordering within a round; group-stage matches excluded; TBD placeholder for missing team; score annotation end-to-end; empty bracket returns empty `Rounds` |

- Unit tests: xUnit + Bogus; start from a valid input and mutate one field per test. `MatchDuration` literals (`"EXTRA_TIME"` etc.) are the things under test — hardcoding them is fine.
- Integration test: `PostgreSqlFixture` (`IClassFixture<PostgreSqlFixture>`); seed `Team` + `Match` rows; use separate `db.CreateDbContext()` instances for seeding, the handler under test, and post-act verification.
- Bogus username/team-name safety: clamp slices with `name[..Math.Min(n, name.Length)]`.

---

## Out of Scope

- Interactive bracket navigation, zoom/pan, or connector-line SVG drawing (render as grouped sections, not a literal connected tree)
- Predicting bracket outcomes / "fill in your bracket" functionality
- Real-time / live score updates (SignalR)
- Auth gating or per-user personalisation
- Any new nav-bar link (add one only if the user later requests it — this brief does not change `MainLayout.razor`)
- Modifying the existing `/matches` or `/groups` pages

---

## Notes & Gotchas

- `Match.Round` is a **`KnockoutRound?` nullable enum** — filter with `m.Round != null` and order by the enum value. Do not compare against string values.
- `Match.Id` is `Guid` and `KickoffUtc` is `DateTimeOffset` — use those types in the DTO (not `int`/`DateTime`).
- `HomeTeam`/`AwayTeam` are `required` non-null navigations — implement TBD defensively (coalesce to `"TBD"`) so the page is robust if data is ever incomplete.
- Drive score annotation off `MatchDuration` (`REGULAR`/`EXTRA_TIME`/`PENALTY_SHOOTOUT`), not by null-checking score fields — more reliable per the data model.
- Follow the projection-only pattern from `GetAllMatchesQuery.cs`; never combine `.Include()` with `.Select()`.
- Register the endpoint through `Infrastructure/Extensions/EndpointExtensions.cs` like the other features; use `ISender` (never `IMediator`), `TypedResults`, and `MapGroup("/api/v1/bracket")`.
- XML `<summary>` doc comments on every public class, method, and constructor; file-scoped namespaces throughout.
- MudBlazor only; HTML attributes on Mud components must be lowercase (MUD0002 build error). IDE0011 requires braces on every `if`.
- Update `.claude/rules/feature-index.md`:
  - Add a **Bracket** row to the Features table: handler `GetBracketQuery`, endpoint `GET /api/v1/bracket`, page `/bracket`
  - Add a `/bracket` row to the Blazor Pages & Components table
  - Add `ScoreFormatterTests` (Unit) and `GetBracketQueryTests` (Integration) to the Test Coverage table
