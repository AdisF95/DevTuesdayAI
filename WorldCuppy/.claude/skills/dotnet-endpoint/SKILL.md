---
name: dotnet-endpoint
description: Scaffolds a complete vertical slice endpoint for the WorldCuppy .NET 10 project — including the query/command + handler, response DTO, optional FluentValidation validator, endpoint registration class, and wiring into EndpointExtensions.cs. Use this skill whenever the user asks to create a new API endpoint, add a route, implement a new feature's backend, scaffold a vertical slice, or says things like "create an endpoint for X", "add a POST to do Y", or "I need an API that returns Z". Trigger even if the user only describes the business need without using the word "endpoint".
---

# Create .NET Endpoint

You are scaffolding a vertical slice endpoint for the WorldCuppy project. The stack is .NET 10, MediatR 14, EF Core 10 (PostgreSQL), Minimal API, and FluentValidation.

## Step 1: Gather what you need

Confirm these before writing code — if the user hasn't provided them, ask (one question):

- **Feature name** — PascalCase folder name (e.g., `Predictions`, `Leaderboard`)
- **Operation** — what it does (get all, get by id, create, update, delete)
- **Parameters** — route params, query string, or request body fields
- **Return shape** — what data comes back

## Step 2: Pick the operation type

| Operation | File name prefix | HTTP verb | Returns |
|---|---|---|---|
| Read (no side effects) | `Get<Name>` | GET | `List<T>` or `T?` |
| Create | `Create<Name>` | POST | Created `T` |
| Update | `Update<Name>` | PUT | Updated `T` or `NoContent` |
| Delete | `Delete<Name>` | DELETE | `NoContent` |

All files go under `WorldCuppy/Features/<FeatureName>/`.

## Step 3: Create the files

### 1. Response DTO — `<FeatureName>Response.cs`

One per feature folder. Skip if it already exists.

```csharp
namespace WorldCuppy.Features.<FeatureName>;

/// <summary>Response payload for <FeatureName> operations.</summary>
public record <FeatureName>Response(
    Guid Id
    // add remaining properties
);
```

### 2. Query or Command + Handler — one file, two types

**Query (GET):**

```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using WorldCuppy.Infrastructure.Persistence;

namespace WorldCuppy.Features.<FeatureName>;

/// <summary>Query that retrieves <description>.</summary>
public record Get<Name>Query(/* params */) : IRequest<List<<FeatureName>Response>>;

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

For a single nullable result, use `.FirstOrDefaultAsync()` and return type `<T>?`.

**Command (POST/PUT/DELETE):**

```csharp
using MediatR;
using WorldCuppy.Domain;
using WorldCuppy.Infrastructure.Persistence;

namespace WorldCuppy.Features.<FeatureName>;

/// <summary>Command that <description>.</summary>
public record Create<Name>Command(/* params */) : IRequest<<FeatureName>Response>;

/// <summary>Handles <see cref="Create<Name>Command" />.</summary>
public class Create<Name>Handler(WorldCuppyDbContext db)
    : IRequestHandler<Create<Name>Command, <FeatureName>Response>
{
    /// <summary>Persists the new <Name> and returns the created resource.</summary>
    public async Task<<FeatureName>Response> Handle(Create<Name>Command request, CancellationToken cancellationToken)
    {
        var entity = new <Entity>
        {
            Id = Guid.NewGuid(),
            // map from request
        };

        db.<DbSet>.Add(entity);
        await db.SaveChangesAsync(cancellationToken);

        return new <FeatureName>Response(/* map entity */);
    }
}
```

**Critical rules:**
- Query + Handler always live in the **same file**
- Use `.Select()` for projections — EF auto-joins navigation properties, no `.Include()` needed
- Primary constructor DI: `WorldCuppyDbContext db` in the handler class signature
- Never use `IMediator` — always `ISender` in endpoints

### 3. Validator — `Create<Name>Validator.cs` (commands with a request body only)

Skip for GET queries and DELETE by id.

```csharp
using FluentValidation;

namespace WorldCuppy.Features.<FeatureName>;

/// <summary>Validates <see cref="Create<Name>Command" /> input.</summary>
public class Create<Name>Validator : AbstractValidator<Create<Name>Command>
{
    /// <summary>Defines the validation rules.</summary>
    public Create<Name>Validator()
    {
        RuleFor(x => x./* Property */).NotEmpty();
        // add rules
    }
}
```

**Validation pipeline:** FluentValidation runs automatically through the MediatR `ValidationBehavior<,>` registered in `ApplicationExtensions.cs`. Verify that `Infrastructure/Behaviours/ValidationBehavior.cs` exists and that `cfg.AddOpenBehavior(typeof(ValidationBehavior<,>))` is called in `AddApplication()`. If either is missing, create them — the validators won't fire otherwise. Do not add manual `ValidationException` try/catch blocks in endpoints; the behavior handles it before the handler runs.

### 4. Endpoint registration — `<FeatureName>Endpoints.cs`

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

        // GET single — typed return so OpenAPI knows the 404
        group.MapGet("/{id:guid}", async Task<Results<Ok<<FeatureName>Response>, NotFound>> (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new Get<Name>ByIdQuery(id));
            return result is null
                ? TypedResults.NotFound()
                : TypedResults.Ok(result);
        })
        .WithName("Get<Name>ById")
        .WithSummary("<one line description>");

        // POST
        group.MapPost("/", async Task<Results<Created<<FeatureName>Response>, ValidationProblem>> (Create<Name>Command command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return TypedResults.Created($"/api/v1/<feature-kebab-case>/{result.Id}", result);
        })
        .WithName("Create<Name>")
        .WithSummary("<one line description>")
        .ProducesValidationProblem();
    }
}
```

Only include the route handlers that are actually needed — don't scaffold unused routes.

## Step 4: Register in EndpointExtensions.cs

Open `WorldCuppy/Infrastructure/Extensions/EndpointExtensions.cs` and add:

```csharp
using WorldCuppy.Features.<FeatureName>;   // add to usings at top

// inside MapAllEndpoints:
<FeatureName>Endpoints.MapEndpoints(app);
```

## Step 5: Verify

Run `dotnet build WorldCuppy/WorldCuppy.csproj` and confirm 0 errors, 0 warnings before declaring done. Fix any issues before finishing.

## Style rules — apply to every file, no exceptions

- File-scoped namespace (`namespace Foo;` not `namespace Foo { }`)
- Every public class, every public method, and every public constructor gets an XML `<summary>` doc comment — this applies to the response DTO record, the query/command record, the handler class and its Handle method, the validator class and its constructor, and the endpoints class and its MapEndpoints method
- `TypedResults` throughout (not `Results.Ok(...)` or bare `Ok(...)`)
- POST returns `TypedResults.Created(location, body)`, not `TypedResults.Ok(...)`
- No `.Include()` alongside `.Select()`
- Validator only for operations that accept a body
