# Feature & Entity Index

Quick-reference for what exists in the codebase. Update this file whenever a feature, entity, or page is added or removed.

---

## Features

| Feature | Handlers / Commands / Queries | API Endpoints | Blazor Page |
|---|---|---|---|
| **Users** | `RegisterUserCommand`, `LoginUserQuery`, `RegisterUserValidator` | `GET /account/complete-auth/{token}`, `GET /account/logout` | `/register`, `/login` |
| **Predictions** | `CreatePredictionCommand`, `CreatePredictionValidator`, `GetPredictionsByUserQuery` | `POST /api/v1/predictions`, `GET /api/v1/predictions/user/{userId}` | `/predictions` *(nav link exists, page not yet built)* |
| **Matches** | `GetMatchByIdQuery`, `GetMatchesByGameDayQuery`, `GetAllMatchesQuery`, `GetMatchDetailQuery` | `GET /api/v1/matches/{id}`, `GET /api/v1/matches?gameDay=` | `/matches` — scroll view of all matches grouped by day, auto-scrolls to today, matchday filter chips, click-to-detail dialog |
| **Groups** | `GetGroupStandingsQuery` | — | `/groups` — group stage standings tables with form indicators |
| **Teams** | `GetTeamsQuery`, `GetTeamByCodeQuery` | `GET /api/v1/teams`, `GET /api/v1/teams/{code}` | — |
| **Leaderboard** | `GetLeaderboardQuery`, `LeaderboardCalculator` (internal static) | `GET /api/v1/leaderboard` | `/leaderboard` *(nav link exists, page not yet built)* |
| **Sync** | `SyncCommand`, `SyncJob` (Hangfire) | `POST /api/v1/sync` (manual trigger) | — |

---

## Domain Entities

| Entity | Key Columns | Constraints | Notes |
|---|---|---|---|
| `User` | `Id (Guid)`, `Username`, `Email`, `PasswordHash`, `CreatedAtUtc` | Unique index on `Username`, unique index on `Email` | PBKDF2 hashed password |
| `Match` | `Id`, `HomeTeamId`, `AwayTeamId`, `KickoffUtc`, `GameDay`, `Round`, `Venue`, `Group`, `HomeScore?`, `AwayScore?`, `HalfTimeHomeScore?`, `HalfTimeAwayScore?`, `ExtraTimeHomeScore?`, `ExtraTimeAwayScore?`, `PenaltyHomeScore?`, `PenaltyAwayScore?`, `MatchDuration?`, `Status`, `ExternalId` | Unique index on `ExternalId`; FK to `Team` ×2 (Restrict) | `Round` and `Status` stored as string; `MatchDuration`: REGULAR / EXTRA_TIME / PENALTY_SHOOTOUT |
| `Team` | `Id`, `Name`, `Code` (3-char), `CrestUrl?`, `ExternalId` | Unique index on `ExternalId` | Synced from football-data.org |
| `Prediction` | `Id`, `UserId (Guid)`, `MatchId`, `PredictedHomeScore`, `PredictedAwayScore`, `SubmittedAtUtc` | Composite unique index on `(UserId, MatchId)`; FK to `Match` (Cascade) | `UserId` is a bare Guid — no FK to `Users` table yet |
| `GoalEvent` | `Id`, `MatchId`, `Minute`, `ScorerName?`, `AssistName?`, `Type`, `TeamName`, `IsHomeTeam` | FK to `Match` (Cascade) | Type: REGULAR / OWN_GOAL / PENALTY; synced via `GET /v4/matches/{id}` |
| `BookingEvent` | `Id`, `MatchId`, `Minute`, `PlayerName?`, `CardType`, `IsHomeTeam` | FK to `Match` (Cascade) | CardType: YELLOW_CARD / RED_CARD / YELLOW_RED_CARD; same sync call as GoalEvent |
| `GroupStanding` | `Id`, `Group`, `TeamId`, `Position`, `PlayedGames`, `Won`, `Draw`, `Lost`, `GoalsFor`, `GoalsAgainst`, `GoalDifference`, `Points`, `Form?` | Unique index on `(Group, TeamId)`; FK to `Team` (Restrict) | Synced from `GET /v4/competitions/WC/standings` every 15 min |

## Enums

| Enum | Values |
|---|---|
| `KnockoutRound` | `RoundOf32`, `RoundOf16`, `QuarterFinal`, `SemiFinal`, `Final` |
| `MatchStatus` | `Scheduled`, `Live`, `Finished`, `Postponed`, `Cancelled` |

---

## Blazor Pages & Components

| Path | File | Render Mode | What it does |
|---|---|---|---|
| `/` | `Pages/Home.razor` | Static SSR | Today/tomorrow match schedule via `GetMatchesByGameDayQuery` |
| `/login` | `Pages/Login.razor` | Interactive Server | Login form → `LoginUserQuery` → cookie via PendingAuthStore |
| `/register` | `Pages/Register.razor` | Interactive Server | Register form → `RegisterUserCommand` → cookie via PendingAuthStore |
| `/matches` | `Pages/Matches.razor` | Interactive Server | All matches grouped by day; matchday filter chips; auto-scrolls to today on load; click a card to open `MatchDetailDialog` |
| `/groups` | `Pages/Groups.razor` | Interactive Server | Group stage standings tables (Pos/W/D/L/GD/Pts/Form) with crest + form colour chips |

**Shared components:** `Matches/MatchCard.razor`, `Matches/MatchDaySection.razor`, `Matches/MatchDetailDialog.razor`

---

## Infrastructure

| Area | Key Files |
|---|---|
| Auth | `Infrastructure/Auth/PendingAuthStore.cs`, `PasswordHasher.cs`, `ClaimsPrincipalFactory.cs` |
| Extensions | `AuthExtensions.cs`, `ApplicationExtensions.cs`, `PersistenceExtensions.cs`, `FootballDataExtensions.cs`, `HangfireExtensions.cs`, `EndpointExtensions.cs` |
| External data | `Infrastructure/FootballData/FootballDataClient.cs` — syncs matches + teams + standings from football-data.org every 15 min via Hangfire; per-match goals/bookings fetched once per finished match via `GET /v4/matches/{id}`; **teams skipped if already populated** (fixed roster); **matches scoped to a yesterday → +5 day window** after initial full-fetch, FINISHED matches never re-updated (scores are final); fixed calls (matches, standings) retry on 429 with 65 s back-off; **match event calls do not retry** — 429 breaks the batch immediately and remaining matches carry forward to the next cycle; **batch capped at 10 events per sync** paced at 10 s apart (~1.7 min worst case, well within the 15-min window); rate pressure is an initial-seeding concern only — steady state is 1–2 event calls per sync |

---

## Test Coverage

| Test class | Type | What it covers |
|---|---|---|
| `RegisterUserValidatorTests` | Unit | Username/email/password validation rules |
| `RegisterUserCommandTests` | Integration | Happy path, duplicate username, duplicate email |
| `LoginUserQueryTests` | Integration | Login by username, by email, wrong password, unknown user |
| `CreatePredictionValidatorTests` | Unit | Score range, required Guids |
| `LeaderboardCalculatorTests` | Unit | Ranking logic, points, goals |
| `SyncHandlerMappingTests` | Unit | `MapStatus()` and `MapRound()` string → enum conversions |
| `GetAllMatchesQueryTests` | Integration | Kickoff ordering, team projection, multi-day span |
| `GetMatchDetailQueryTests` | Integration | Null for missing match; core fields; goals ordered by minute; bookings ordered by minute; empty collections when no events; extended scores (AET/penalties) |
| `GetGroupStandingsQueryTests` | Integration | Ordering by group then position; team detail projection (name/code/crest/points); form string; empty-list safety |
