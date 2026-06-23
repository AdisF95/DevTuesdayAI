# WorldCuppy — Claude Code Guide

WorldCuppy is a **2026 FIFA World Cup prediction game**. Users predict knockout match outcomes and score points.

**Scoring:** exact scoreline = 3 pts · correct result = 1 pt · wrong result = 0 pts. Knockout stage only (Round of 32 → Final). Points awarded automatically when a match result is recorded.

**Entity names — use consistently:** `Tournament`, `Team`, `Match`, `Prediction`, `User`, `Leaderboard`, `MatchResult`, `KnockoutRound`

---

## Stack

| Layer | Technology |
|---|---|
| Frontend | Blazor Server (.NET 10, Interactive Server Components) |
| UI | MudBlazor 9 |
| Backend | ASP.NET Core Minimal API (.NET 10) |
| ORM | EF Core 10 (PostgreSQL) |
| Messaging | MediatR 14 (CQRS) |
| Validation | FluentValidation (MediatR pipeline) |

**Project layout:** `Components/` (Blazor pages + layout) · `Domain/` (plain entities) · `Features/` (vertical slices) · `Infrastructure/` (auth, persistence, extensions)

**Living inventory:** `.claude/rules/feature-index.md` is the authoritative list of every feature, entity, endpoint, Blazor page, and test class. Read it at the start of any session to understand what exists. Update it whenever a feature, entity, or page is added or removed.

---

## Key Conventions

**Vertical slice:** one folder per feature under `Features/`. A feature owns its own handler. No shared handlers across features.

**MediatR:** Commands mutate state; Queries return data. Pipeline order: `ValidationBehavior` → `LoggingBehavior` → handler. Use `INotification`/`INotificationHandler` for fan-out domain events only (one state change → multiple independent handlers); use the `domain-event` skill. Always `ISender` in endpoints, never `IMediator`. `IPublisher` for notifications, never `ISender`.

**Minimal API:** each feature registers its own endpoint via `static MapEndpoints(IEndpointRouteBuilder)`, wired through `Infrastructure/Extensions/EndpointExtensions.cs`. Use `TypedResults`. Group with `app.MapGroup("/api/v1/<feature>")`.

**EF Core:** one `DbContext` (`WorldCuppyDbContext`). Entity configs use `IEntityTypeConfiguration<T>`. Query directly inside handlers — no repository pattern. Use `.Select()` for projections; no `.Include()` alongside `.Select()`.

**Coding style:** XML `<summary>` doc comment on every public class, method, and constructor. File-scoped namespaces. Never remove existing comments — update stale ones.

**Authentication — PendingAuthStore bridge pattern** (no ASP.NET Core Identity):
`HttpContext` is unavailable inside a SignalR circuit. Sign-in flow: Blazor page → handler validates credentials → `PendingAuthStore.Store(principal)` returns a one-time `Guid` token → `NavigationManager.NavigateTo("/account/complete-auth/{token}", forceLoad: true)` → minimal API endpoint calls `HttpContext.SignInAsync()` and redirects to `/`.
Key files: `Infrastructure/Auth/PendingAuthStore.cs`, `PasswordHasher.cs` (PBKDF2), `ClaimsPrincipalFactory.cs`, `Features/Users/UsersEndpoints.cs`.
Add `@using Microsoft.AspNetCore.Components.Authorization` to `Components/_Imports.razor` for `AuthorizeView`.

**Blazor:** keep pages thin — send MediatR requests directly. Use `@rendermode InteractiveServer` only where needed; default to static SSR.

**MudBlazor:** no raw `<button>`, `<input>`, `<table>`, `<form>` — use Mud equivalents. No other CSS frameworks. `MudThemeProvider`/`MudPopoverProvider`/`MudDialogProvider`/`MudSnackbarProvider` stay in `MainLayout.razor`. Use `Color.Primary`/`Color.Secondary` (FIFA green `#1a5c38` / gold `#c9a02a`). **MUD0002:** HTML attributes on Mud components must be lowercase — build error.

---

## Testing Requirements

Every new command or query **must** ship with tests.

| What | Test type | Location |
|---|---|---|
| FluentValidation validator | Unit (xUnit + Bogus + FluentValidation.TestHelper) | `WorldCuppy.Tests/Unit/<Feature>/` |
| Pure calculation / mapping | Unit (xUnit + Bogus) | `WorldCuppy.Tests/Unit/<Feature>/` |
| MediatR handler (DB queries) | Integration (Testcontainers) | `WorldCuppy.Tests/Integration/<Feature>/` |

- Use **Bogus** for all test data — never hardcode literals unless the literal is the thing under test
- Start from a valid `ValidCommand()` built with Bogus; mutate one property per test
- Use `FluentValidation.TestHelper` (`ShouldHaveValidationErrorFor`, `ShouldNotHaveAnyValidationErrors`)
- Extract pure in-handler logic to `internal static` classes for unit testability (`InternalsVisibleTo("WorldCuppy.Tests")` is set)
- Integration base: `PostgreSqlFixture` (`IClassFixture<PostgreSqlFixture>`) — spins up `postgres:17`, applies migrations; call `db.CreateDbContext()` per role (separate instances for seeding, the handler under test, and post-act verification — sharing one instance causes tracked-entity conflicts)
- **EF Core version pinning:** when adding Testcontainers packages, also pin `Microsoft.EntityFrameworkCore` and `Npgsql.EntityFrameworkCore.PostgreSQL` to match the main project — otherwise CS1705
- **Bogus username safety:** clamp slices: `name[..Math.Min(n, name.Length)]`

---

## Guardrails

- **Never commit to `main` directly.** Always work on a feature branch. For parallel tasks use git worktrees.
- **Only commit green builds.** The pre-commit hook runs `dotnet build` + `dotnet test` and blocks on failure.
- **Never push or merge without human review.** `git push`, `git merge`, and `git checkout main` are permanently blocked.
- **Static analysis is enforced at build time.** `TreatWarningsAsErrors=true` + `AnalysisMode=Recommended` — a warning is a build error.
- **Human approval required before:** adding NuGet packages, `dotnet ef database drop`, any force-push or destructive git operation.

---

## What NOT to Do

- No repository pattern — query EF Core directly inside handlers
- No horizontal project layers — everything in `WorldCuppy/` until a second deployable is needed
- No `OnModelCreating` soup — each entity gets its own `IEntityTypeConfiguration<T>`
- No fat `Program.cs` — service registration belongs in `Infrastructure/Extensions/`
- No AutoMapper — map manually with `static ToResponse()`
- No `EnsureCreated` — always use migrations
- No shared handlers across features — duplication beats coupling
- No `async void` — use `async Task`
- No `!` nullable suppressions without a comment explaining why it is safe
- Do not add packages without discussing first
- Never remove comments from existing code — update stale ones
- No CSS frameworks other than MudBlazor
- No raw HTML form/interactive elements — use MudBlazor equivalents
