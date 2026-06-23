---
name: integration-test
description: Scaffolds integration tests for WorldCuppy using PostgreSqlFixture (real PostgreSQL via Testcontainers) and direct handler instantiation. Use this skill whenever the user asks to add integration tests, test a handler against a real database, or says things like "test the X handler", "add integration tests for Y", "I need DB-level tests for Z". For pure logic tests (validators, calculators) use the unit-test skill instead.
---

# Create Integration Test

You are adding integration tests to WorldCuppy (.NET 10, xUnit, Testcontainers, EF Core 10). Tests use a real PostgreSQL container. Handlers are instantiated directly — there is no WebApplicationFactory or HTTP layer involved.

## What belongs here vs unit tests

| Target | Test type |
|---|---|
| MediatR handlers that query/write EF Core | **Integration test** — needs real PostgreSQL |
| FluentValidation validators | **Unit test** — use the `unit-test` skill |
| Pure calculation classes | **Unit test** — use the `unit-test` skill |

## Step 1: Check whether the test project exists

```powershell
Test-Path WorldCuppy.Tests/WorldCuppy.Tests.csproj
```

If it does **not** exist, run Step 2. If it already exists, skip to Step 3.

## Step 2: Create the test project (first time only)

```powershell
dotnet new xunit -n WorldCuppy.Tests -o WorldCuppy.Tests
dotnet sln WorldCuppy.slnx add WorldCuppy.Tests/WorldCuppy.Tests.csproj
dotnet add WorldCuppy.Tests/WorldCuppy.Tests.csproj reference WorldCuppy/WorldCuppy.csproj
```

Then add required packages — **ask the user for approval before running**:

```powershell
dotnet add WorldCuppy.Tests/WorldCuppy.Tests.csproj package Testcontainers.PostgreSql
dotnet add WorldCuppy.Tests/WorldCuppy.Tests.csproj package Bogus
```

**EF Core version pinning** (critical — Testcontainers.PostgreSql pulls an older EF Core transitively):

```powershell
dotnet add WorldCuppy.Tests/WorldCuppy.Tests.csproj package Microsoft.EntityFrameworkCore --version 10.0.x
dotnet add WorldCuppy.Tests/WorldCuppy.Tests.csproj package Npgsql.EntityFrameworkCore.PostgreSQL --version 10.0.x
```

Use the same version as `WorldCuppy/WorldCuppy.csproj`. Failing to pin causes CS1705 build errors.

Set the target framework to match the main project (`net10.0`, `Nullable=enable`, `ImplicitUsings=enable`).

### Create the shared test fixture (once per project)

File: `WorldCuppy.Tests/Integration/Infrastructure/PostgreSqlFixture.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using WorldCuppy.Infrastructure.Persistence;

namespace WorldCuppy.Tests.Integration.Infrastructure;

/// <summary>
/// Spins up a real PostgreSQL container and applies EF migrations once per test class.
/// Dispose is called automatically by xUnit after the test class finishes.
/// </summary>
public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17").Build();

    /// <summary>Creates a <see cref="WorldCuppyDbContext" /> connected to the running container.</summary>
    public WorldCuppyDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<WorldCuppyDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;

        return new WorldCuppyDbContext(options);
    }

    /// <summary>Starts the container and migrates the schema.</summary>
    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await using var db = CreateDbContext();
        await db.Database.MigrateAsync();
    }

    /// <summary>Stops and removes the container.</summary>
    public async Task DisposeAsync() => await _container.DisposeAsync();
}
```

## Step 3: Create the test class

File: `WorldCuppy.Tests/Integration/<FeatureName>/<ClassName>Tests.cs`

Mirror the vertical slice folder structure from the main project.

```csharp
using Bogus;
using WorldCuppy.Features.<FeatureName>;
using WorldCuppy.Tests.Integration.Infrastructure;

namespace WorldCuppy.Tests.Integration.<FeatureName>;

/// <summary>Integration tests for <see cref="<HandlerName>" /> against a real PostgreSQL database.</summary>
public class <HandlerName>Tests(PostgreSqlFixture db) : IClassFixture<PostgreSqlFixture>
{
    private readonly Faker _faker = new();

    /// <summary>A valid command built with Bogus — all test variations start from this baseline.</summary>
    private <CommandName> ValidCommand() => new(
        Property1: _faker.Random.Guid(),
        Property2: _faker.Random.Int(0, 10));

    /// <summary>Instantiates the handler with a fresh DbContext from the fixture.</summary>
    private <HandlerName> CreateHandler() => new(db.CreateDbContext());

    [Fact]
    public async Task <Action>_<Condition>_<ExpectedOutcome>()
    {
        // Arrange — seed required data directly via DbContext
        await using var ctx = db.CreateDbContext();
        var entity = new <Entity>
        {
            Id = Guid.NewGuid(),
            // set required properties with Bogus
        };
        ctx.<DbSet>.Add(entity);
        await ctx.SaveChangesAsync();

        // Act — call the handler directly
        var handler = CreateHandler();
        var result = await handler.Handle(new <QueryName>(entity.Id), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(entity.Id, result.Id);
    }
}
```

**Key pattern:** handlers are instantiated directly with `new XHandler(db.CreateDbContext())`. Each `CreateDbContext()` call returns a fresh instance — use a separate one for seeding and for the handler so there are no tracked-entity conflicts.

## Step 4: Common test patterns

### Query handler

```csharp
// Seed
await using var ctx = db.CreateDbContext();
ctx.Matches.Add(new Match { Id = Guid.NewGuid(), /* ... */ });
await ctx.SaveChangesAsync();

// Act
var handler = new GetAllMatchesHandler(db.CreateDbContext());
var result = await handler.Handle(new GetAllMatchesQuery(), CancellationToken.None);

// Assert
Assert.Contains(result, r => r.Id == seededMatch.Id);
```

### Command handler — verify persistence

```csharp
var cmd = ValidCommand();
var handler = CreateHandler();

var response = await handler.Handle(cmd, CancellationToken.None);

Assert.NotEqual(Guid.Empty, response.Id);

await using var verify = db.CreateDbContext();
var inDb = await verify.<DbSet>.FindAsync(response.Id);
Assert.NotNull(inDb);
Assert.Equal(cmd.SomeProperty, inDb.SomeProperty);
```

### Uniqueness constraint — expect exception

```csharp
var cmd = ValidCommand();
await CreateHandler().Handle(cmd, CancellationToken.None);

// Same unique key — should throw
await Assert.ThrowsAsync<InvalidOperationException>(
    () => CreateHandler().Handle(cmd with { /* change non-key field */ }, CancellationToken.None));
```

### Query ordering / projection

```csharp
// Seed multiple entities with known sort-deterministic properties
// ...

var result = await handler.Handle(new GetMatchesQuery(), CancellationToken.None);

Assert.Equal(expectedCount, result.Count);
Assert.Equal(firstExpectedId, result[0].Id);
```

## Step 5: Run the tests

```powershell
dotnet test WorldCuppy.Tests/WorldCuppy.Tests.csproj --filter "FullyQualifiedName~Integration" --verbosity minimal
```

Testcontainers pulls the `postgres:17` image on first run (~30 seconds). Subsequent runs use the cached image.

## Step 6: Update the Feature Index

Open `.claude/rules/feature-index.md` and add a row to the **Test Coverage** table for every new test class.

## Style rules

- **Real PostgreSQL** — never `UseInMemoryDatabase`
- **Fresh `CreateDbContext()` per role** — one for seeding, a separate one for the handler, a separate one for verification
- **Bogus for all generated data** — never hardcode magic literals unless the literal is the thing under test
- **One assertion concern per test** — test one behaviour, not a workflow
- **Descriptive names** — `Action_Condition_ExpectedOutcome`
- **No shared mutable state** — each test seeds its own data
- Every test class and method gets an XML `<summary>` doc comment
- File-scoped namespace
