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
                               │ user confirms → automatically spawns
                               │ both agents in parallel worktrees
                               │
               ┌───────────────┴────────────────┐
               │  (parallel, isolated worktrees) │
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

`backend-dev` and `frontend-dev` run automatically in parallel after you confirm the brief. `brief-writer` spawns both agents simultaneously using the Agent tool's built-in `isolation: "worktree"` — each agent gets its own isolated git worktree and cannot conflict with the other.

When both finish you will receive:
- A summary of what each agent built
- Two branch names — one per worktree

**Merge them before opening a PR:**

```powershell
git checkout <backend-branch>
git merge <frontend-branch>
# resolve any conflicts (unlikely given non-overlapping scopes)
# then open your PR from <backend-branch>
```

**Scope boundaries (why conflicts are rare):**
- `backend-dev` owns: `Features/`, `Domain/`, `Infrastructure/`, `WorldCuppy.Tests/`
- `frontend-dev` owns: `Components/` (pages + shared components)

---

## Key rules files

| File | Purpose |
|---|---|
| `CLAUDE.md` | Primary project instructions — conventions, stack, guardrails |
| `.claude/rules/feature-index.md` | Authoritative list of every entity, handler, endpoint, page, and test |
| `.claude/rules/dev-setup.md` | How to build, run, migrate, and test locally |
| `.claude/briefs/_template.md` | Required format for all feature briefs |
| `.claude/PIPELINE.md` | This file |
