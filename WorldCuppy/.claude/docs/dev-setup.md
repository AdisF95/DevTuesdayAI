# Dev Setup

## Visual Studio (F5)

Pressing **Start** launches the full stack via Docker Compose (`docker-compose.dcproj`):

| Service | How it starts |
|---|---|
| App (Blazor + API) | Linux container, debugger attached |
| PostgreSQL | `postgres:17`, persisted via named volume |

**Prerequisite:** Docker Desktop running.

Migrations are applied automatically on startup (`MigrateAsync()` in `Program.cs`).

**Connection strings:**

| Context | Host |
|---|---|
| Via VS / Docker Compose | `Host=postgres` (injected by `docker-compose.override.yml`) |
| Locally without Docker | `Host=localhost` (`appsettings.Development.json`) |

Dev password (non-production): `Dev@12345!`

## Build & Run (without Docker)

```powershell
dotnet restore
dotnet build
dotnet run --project WorldCuppy/WorldCuppy.csproj
dotnet watch --project WorldCuppy/WorldCuppy.csproj  # hot reload
```

URLs: `http://localhost:5048`

## Migrations

```powershell
dotnet ef migrations add <Name> --project WorldCuppy/WorldCuppy.csproj
dotnet ef migrations list  --project WorldCuppy/WorldCuppy.csproj
dotnet ef database update  --project WorldCuppy/WorldCuppy.csproj
```

## Tests

```powershell
dotnet test
dotnet test --collect:"XPlat Code Coverage"
```

Test projects mirror the vertical slice structure. Integration tests use Testcontainers (real PostgreSQL).

## OpenAPI / Scalar

- OpenAPI document: `http://localhost:5048/openapi/v1.json`
- Scalar UI: `http://localhost:5048/scalar/v1`

Dev/staging only — never expose `/openapi` or `/scalar` in production.
Do not add Swashbuckle; use `Microsoft.AspNetCore.OpenApi` + `Scalar.AspNetCore`.

## DBeaver

| Field | Value |
|---|---|
| Host | `localhost` |
| Port | `5432` |
| Database | `worldcuppy` |
| Username | `postgres` |
| Password | `Dev@12345!` |
