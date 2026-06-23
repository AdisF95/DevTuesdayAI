# WorldCuppy — Agent & Skill Pipeline

Quick reference for how the Claude agents, skills, and rules fit together. Read this before triggering any agent.

---

## Agent Pipeline

```
                        ┌─────────────┐
     "write a brief     │             │  asks clarifying questions,
      for X"     ──────▶│ brief-writer│  shows draft for approval,
                        │  (Opus 4.8) │  writes .claude/briefs/<name>.md
                        └──────┬──────┘
                               │ "Brief written. Trigger backend-dev
                               │  and frontend-dev in parallel."
                               │
               ┌───────────────┴────────────────┐
               │  (run in parallel)             │
               ▼                                ▼
    ┌─────────────────────┐         ┌─────────────────────┐
    │     backend-dev     │         │    frontend-dev      │
    │    (Sonnet 4.6)     │         │    (Sonnet 4.6)      │
    │                     │         │                      │
    │ entity · command    │         │ Blazor page          │
    │ query · validator   │         │ components           │
    │ endpoint · migration│         │ MudBlazor layout     │
    │ handler tests       │         │ auth wiring          │
    └──────────┬──────────┘         └──────────┬───────────┘
               │                               │
               ▼                               │
    ┌─────────────────────┐                    │
    │    test-auditor     │                    │
    │    (Sonnet 4.6)     │                    │
    │                     │                    │
    │ reads feature-index │                    │
    │ scans test project  │                    │
    │ scaffolds gaps      │                    │
    └──────────┬──────────┘                    │
               │                               │
               └──────────────┬────────────────┘
                              ▼
                ┌─────────────────────────┐
                │    convention-checker   │
                │     (Haiku 4.5)         │
                │                         │
                │ 15-point diff audit     │
                │ doc comments · namespaces│
                │ MudBlazor · ISender     │
                │ feature-index accuracy  │
                └─────────────────────────┘
                              │
                    (if new entity/handler/page added)
                              ▼
                ┌─────────────────────────┐
                │  maintain-instructions  │
                │       (skill)           │
                │                         │
                │ syncs feature-index     │
                │ checks CLAUDE.md drift  │
                └─────────────────────────┘
```

---

## Trigger phrases

| Agent | Trigger |
|---|---|
| `brief-writer` | "write a brief for X", "I want to build Y" |
| `backend-dev` | "implement the backend for `.claude/briefs/<name>.md`" |
| `frontend-dev` | "implement the frontend for `.claude/briefs/<name>.md`" |
| `test-auditor` | "audit test coverage", "what's missing tests", "check coverage for X" |
| `convention-checker` | "check conventions", "audit the branch", "pre-PR check" |

`maintain-instructions` is a **skill** (not an agent) — invoked by `backend-dev` automatically, or manually via `/maintain-instructions`.

---

## Skills dispatch

| Skill | Used by | When |
|---|---|---|
| `ef-entity` | `backend-dev` | New domain entity + EF Core config |
| `dotnet-command` | `backend-dev` | Create / update / delete handler |
| `dotnet-query` | `backend-dev` | Read-only fetch handler |
| `domain-event` | `backend-dev` | One state change → multiple independent handlers |
| `hangfire-job` | `backend-dev` | Recurring or background job |
| `unit-test` | `backend-dev`, `test-auditor` | Validator or pure-logic tests |
| `integration-test` | `backend-dev`, `test-auditor` | Handler tests against real PostgreSQL |
| `blazor-page` | `frontend-dev` | Routable Blazor page |
| `blazor-component` | `frontend-dev` | Reusable non-routable component |
| `auth-flow` | `frontend-dev`, `backend-dev` | New sign-in / registration flow (PendingAuthStore bridge) |
| `maintain-instructions` | `backend-dev` (conditional) | Sync feature-index + check CLAUDE.md drift |

---

## Parallel execution & worktrees

`backend-dev` and `frontend-dev` can run simultaneously because their scopes do not overlap:

- `backend-dev` owns: `Features/`, `Domain/`, `Infrastructure/`, `WorldCuppy.Tests/`
- `frontend-dev` owns: `Components/` (pages + shared components)

**To run them in parallel without file conflicts, use git worktrees:**

```powershell
# From the repo root — create two worktrees on the same branch
git worktree add ../WorldCuppy-backend feature/<name>
git worktree add ../WorldCuppy-frontend feature/<name>

# Trigger backend-dev in one terminal (pointed at ../WorldCuppy-backend)
# Trigger frontend-dev in another terminal (pointed at ../WorldCuppy-frontend)

# After both finish, remove the worktrees
git worktree remove ../WorldCuppy-backend
git worktree remove ../WorldCuppy-frontend
```

Both agents commit to the same branch; changes are merged when the worktrees are removed.

---

## Key rules files

| File | Purpose |
|---|---|
| `CLAUDE.md` | Primary project instructions — conventions, stack, guardrails |
| `.claude/rules/feature-index.md` | Authoritative list of every entity, handler, endpoint, page, and test |
| `.claude/rules/dev-setup.md` | How to build, run, migrate, and test locally |
| `.claude/briefs/_template.md` | Required format for all feature briefs |
| `.claude/PIPELINE.md` | This file |
