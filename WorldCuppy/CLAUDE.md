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
