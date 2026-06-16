# WorldCuppy — Claude Code Guide

## Domain

WorldCuppy is a **2026 FIFA World Cup prediction game**. Users predict knockout match outcomes and score points based on accuracy.

### Key Concepts

- **Tournament** — the 2026 World Cup. 48 teams, new format: 12 groups of 4, top 2 + 8 best third-place teams advance to a Round of 32.
- **Match** — a fixture with two teams, a scheduled kickoff time, and an optional final score once played.
- **Prediction** — a user's predicted scoreline for a specific knockout match, locked before kickoff.
- **User** — a registered player who submits predictions and accumulates points on a leaderboard.
- **Leaderboard** — ranked list of users by total points.

### Prediction Scope

Users predict **knockout stage matches only** (Round of 32 → Round of 16 → Quarter-finals → Semi-finals → Final). No group stage predictions.

### Scoring Rules

| Outcome | Points |
|---|---|
| Exact scoreline correct | 3 pts |
| Correct result (win/draw/loss), wrong score | 1 pt |
| Wrong result | 0 pts |

Points are awarded automatically when a match result is recorded.

### Entity Naming

Use these names consistently throughout the codebase:

`Tournament`, `Team`, `Match`, `Prediction`, `User`, `Leaderboard`, `MatchResult`, `KnockoutRound`

---

## Stack

| Layer | Technology |
|---|---|
| Frontend | Blazor Server (.NET 10, Interactive Server Components) |
| Backend | ASP.NET Core Minimal API (.NET 10) |
| ORM | Entity Framework Core (PostgreSQL) |
| Messaging | MediatR (CQRS — commands, queries, notifications) |
| Architecture | Vertical Slice / Clean Architecture |
| Database | PostgreSQL |
| Validation | FluentValidation (wired into MediatR pipeline) |

## Project Layout

```
WorldCuppy/
├── Components/              # Blazor UI layer
│   ├── Pages/               # Routable page components
│   └── Layout/              # Shared layout components
├── Features/                # Vertical slices (one folder per feature)
│   └── <FeatureName>/
│       ├── <FeatureName>Command.cs        # MediatR command/query + handler
│       ├── <FeatureName>Endpoint.cs       # Minimal API endpoint registration
│       ├── <FeatureName>Validator.cs      # FluentValidation validator
│       └── <FeatureName>Response.cs       # Response DTO (if needed)
├── Infrastructure/
│   ├── Persistence/         # DbContext, migrations, entity configs
│   └── Extensions/          # IServiceCollection extension methods
└── Program.cs               # App wiring — keep thin
```

Each **feature folder is self-contained**: the command/query, its handler, the endpoint, the validator, and any DTOs live together. Do not scatter these across horizontal layers.

## Conventions

### Vertical Slice

- One folder per feature under `Features/`.
- A feature owns its own handler; do not share handlers across features.
- Cross-cutting concerns (auth, validation, logging) go through MediatR pipeline behaviours, not inside handlers.

### MediatR

- Commands mutate state; Queries return data. Name them accordingly: `CreateTournamentCommand`, `GetGroupStandingsQuery`.
- Register pipeline behaviours in order: `ValidationBehaviour` → `LoggingBehaviour` → handler.
- Use `INotification` / `INotificationHandler` for domain events, not for request/response flows.

### Minimal API

- Each feature registers its own endpoint via a static `MapEndpoints(IEndpointRouteBuilder)` method on its endpoint class.
- Call all `MapEndpoints` from a single extension method in `Infrastructure/Extensions/EndpointExtensions.cs`.
- Use `TypedResults` over `Results<T>` where the return type is unambiguous.
- Group endpoints with `app.MapGroup("/api/v1/<feature>")`.

### Entity Framework Core

- One `DbContext` (`WorldCuppyDbContext`).
- Entity configurations use `IEntityTypeConfiguration<T>` — never inline `OnModelCreating` for individual entities.
- Always use migrations (`dotnet ef migrations add`). Never use `EnsureCreated` outside of tests.
- Repository pattern is **not** used — query directly via `DbContext` inside handlers.

### Blazor

- Pages live in `Components/Pages/`. They call the API via `HttpClient` or via server-side MediatR calls (prefer the latter for server components to avoid a round-trip).
- Keep pages thin: extract logic into services or send MediatR requests directly.
- Use `@rendermode InteractiveServer` only where interactivity is needed; default to static SSR.

## Dev Setup — Visual Studio (F5)

Pressing **Start** in Visual Studio launches the full stack automatically via Docker Compose:

| Service | How it starts |
|---|---|
| App (Blazor + API) | Built into a Linux container, debugger attached |
| PostgreSQL | `postgres:17` container, persisted via named volume |

**Prerequisites:** Docker Desktop running.

VS uses `docker-compose.dcproj` as the startup project. It merges `docker-compose.yml` + `docker-compose.override.yml` — the override sets `ASPNETCORE_ENVIRONMENT=Development` and injects the connection string pointing to the `postgres` service.

**Migrations are applied automatically on startup** — `Program.cs` calls `db.Database.MigrateAsync()` before the app begins serving requests. No manual `dotnet ef database update` needed in dev.

**Connection strings:**

| Context | Host |
|---|---|
| Running via VS / Docker Compose | `Host=postgres` (injected by docker-compose.override.yml) |
| Running locally without Docker | `Host=localhost` (appsettings.Development.json) |

Dev password (non-production only): `Dev@12345!`

## Build & Run

```powershell
# Restore
dotnet restore

# Build
dotnet build

# Run (dev, without Docker)
dotnet run --project WorldCuppy/WorldCuppy.csproj

# Watch (hot reload, without Docker)
dotnet watch --project WorldCuppy/WorldCuppy.csproj
```

Default URLs: `http://localhost:5048` / `https://localhost:7014`

## Database / Migrations

```powershell
# Add a migration
dotnet ef migrations add <MigrationName> --project WorldCuppy/WorldCuppy.csproj

# Apply migrations
dotnet ef database update --project WorldCuppy/WorldCuppy.csproj

# Drop the database (dev only)
dotnet ef database drop --project WorldCuppy/WorldCuppy.csproj
```

Connection string lives in `appsettings.Development.json` under `ConnectionStrings:Default`.

## OpenAPI

.NET 10 has built-in OpenAPI support — **do not add Swashbuckle**. Use `Microsoft.AspNetCore.OpenApi` (already in the SDK).

```csharp
// Program.cs (dev only)
builder.Services.AddOpenApi();
// ...
if (app.Environment.IsDevelopment())
    app.MapOpenApi(); // serves at /openapi/v1.json
```

For a browser UI use **Scalar** (`Scalar.AspNetCore`) instead of Swagger UI:

```csharp
app.MapScalarApiReference(); // serves at /scalar/v1
```

Document endpoints directly on the route builder — no attributes on handlers:

```csharp
app.MapPost("/api/v1/tournaments", handler)
   .WithName("CreateTournament")
   .WithSummary("Creates a new tournament")
   .WithTags("Tournaments")
   .Produces<TournamentResponse>(StatusCodes.Status201Created)
   .ProducesValidationProblem();
```

OpenAPI setup is **dev/staging only** — never expose `/openapi` or `/scalar` in production.

## Tests

```powershell
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

Test projects follow the same vertical slice structure. Integration tests use a real PostgreSQL instance (Testcontainers or a local dev DB).

## What NOT to Do

- **No repository pattern.** Query EF Core directly inside MediatR handlers.
- **No horizontal layers** (Application/, Domain/, Infrastructure/ as separate projects for now). Everything lives in the single `WorldCuppy` project until a second deployable is needed.
- **No `OnModelCreating` soup.** Each entity gets its own `IEntityTypeConfiguration<T>` class.
- **No fat `Program.cs`.** Service registration goes in `IServiceCollection` extension methods under `Infrastructure/Extensions/`.
- **No AutoMapper.** Map manually or write a simple `static ToDomain()` / `static ToResponse()` on the record/class.
- **No `EnsureCreated`.** Always use migrations.
- **No shared handlers between features.** Duplication is preferred over coupling in a vertical-slice design.
- **No `async void`.** Use `async Task` everywhere.
- **No nullable suppressions (`!`) without a comment explaining why it is safe.**
- **Do not add packages without discussing first.** The dependency list is intentional.
