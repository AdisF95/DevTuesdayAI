# Brief: <Feature Name>

## Context
<!-- 1-3 sentences: why this feature exists and how it fits into the app -->

## Scope
<!-- What should exist after this is done. Be concrete. -->

### Commands / Queries
- `XxxCommand` — mutates Y
- `XxxQuery` — returns Z

### API Endpoints
- `POST /api/v1/<feature>` — accepts `{ ... }`, returns `{ ... }`
- `GET  /api/v1/<feature>/{id}` — returns `{ ... }`

### Blazor UI
- Page at `/route` — describe what the user sees and does
- Components (if any) — describe what each renders

### Domain / DB changes
- New entity: `XxxEntity` with fields ...
- New migration: `Add<Entity>`
- Changes to existing entities (if any)

## Acceptance Criteria
<!-- Each item must be verifiable by the agent running dotnet test or observing build output -->
- [ ] `POST /api/v1/...` returns 201 with the created resource
- [ ] Duplicate X returns 409
- [ ] Validator rejects Y (unit test)
- [ ] Handler integration test covers happy path + edge cases
- [ ] Blazor page renders without errors

## Test requirements

| Test class | Type | What it covers |
|---|---|---|
| `XxxValidatorTests` | Unit | ... |
| `XxxCommandTests` | Integration | Happy path; error cases |
| `XxxQueryTests` | Integration | ... |

## Out of Scope
<!-- Anything explicitly excluded to avoid scope creep -->
- No real-time updates
- No admin-only access controls (yet)

## Notes
<!-- Constraints, gotchas, links to related features -->
- Reuse `XxxQuery` from the `Yyy` feature for the dropdown data
