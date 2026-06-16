---
name: integration-test
description: Scaffolds integration tests for WorldCuppy using Testcontainers (real PostgreSQL), xUnit, and WebApplicationFactory. Use this skill whenever the user asks to add tests, write an integration test, test a handler or endpoint, set up the test project, or says things like "test the X feature", "add tests for Y", "I need integration tests".
---

# Create Integration Test

You are adding integration tests to WorldCuppy (.NET 10, xUnit, Testcontainers, EF Core 10). Tests use a real PostgreSQL container — no mocks, no in-memory database.

## Step 1: Check whether the test project exists

```powershell
Test-Path WorldCuppy.Tests/WorldCuppy.Tests.csproj
```

If it does **not** exist, run Step 2 first. If it already exists, skip to Step 3.

## Step 2: Create the test project (first time only)

```powershell
dotnet new xunit -n WorldCuppy.Tests -o WorldCuppy.Tests
dotnet sln WorldCuppy.slnx add WorldCuppy.Tests/WorldCuppy.Tests.csproj
dotnet add WorldCuppy.Tests/WorldCuppy.Tests.csproj reference WorldCuppy/WorldCuppy.csproj
```

Then add required packages — **ask the user for approval before running**:

```powershell
dotnet add WorldCuppy.Tests/WorldCuppy.Tests.csproj package Microsoft.AspNetCore.Mvc.Testing
dotnet add WorldCuppy.Tests/WorldCuppy.Tests.csproj package Testcontainers.PostgreSql
dotnet add WorldCuppy.Tests/WorldCuppy.Tests.csproj package FluentAssertions
```

Set the test project's target framework to match the main project. Open `WorldCuppy.Tests/WorldCuppy.Tests.csproj` and ensure:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
</Project>
```

### Create the shared test fixture

File: `WorldCuppy.Tests/WorldCuppyWebAppFactory.cs`

This is the shared fixture that starts a PostgreSQL container and configures the app for testing. Create it once — all test classes reuse it.

```csharp
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using WorldCuppy.Infrastructure.Persistence;

namespace WorldCuppy.Tests;

/// <summary>
/// Shared xUnit fixture that boots a PostgreSQL Testcontainer and a
/// <see cref="WebApplicationFactory{TEntryPoint}" /> for the full app.
/// </summary>
public class WorldCuppyWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithDatabase("worldcuppy_test")
        .WithUsername("postgres")
        .WithPassword("test")
        .Build();

    /// <summary>Starts the PostgreSQL container before any test in the collection runs.</summary>
    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
    }

    /// <summary>Stops and disposes the container after all tests complete.</summary>
    public new async Task DisposeAsync()
    {
        await _postgres.StopAsync();
        await base.DisposeAsync();
    }

    /// <summary>Overrides the app's database connection to point at the Testcontainer.</summary>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the real DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<WorldCuppyDbContext>));
            if (descriptor is not null)
                services.Remove(descriptor);

            // Register against the test container
            services.AddDbContext<WorldCuppyDbContext>(options =>
                options.UseNpgsql(_postgres.GetConnectionString()));
        });
    }

    /// <summary>Creates a scoped service provider from the test app's DI container.</summary>
    public IServiceScope CreateScope() => Services.CreateScope();
}
```

### Make Program accessible to the test project

Add this to the bottom of `WorldCuppy/Program.cs` (after all middleware registrations):

```csharp
// Makes the implicit top-level Program class accessible to test projects.
public partial class Program { }
```

## Step 3: Create the test class

File: `WorldCuppy.Tests/Features/<FeatureName>/<FeatureName>Tests.cs`

Mirror the vertical slice folder structure from the main project.

```csharp
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using WorldCuppy.Features.<FeatureName>;
using WorldCuppy.Infrastructure.Persistence;

namespace WorldCuppy.Tests.Features.<FeatureName>;

/// <summary>Integration tests for the <FeatureName> feature.</summary>
public class <FeatureName>Tests(WorldCuppyWebAppFactory factory)
    : IClassFixture<WorldCuppyWebAppFactory>
{
    /// <summary>
    /// <Describe what this test verifies in plain English.>
    /// </summary>
    [Fact]
    public async Task <DescriptiveMethodName>_<ExpectedOutcome>()
    {
        // Arrange
        using var scope = factory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WorldCuppyDbContext>();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        // Seed — insert minimum required data directly via DbContext
        var entity = new <Entity>
        {
            Id = Guid.NewGuid(),
            // set required properties
        };
        db.<DbSet>.Add(entity);
        await db.SaveChangesAsync();

        // Act
        var result = await sender.Send(new Get<FeatureName>Query(/* params */));

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].Id.Should().Be(entity.Id);
    }
}
```

## Step 4: Run the tests

```powershell
dotnet test WorldCuppy.Tests/WorldCuppy.Tests.csproj --verbosity minimal
```

Testcontainers pulls the PostgreSQL image on first run (~30 seconds). Subsequent runs are faster.

## Style rules — apply to every test, no exceptions

- **Real PostgreSQL** — no `UseInMemoryDatabase`. Testcontainers provides a real DB; use it
- **One assertion concern per test** — test one behaviour, not an entire workflow
- **Descriptive names** — `<Action>_<Condition>_<ExpectedOutcome>` naming convention
- **Seed via DbContext directly** — not via the API or handlers (keeps arrange fast and clear)
- **No shared mutable state** — each test seeds its own data; don't rely on data from other tests
- **Clean up between tests** — either truncate tables in a per-test fixture or use unique identifiers so tests don't interfere
- Every test class and method gets an XML `<summary>` doc comment
- Use `FluentAssertions` (`.Should().Be(...)`) not raw `Assert.Equal(...)` — messages are clearer on failure
- Always `await` async operations — never `.Result` or `.Wait()`

## Common test patterns

### Test a query handler
```csharp
var result = await sender.Send(new Get<Name>Query());
result.Should().ContainSingle(x => x.Id == seededEntity.Id);
```

### Test a command handler
```csharp
var command = new Create<Name>Command(/* valid input */);
var response = await sender.Send(command);
response.Id.Should().NotBeEmpty();
var inDb = await db.<DbSet>.FindAsync(response.Id);
inDb.Should().NotBeNull();
```

### Test validation rejection
```csharp
var act = () => sender.Send(new Create<Name>Command(/* invalid input */));
await act.Should().ThrowAsync<ValidationException>();
```

### Test a GET endpoint via HTTP
```csharp
var client = factory.CreateClient();
var response = await client.GetAsync("/api/v1/<feature>");
response.StatusCode.Should().Be(HttpStatusCode.OK);
```
