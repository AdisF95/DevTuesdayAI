# WorldCuppy — Claude Code Guide

@.claude/docs/domain.md
@.claude/docs/architecture.md
@.claude/docs/dev-setup.md

## Model Routing

When spawning sub-agents via the Agent tool, pick the model tier that matches the task complexity:

| Model | ID | Use for |
|---|---|---|
| Haiku 4.5 | `claude-haiku-4-5` | File lookups, grep/search, single-file reads, boilerplate generation, simple checks |
| Sonnet 4.6 | `claude-sonnet-4-6` | Regular feature work, debugging, code review, multi-file edits, endpoint scaffolding |
| Opus 4.8 | `claude-opus-4-8` | Architecture decisions, complex cross-cutting debugging, security review, multi-agent orchestration |

Default to **Sonnet** when in doubt. Escalate to **Opus** only when the task requires deep reasoning across many files. Use **Haiku** for pure read/search tasks where no judgment is needed.

## Testing Requirements

Every new command or query **must** ship with tests. Use the `unit-test` skill and `integration-test` skill to determine which applies:

| What you're testing | Test type | Location |
|---|---|---|
| FluentValidation validator | Unit test (xUnit + Bogus + FluentValidation.TestHelper) | `WorldCuppy.Tests/Unit/<Feature>/` |
| Pure calculation / mapping method | Unit test (xUnit + Bogus) | `WorldCuppy.Tests/Unit/<Feature>/` |
| MediatR handler (DB queries) | Integration test (Testcontainers) | `WorldCuppy.Tests/Integration/<Feature>/` |

**Unit test rules:**
- Use **Bogus** for all generated test data — never hardcode magic literals unless the literal is the thing under test
- Start from a valid `ValidCommand()` built with Bogus; mutate one property per test
- Use `FluentValidation.TestHelper` (`ShouldHaveValidationErrorFor`, `ShouldNotHaveAnyValidationErrors`) for validator tests
- Pure calculation logic that lives in a handler must be extracted to an `internal static` class so it is unit-testable without the DB

**Key test infrastructure:**
- Test project: `WorldCuppy.Tests/WorldCuppy.Tests.csproj`
- `InternalsVisibleTo("WorldCuppy.Tests")` is set in `WorldCuppy/Properties/AssemblyInfo.cs` — `internal` helpers are automatically accessible
- Integration test base: `PostgreSqlFixture` in `WorldCuppy.Tests/Integration/Infrastructure/` — `IAsyncLifetime`, spins up `postgres:17` via Testcontainers, applies EF migrations. Use via `IClassFixture<PostgreSqlFixture>`; call `db.CreateDbContext()` per test to get a fresh `WorldCuppyDbContext`.
- **EF Core version pinning:** `Testcontainers.PostgreSql` pulls in an older EF Core transitively. When adding any Testcontainers package to the test project, also explicitly pin `Microsoft.EntityFrameworkCore` and `Npgsql.EntityFrameworkCore.PostgreSQL` to match the main project's versions — otherwise CS1705 breaks the build.
- **Bogus username safety:** `_faker.Internet.UserName()` can return strings shorter than your slice target. Always clamp: `name[..Math.Min(n, name.Length)]`.

## Guardrails

- **Never commit to `main` directly.** Always work on a feature branch (`git checkout -b feature/<name>`). For parallel tasks use git worktrees.
- **Only commit green builds.** The pre-commit hook runs `dotnet build` + `dotnet test` and blocks the commit on failure.
- **Never push or merge without human review.** `git push`, `git merge`, and `git checkout main` are permanently blocked by tool permissions.
- **Static analysis is enforced at build time.** `TreatWarningsAsErrors=true` + `AnalysisMode=Recommended` + `.editorconfig` — a warning is a build error.
- **Human approval required before:** adding NuGet packages, `dotnet ef database drop`, any force-push or destructive git operation.

## What NOT to Do

- **No repository pattern.** Query EF Core directly inside MediatR handlers.
- **No horizontal project layers.** Everything lives in `WorldCuppy/` until a second deployable is needed.
- **No `OnModelCreating` soup.** Each entity gets its own `IEntityTypeConfiguration<T>`.
- **No fat `Program.cs`.** Service registration belongs in `Infrastructure/Extensions/`.
- **No AutoMapper.** Map manually with `static ToResponse()` on the record/class.
- **No `EnsureCreated`.** Always use migrations.
- **No shared handlers across features.** Duplication beats coupling in vertical-slice design.
- **No `async void`.** Use `async Task`.
- **No `!` nullable suppressions** without a comment explaining why it is safe.
- **Do not add packages without discussing first.**
- **Never remove comments from existing code.** If a comment is stale, update it — don't delete it.
- **No other CSS frameworks.** MudBlazor handles all styling — never add Bootstrap, Tailwind, or any other CSS library.
- **No raw HTML form/interactive elements.** Use MudBlazor equivalents (`MudButton`, `MudTextField`, `MudSelect`, `MudDataGrid`, etc.) instead of bare `<button>`, `<input>`, `<table>`, `<form>`.
