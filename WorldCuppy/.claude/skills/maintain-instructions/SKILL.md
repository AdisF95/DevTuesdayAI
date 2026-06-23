---
name: maintain-instructions
description: Reviews CLAUDE.md and all skill/rules files for accuracy against the current codebase — syncs the Feature & Entity Index, flags stale or missing entries, and updates outdated skill content. Use this skill after significant feature additions or removals, or whenever you suspect CLAUDE.md or a skill is out of date. Trigger on phrases like "keep instructions up to date", "review CLAUDE.md", "sync the feature index", "check if skills are still accurate", or "maintain docs".
---

# Maintain Instructions & Skills

You are reviewing and updating the project's Claude configuration for accuracy. The goal is to keep CLAUDE.md and all skill/rules files in sync with the actual codebase so future sessions start with a correct picture.

## Step 1: Sync the Feature & Entity Index in `.claude/rules/feature-index.md`

### Features table

1. List actual feature folders: `WorldCuppy/Features/`
2. For each folder, check what handlers/commands/queries exist (files matching `*Command.cs`, `*Query.cs`)
3. Check what endpoints exist (`*Endpoints.cs`) and what routes they register
4. Check `WorldCuppy/Components/Pages/` for corresponding Blazor pages
5. Compare against the Features table — add missing rows, remove deleted ones, update stale cells

### Domain Entities table

1. List `WorldCuppy/Domain/*.cs`
2. For each entity, check its properties and the corresponding `IEntityTypeConfiguration<T>` in `WorldCuppy/Infrastructure/Persistence/Configurations/`
3. Compare against the Domain Entities table — update columns, constraints, or notes that have drifted

### Enums table

1. Check `WorldCuppy/Domain/` for enum files
2. Compare against the Enums section — add missing, remove deleted

### Blazor Pages & Components table

1. List `WorldCuppy/Components/Pages/*.razor`
2. List shared components under `WorldCuppy/Components/`
3. Compare against the table — update render modes, descriptions, shared component list

### Test Coverage table

1. List `WorldCuppy.Tests/Unit/**/*Tests.cs` and `WorldCuppy.Tests/Integration/**/*Tests.cs`
2. Compare — add new test classes, remove deleted ones, update descriptions

## Step 2: Review each skill file

For each skill under `.claude/skills/*/SKILL.md`:

1. **Is it still a repeatable task?** If a skill was added for a one-off feature, flag it for removal.
2. **Are file paths still valid?** Check referenced paths exist (e.g., `Infrastructure/Extensions/EndpointExtensions.cs`, `Infrastructure/Behaviours/ValidationBehavior.cs`).
3. **Are code patterns still correct?** Spot-check one or two real files created with the skill — if the generated pattern has drifted from the codebase, update the template.
4. **Are trigger descriptions accurate?** If the description mentions renamed entities or files, update it.

## Step 3: Review CLAUDE.md for drift

- **Architecture > Project Layout** — does the folder structure still match reality?
- **Architecture > Conventions** — has any convention been superseded in the actual code?
- **Guardrails / What NOT to Do** — are any rules redundant or missing?
- **Testing Requirements** — do the test infrastructure notes still match the test project setup?

## Step 4: Apply all updates

Edit files directly. Keep changes minimal — update the specific cell, row, or sentence that is wrong; don't rewrite sections that are still accurate.

After all edits:

```powershell
dotnet build WorldCuppy/WorldCuppy.csproj
```

A CLAUDE.md or skill edit cannot break the build, but run it anyway to catch any unrelated drift you may have noticed.

## Step 5: Report what changed

Return a brief summary:
- Files updated and what specifically changed
- Anything stale that couldn't be automatically resolved (needs human decision)
- Skills flagged for removal or creation
