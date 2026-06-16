# Architecture

## Stack

| Layer | Technology |
|---|---|
| Frontend | Blazor Server (.NET 10, Interactive Server Components) |
| UI Components | MudBlazor 9 (Material Design component library) |
| Backend | ASP.NET Core Minimal API (.NET 10) |
| ORM | Entity Framework Core (PostgreSQL) |
| Messaging | MediatR (CQRS — commands, queries, notifications) |
| Architecture | Vertical Slice |
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
│       ├── <FeatureName>Command.cs
│       ├── <FeatureName>Endpoint.cs
│       ├── <FeatureName>Validator.cs
│       └── <FeatureName>Response.cs
├── Infrastructure/
│   ├── Persistence/         # DbContext, migrations, entity configs
│   └── Extensions/          # IServiceCollection extension methods
└── Program.cs               # App wiring — keep thin
```

## Conventions

### Vertical Slice
- One folder per feature under `Features/`. A feature owns its own handler.
- Cross-cutting concerns (auth, validation, logging) go through MediatR pipeline behaviours.

### MediatR
- Commands mutate state; Queries return data: `CreateTournamentCommand`, `GetGroupStandingsQuery`.
- Pipeline behaviour order: `ValidationBehaviour` → `LoggingBehaviour` → handler.
- Use `INotification` / `INotificationHandler` for domain events only.

### Minimal API
- Each feature registers its own endpoint via `static MapEndpoints(IEndpointRouteBuilder)`.
- All features wired up through `Infrastructure/Extensions/EndpointExtensions.cs`.
- Use `TypedResults`. Group with `app.MapGroup("/api/v1/<feature>")`.
- Document on the route builder: `.WithName()`, `.WithSummary()`, `.Produces<T>()`.

### Entity Framework Core
- One `DbContext` (`WorldCuppyDbContext`).
- Entity configs use `IEntityTypeConfiguration<T>` — never inline in `OnModelCreating`.
- Query directly via `DbContext` inside handlers — no repository pattern.

### Coding Style

- Always add an XML `<summary>` doc comment above every public method, constructor, and class.
- Never remove existing comments from code, even when refactoring. If a comment is stale, update it.

### Blazor
- Keep pages thin — send MediatR requests directly rather than round-tripping via HTTP.
- Use `@rendermode InteractiveServer` only where needed; default to static SSR.

### MudBlazor
- Use MudBlazor components for all UI — no raw `<button>`, `<input>`, `<table>`, `<form>` when a Mud equivalent exists.
- Do not introduce other CSS frameworks (Bootstrap, Tailwind, etc.) — MudBlazor handles all styling.
- `MudThemeProvider`, `MudPopoverProvider`, `MudDialogProvider`, and `MudSnackbarProvider` must stay in `MainLayout.razor` — do not add them to individual pages.
- The World Cup theme (FIFA green `#1a5c38` primary, gold `#c9a02a` secondary) is defined in `MainLayout.razor`. Refer to `Color.Primary` / `Color.Secondary` rather than hard-coding hex values in pages.
- Pages use `MudText`, `MudGrid`/`MudItem`, `MudCard`, `MudButton` as the baseline building blocks.
