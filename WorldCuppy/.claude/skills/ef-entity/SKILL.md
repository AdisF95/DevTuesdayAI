---
name: ef-entity
description: Scaffolds a complete EF Core entity for WorldCuppy — domain class, IEntityTypeConfiguration<T>, DbSet in WorldCuppyDbContext, and migration guidance. Use this skill whenever the user asks to add a new database entity, create a new table, model a new domain concept, or says things like "I need a User entity", "add a Leaderboard table", "model X in the database".
---

# Create EF Core Entity

You are adding a new persisted entity to WorldCuppy (.NET 10, EF Core 10, PostgreSQL). This always touches **four things** in a fixed order: domain class → entity config → DbContext → migration.

## Step 1: Gather what you need

Confirm before writing code — ask in one question if anything is missing:

- **Entity name** — PascalCase singular (e.g. `User`, `LeaderboardEntry`, `PointsLedger`)
- **Properties** — names, types, nullability, and any unique constraints or indexes
- **Relationships** — foreign keys to existing entities (Team, Match, Prediction)?
- **Migration name** — short descriptive PascalCase (e.g. `AddUser`, `AddLeaderboardEntry`)

## Step 2: Create the domain class

File: `WorldCuppy/Domain/<EntityName>.cs`

```csharp
namespace WorldCuppy.Domain;

/// <summary><Description of what this entity represents in the domain.></summary>
public class <EntityName>
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }

    // Required scalar properties — use `required` keyword
    public required string PropertyName { get; set; }

    // Optional properties
    public string? OptionalProperty { get; set; }

    // Timestamps — always UTC
    public DateTimeOffset CreatedAtUtc { get; set; }

    // Foreign key scalar + navigation property pair
    public Guid RelatedEntityId { get; set; }
    public required RelatedEntity RelatedEntity { get; set; }
}
```

**Rules:**
- Use `Guid` primary keys named `Id`
- Timestamps use `DateTimeOffset` named `*Utc`
- Foreign key scalar (`RelatedEntityId`) and navigation property (`RelatedEntity`) always come as a pair
- `required` on non-nullable properties that must be set at construction
- No logic in domain classes — they are plain data containers
- Every public property gets an XML `<summary>` doc comment

## Step 3: Create the entity configuration

File: `WorldCuppy/Infrastructure/Persistence/Configuration/<EntityName>Configuration.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorldCuppy.Domain;

namespace WorldCuppy.Infrastructure.Persistence.Configuration;

/// <summary>EF Core mapping configuration for <see cref="<EntityName>" />.</summary>
public class <EntityName>Configuration : IEntityTypeConfiguration<<EntityName>>
{
    /// <summary>Configures table shape, constraints, and relationships for <EntityName>.</summary>
    public void Configure(EntityTypeBuilder<<EntityName>> builder)
    {
        builder.HasKey(e => e.Id);

        // String length constraints — always set a max length on string columns
        builder.Property(e => e.PropertyName).HasMaxLength(200).IsRequired();
        builder.Property(e => e.OptionalProperty).HasMaxLength(500);

        // Unique index example
        builder.HasIndex(e => e.PropertyName).IsUnique();

        // Enum → string conversion (preserves readability in the DB)
        builder.Property(e => e.EnumProperty).HasConversion<string>();

        // Relationship — always set OnDelete behaviour explicitly
        builder.HasOne(e => e.RelatedEntity)
            .WithMany()
            .HasForeignKey(e => e.RelatedEntityId)
            .OnDelete(DeleteBehavior.Cascade);  // or Restrict for lookups

        // Composite unique index example
        builder.HasIndex(e => new { e.UserId, e.MatchId }).IsUnique();
    }
}
```

**Rules:**
- Always set `HasMaxLength` on every string column
- Enums stored as strings via `.HasConversion<string>()` — never as integers
- `OnDelete` behaviour must be explicit: `Cascade` for owned/child records, `Restrict` for lookup references (Team, etc.)
- No inline entity config in `OnModelCreating` — `ApplyConfigurationsFromAssembly` auto-discovers this class
- Every public method and class gets an XML `<summary>` doc comment

## Step 4: Add DbSet to WorldCuppyDbContext

Open `WorldCuppy/Infrastructure/Persistence/WorldCuppyDbContext.cs` and add the `DbSet`:

```csharp
public DbSet<<EntityName>> <PluralEntityName> => Set<<EntityName>>();
```

Add it after the existing DbSet properties, following the same pattern:

```csharp
public class WorldCuppyDbContext(DbContextOptions<WorldCuppyDbContext> options) : DbContext(options)
{
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Prediction> Predictions => Set<Prediction>();
    public DbSet<<EntityName>> <PluralEntityName> => Set<<EntityName>>();  // ← add here

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WorldCuppyDbContext).Assembly);
    }
}
```

## Step 5: Create and apply the migration

```powershell
dotnet ef migrations add <MigrationName> --project WorldCuppy/WorldCuppy.csproj
```

After the migration file is generated, inspect it and confirm:
- The correct columns are created
- Foreign key constraints are correct
- Indexes match what was configured

The migration is applied automatically on the next app startup via `MigrateAsync()` in `Program.cs`. To apply manually:

```powershell
dotnet ef database update --project WorldCuppy/WorldCuppy.csproj
```

## Step 6: Verify the build

```powershell
dotnet build WorldCuppy/WorldCuppy.csproj
```

Must complete with 0 errors, 0 warnings before declaring done. `TreatWarningsAsErrors=true` is enforced — missing XML `<summary>` comments are warnings that become build errors.

## Common patterns from the codebase

**Lookup relationship (Restrict delete):**
```csharp
builder.HasOne(e => e.Team)
    .WithMany()
    .HasForeignKey(e => e.TeamId)
    .OnDelete(DeleteBehavior.Restrict);
```

**External API id (unique index):**
```csharp
builder.HasIndex(e => e.ExternalId).IsUnique();
```

**One-per-user-per-match uniqueness:**
```csharp
builder.HasIndex(e => new { e.UserId, e.MatchId }).IsUnique();
```
