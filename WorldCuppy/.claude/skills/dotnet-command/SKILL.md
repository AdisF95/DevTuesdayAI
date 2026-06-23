---
name: dotnet-command
description: Scaffolds a state-changing vertical slice for WorldCuppy — Command record, Handler, Response DTO, FluentValidation validator, and Minimal API POST/PUT/DELETE endpoint registration. Use this skill whenever the user asks to create, update, or delete data: "create a prediction", "submit a match result", "implement a POST to do X", "add a command for Y". Trigger on POST/PUT/DELETE operations only; for read-only data retrieval use the dotnet-query skill instead.
---

# Create .NET Command

You are scaffolding a state-changing vertical slice for the WorldCuppy project. The stack is .NET 10, MediatR 14, EF Core 10 (PostgreSQL), Minimal API, and FluentValidation.

## Step 1: Confirm what you need

If the user hasn't provided these, ask (one question):

- **Feature name** — PascalCase folder name (e.g., `Predictions`, `Scoring`)
- **Operation** — create, update, or delete
- **Request body fields** — what the caller sends in
- **Return shape** — what data comes back (or `NoContent` for deletes)

## Step 2: Create the files

All files go under `WorldCuppy/Features/<FeatureName>/`.

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

### 2. Command + Handler — one file, two types

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
- Command + Handler always live in the **same file**
- Primary constructor DI: `WorldCuppyDbContext db` in the handler class signature
- Never use `IMediator` — always `ISender` in endpoints
- If the handler must also fan out to other features, inject `IPublisher` (not `ISender`) and publish an `INotification`

### 3. Validator — `Create<Name>Validator.cs`

Every command with a request body **must** have a validator.

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

**Validation pipeline:** FluentValidation runs automatically through the MediatR `ValidationBehavior<,>` registered in `ApplicationExtensions.cs`. Verify `Infrastructure/Behaviours/ValidationBehavior.cs` exists and `cfg.AddOpenBehavior(typeof(ValidationBehavior<,>))` is called in `AddApplication()`. Do not add manual `ValidationException` try/catch in endpoints.

### 4. Endpoint registration — `<FeatureName>Endpoints.cs`

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

        // POST — returns 201 Created
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

## Step 3: Write tests

### Unit test for the validator

Create `WorldCuppy.Tests/Unit/<FeatureName>/<ValidatorName>Tests.cs` following the `unit-test` skill:

- One happy-path test: valid Bogus-generated command, `ShouldNotHaveAnyValidationErrors()`
- One failure test per validation rule: mutate one property, `ShouldHaveValidationErrorFor(x => x.Prop)`
- Method names: `Create<Name>Validator_When<Property>Is<Condition>_Should<Outcome>`

```powershell
dotnet test WorldCuppy.Tests/WorldCuppy.Tests.csproj --filter "FullyQualifiedName~Unit" --verbosity minimal
```

If the handler contains pure in-memory calculation logic (no DB calls), extract it to an `internal static` class and add correctness tests. `InternalsVisibleTo("WorldCuppy.Tests")` is already wired in `AssemblyInfo.cs`.

### Integration test for the handler

Follow the `integration-test` skill. Key points for command handlers:
- Instantiate directly: `new Create<Name>Handler(db.CreateDbContext())`
- After `Handle()`, open a **separate** `CreateDbContext()` to verify the entity is in the DB
- Test uniqueness constraints: call the handler twice with the same key, expect `InvalidOperationException`

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
- **Features table** — add or update the row for this feature (command/query, endpoint, page columns)
- **Test Coverage table** — add rows for the new unit test class and integration test class

## Step 6: Verify

```powershell
dotnet build WorldCuppy/WorldCuppy.csproj
```

0 errors, 0 warnings before declaring done.

## Style rules — apply to every file

- File-scoped namespace (`namespace Foo;` not `namespace Foo { }`)
- Every public class, every public method, and every public constructor gets an XML `<summary>` doc comment
- `TypedResults` throughout (not `Results.Ok(...)` or bare `Ok(...)`)
- POST returns `TypedResults.Created(location, body)`, not `TypedResults.Ok(...)`
- `IPublisher` for notifications; `ISender` for commands/queries — never swap them
