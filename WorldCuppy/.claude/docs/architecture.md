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
├── Domain/                  # Entity classes (plain C# — no EF attributes)
├── Features/                # Vertical slices (one folder per feature)
│   └── <FeatureName>/
│       ├── <FeatureName>Command.cs
│       ├── <FeatureName>Endpoint.cs
│       ├── <FeatureName>Validator.cs
│       └── <FeatureName>Response.cs
├── Infrastructure/
│   ├── Auth/                # Cookie auth helpers (PendingAuthStore, PasswordHasher, ClaimsPrincipalFactory)
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

### Authentication

Cookie auth — no ASP.NET Core Identity, no extra NuGet packages.

**Stack:** `AddAuthentication().AddCookie()` + `AddCascadingAuthenticationState()` registered in `Infrastructure/Extensions/AuthExtensions.cs`.

**Blazor Server constraint:** `HttpContext` is unavailable inside a SignalR circuit (interactive pages). Sign-in therefore uses the **PendingAuthStore bridge pattern**:
1. The Blazor page calls a MediatR handler which validates credentials, then calls `PendingAuthStore.Store(ClaimsPrincipal)` → returns a one-time `Guid` token (5-min TTL).
2. The page calls `NavigationManager.NavigateTo($"/account/complete-auth/{token}", forceLoad: true)` — a full HTTP round-trip.
3. The minimal API endpoint at `GET /account/complete-auth/{token:guid}` reads from `PendingAuthStore`, calls `HttpContext.SignInAsync()`, and redirects to `/`.

Key files: `Infrastructure/Auth/PendingAuthStore.cs`, `Infrastructure/Auth/PasswordHasher.cs` (PBKDF2), `Infrastructure/Auth/ClaimsPrincipalFactory.cs`, `Features/Users/UsersEndpoints.cs`.

Logout: `GET /account/logout` calls `HttpContext.SignOutAsync()` and redirects home.

**`AuthorizeView` in Blazor pages/layouts:** add `@using Microsoft.AspNetCore.Components.Authorization` to `Components/_Imports.razor` — it is not included by default and the component will appear as an unknown element without it.

### Blazor
- Keep pages thin — send MediatR requests directly rather than round-tripping via HTTP.
- Use `@rendermode InteractiveServer` only where needed; default to static SSR.

### MudBlazor
- Use MudBlazor components for all UI — no raw `<button>`, `<input>`, `<table>`, `<form>` when a Mud equivalent exists.
- Do not introduce other CSS frameworks (Bootstrap, Tailwind, etc.) — MudBlazor handles all styling.
- `MudThemeProvider`, `MudPopoverProvider`, `MudDialogProvider`, and `MudSnackbarProvider` must stay in `MainLayout.razor` — do not add them to individual pages.
- The World Cup theme (FIFA green `#1a5c38` primary, gold `#c9a02a` secondary) is defined in `MainLayout.razor`. Refer to `Color.Primary` / `Color.Secondary` rather than hard-coding hex values in pages.
- Pages use `MudText`, `MudGrid`/`MudItem`, `MudCard`, `MudButton` as the baseline building blocks.
- **MUD0002 analyzer:** HTML attributes on Mud components must be lowercase — `title` not `Title`, `class` not `Class` when used as a plain HTML pass-through. This is a build error (`TreatWarningsAsErrors=true`).
