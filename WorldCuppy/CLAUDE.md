# WorldCuppy — Claude Code Guide

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

## Build & Run

```powershell
# Restore
dotnet restore

# Build
dotnet build

# Run (dev)
dotnet run --project WorldCuppy/WorldCuppy.csproj

# Watch (hot reload)
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
