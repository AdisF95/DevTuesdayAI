# Feature & Entity Index

Quick-reference for what exists in the codebase. Update this file whenever a feature, entity, or page is added or removed.

---

## Features

| Feature | Handlers / Commands / Queries | API Endpoints | Blazor Page |
|---|---|---|---|
| **Users** | `RegisterUserCommand`, `LoginUserQuery`, `RegisterUserValidator` | `GET /account/complete-auth/{token}`, `GET /account/logout` | `/register`, `/login` |
| **Predictions** | `CreatePredictionCommand`, `CreatePredictionValidator`, `GetPredictionsByUserQuery` | `POST /api/v1/predictions`, `GET /api/v1/predictions/user/{userId}` | `/predictions` *(nav link exists, page not yet built)* |
| **Matches** | `GetMatchByIdQuery`, `GetMatchesByGameDayQuery`, `GetAllMatchesQuery` | `GET /api/v1/matches/{id}`, `GET /api/v1/matches?gameDay=` | `/matches` — scroll view of all matches grouped by day, auto-scrolls to today, matchday filter chips, click-to-detail dialog |
| **Teams** | `GetTeamsQuery`, `GetTeamByCodeQuery` | `GET /api/v1/teams`, `GET /api/v1/teams/{code}` | — |
| **Leaderboard** | `GetLeaderboardQuery`, `LeaderboardCalculator` (internal static) | `GET /api/v1/leaderboard` | `/leaderboard` *(nav link exists, page not yet built)* |
| **Sync** | `SyncCommand`, `SyncJob` (Hangfire) | `POST /api/v1/sync` (manual trigger) | — |

---

## Domain Entities

| Entity | Key Columns | Constraints | Notes |
|---|---|---|---|
| `User` | `Id (Guid)`, `Username`, `Email`, `PasswordHash`, `CreatedAtUtc` | Unique index on `Username`, unique index on `Email` | PBKDF2 hashed password |
| `Match` | `Id`, `HomeTeamId`, `AwayTeamId`, `KickoffUtc`, `GameDay`, `Round`, `Venue`, `Group`, `HomeScore?`, `AwayScore?`, `Status`, `ExternalId` | Unique index on `ExternalId`; FK to `Team` ×2 (Restrict) | `Round` and `Status` stored as string |
| `Team` | `Id`, `Name`, `Code` (3-char), `CrestUrl?`, `ExternalId` | Unique index on `ExternalId` | Synced from football-data.org |
| `Prediction` | `Id`, `UserId (Guid)`, `MatchId`, `PredictedHomeScore`, `PredictedAwayScore`, `SubmittedAtUtc` | Composite unique index on `(UserId, MatchId)`; FK to `Match` (Cascade) | `UserId` is a bare Guid — no FK to `Users` table yet |

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

**Shared components:** `Matches/MatchCard.razor`, `Matches/MatchDaySection.razor`, `Matches/MatchDetailDialog.razor`

---

## Infrastructure

| Area | Key Files |
|---|---|
| Auth | `Infrastructure/Auth/PendingAuthStore.cs`, `PasswordHasher.cs`, `ClaimsPrincipalFactory.cs` |
| Extensions | `AuthExtensions.cs`, `ApplicationExtensions.cs`, `PersistenceExtensions.cs`, `FootballDataExtensions.cs`, `HangfireExtensions.cs`, `EndpointExtensions.cs` |
| External data | `Infrastructure/FootballData/FootballDataClient.cs` — syncs matches + teams from football-data.org every 15 min via Hangfire |

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
