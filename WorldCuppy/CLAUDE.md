# WorldCuppy — Claude Code Guide

@.claude/docs/domain.md
@.claude/docs/architecture.md
@.claude/docs/dev-setup.md

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
