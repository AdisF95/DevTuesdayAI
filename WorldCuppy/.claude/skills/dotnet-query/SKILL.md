---
name: dotnet-query
description: Scaffolds a read-only vertical slice for WorldCuppy — Query record, Handler, Response DTO, and Minimal API GET endpoint registration. Use this skill whenever the user asks to fetch, list, or retrieve data: "get all matches", "query predictions for a user", "return leaderboard data", "I need an endpoint that returns X". Trigger on GET operations only; for state-changing operations (create, update, delete) use the dotnet-command skill instead.
---

# Create .NET Query

You are scaffolding a read-only vertical slice for the WorldCuppy project. The stack is .NET 10, MediatR 14, EF Core 10 (PostgreSQL), Minimal API.

## Step 1: Confirm what you need

If the user hasn't provided these, ask (one question):

- **Feature name** — PascalCase folder name (e.g., `Matches`, `Leaderboard`)
- **What to fetch** — list vs single item, and any filter/route parameters
- **Return shape** — which fields come back

## Step 2: Create the files

All files go under `WorldCuppy/Features/<FeatureName>/`.

### 1. Response DTO — `<FeatureName>Response.cs`

One per feature folder. Skip if it already exists.

```csharp
namespace WorldCuppy.Features.<FeatureName>;

/// <summary>Response payload for <FeatureName> queries.</summary>
public record <FeatureName>Response(
    Guid Id
    // add remaining properties
);
```

### 2. Query + Handler — one file, two types

**List query:**

```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using WorldCuppy.Infrastructure.Persistence;

namespace WorldCuppy.Features.<FeatureName>;

/// <summary>Query that retrieves all <description>.</summary>
public record Get<Name>Query(/* optional filter params */) : IRequest<List<<FeatureName>Response>>;

/// <summary>Handles <see cref="Get<Name>Query" />.</summary>
public class Get<Name>Handler(WorldCuppyDbContext db)
    : IRequestHandler<Get<Name>Query, List<<FeatureName>Response>>
{
    /// <summary>Executes the query against the database.</summary>
    public Task<List<<FeatureName>Response>> Handle(Get<Name>Query request, CancellationToken cancellationToken) =>
        db.<DbSet>
            .Where(x => /* filter */)
            .OrderBy(x => /* order */)
            .Select(x => new <FeatureName>Response(/* projection */))
            .ToListAsync(cancellationToken);
}
```

**Single nullable result** — use `.FirstOrDefaultAsync()` and return type `<T>?`.

**Critical rules:**
- Query + Handler always live in the **same file**
- Use `.Select()` for projections — EF auto-joins navigation properties, no `.Include()` needed
- Primary constructor DI: `WorldCuppyDbContext db` in the handler class signature
- Never use `IMediator` — always `ISender` in endpoints
- No validator needed for GET queries

### 3. Endpoint registration — `<FeatureName>Endpoints.cs`

If the file already exists, add the new route to the existing `MapEndpoints` method. Otherwise create the file:

```csharp
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace WorldCuppy.Features.<FeatureName>;

/// <summary>Registers all <FeatureName> API routes.</summary>
public static class <FeatureName>Endpoints
{
    /// <summary>Maps <FeatureName> endpoints onto <paramref name="app" />.</summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/<feature-kebab-case>").WithTags("<FeatureName>");

        // GET list
        group.MapGet("/", async (ISender sender) =>
        {
            var result = await sender.Send(new Get<Name>Query());
            return TypedResults.Ok(result);
        })
        .WithName("Get<Name>")
        .WithSummary("<one line description>");

        // GET single — typed return so OpenAPI documents the 404
        group.MapGet("/{id:guid}", async Task<Results<Ok<<FeatureName>Response>, NotFound>> (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new Get<Name>ByIdQuery(id));
            return result is null
                ? TypedResults.NotFound()
                : TypedResults.Ok(result);
        })
        .WithName("Get<Name>ById")
        .WithSummary("<one line description>");
    }
}
```

Only include the route handlers that are actually needed — don't scaffold unused routes.

## Step 3: Write an integration test

Queries whose only logic is an EF Core projection are covered by integration tests. Follow the `integration-test` skill. Key points for queries:

- `PostgreSqlFixture` provides `CreateDbContext()` — use it for seeding and for instantiating the handler
- Instantiate the handler directly: `new Get<Name>Handler(db.CreateDbContext())`
- Seed required entities via a separate `CreateDbContext()` before calling the handler
- Assert on ordering, field projection, and null/empty-list edge cases

```powershell
dotnet test WorldCuppy.Tests/WorldCuppy.Tests.csproj --filter "FullyQualifiedName~Integration" --verbosity minimal
```

## Step 4: Wire into EndpointExtensions.cs

Open `WorldCuppy/Infrastructure/Extensions/EndpointExtensions.cs` and add:

```csharp
using WorldCuppy.Features.<FeatureName>;   // add to usings at top

// inside MapAllEndpoints:
<FeatureName>Endpoints.MapEndpoints(app);
```

Skip if `<FeatureName>Endpoints` is already registered there.

## Step 5: Update the Feature Index

Open `.claude/rules/feature-index.md` and update:
- **Features table** — add or update the row for this feature (handlers, endpoint, page columns)
- **Test Coverage table** — add a row for the new integration test class

## Step 6: Verify

```powershell
dotnet build WorldCuppy/WorldCuppy.csproj
```

0 errors, 0 warnings before declaring done.

## Style rules — apply to every file

- File-scoped namespace (`namespace Foo;` not `namespace Foo { }`)
- Every public class, every public method, and every public constructor gets an XML `<summary>` doc comment
- `TypedResults` throughout (not `Results.Ok(...)` or bare `Ok(...)`)
- No `.Include()` alongside `.Select()`
