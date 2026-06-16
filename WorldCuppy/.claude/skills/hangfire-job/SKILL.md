---
name: hangfire-job
description: Scaffolds a Hangfire background job for WorldCuppy — job class with structured logging, registration in HangfireExtensions.cs, and optional recurring schedule. Use this skill whenever the user asks to add a background job, schedule recurring work, run something on a cron, process data in the background, or says things like "add a job that X", "schedule X every Y", "run X in the background".
---

# Create Hangfire Background Job

You are adding a Hangfire background job to WorldCuppy (.NET 10, Hangfire 1.8, PostgreSQL storage). The job class dispatches work through MediatR so that the actual logic stays in a handler and can be tested independently.

## Step 1: Gather what you need

Confirm before writing code — ask in one question if anything is missing:

- **Job name** — PascalCase, ends in `Job` (e.g. `AwardPointsJob`, `LeaderboardRebuildJob`)
- **What it does** — which MediatR command/query it dispatches, or what it triggers
- **Schedule** — cron expression, or fire-once on startup, or both
- **Recurring job id** — kebab-case string used by Hangfire to identify the job (e.g. `award-points`, `rebuild-leaderboard`)

## Step 2: Create the job class

File: `WorldCuppy/Features/<FeatureName>/<JobName>.cs`

Place the job in the same feature folder as the command it dispatches. If the job spans multiple features, use the primary feature it serves.

```csharp
using MediatR;

namespace WorldCuppy.Features.<FeatureName>;

/// <summary>Hangfire background job that <description of what it does>.</summary>
public partial class <JobName>(ISender sender, ILogger<<JobName>> logger)
{
    /// <summary>Executes the job: <brief description>.</summary>
    public async Task ExecuteAsync()
    {
        Log<JobName>Started(logger);
        var result = await sender.Send(new <CommandName>());
        Log<JobName>Complete(logger, /* key result properties */);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "<JobName> started")]
    private static partial void Log<JobName>Started(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "<JobName> complete — {SomeCount} records processed")]
    private static partial void Log<JobName>Complete(ILogger logger, int someCount);
}
```

**Rules:**
- Class must be `partial` — required for `[LoggerMessage]` source generation
- Use `[LoggerMessage]` for every log call — never `logger.LogInformation(...)` with string interpolation
- Inject `ISender` (not `IMediator`) for MediatR dispatch
- Inject `ILogger<<JobName>>` for structured logging
- Primary constructor DI: `(ISender sender, ILogger<<JobName>> logger)`
- Every public method and the class itself gets an XML `<summary>` doc comment
- Return type is `async Task` — never `async void`

## Step 3: Register the job in HangfireExtensions.cs

Open `WorldCuppy/Infrastructure/Extensions/HangfireExtensions.cs`.

Add the `using` at the top if the job is in a new namespace:
```csharp
using WorldCuppy.Features.<FeatureName>;
```

Then add the registration inside the `UseHangfire` method, after the existing jobs:

### Recurring job (cron schedule)

```csharp
// <Description of what this job does and why this cadence>
RecurringJob.AddOrUpdate<<JobName>>(
    "<recurring-job-id>",
    job => job.ExecuteAsync(),
    "<cron-expression>");
```

Common cron expressions:
| Schedule | Expression |
|---|---|
| Every 15 minutes | `"*/15 * * * *"` |
| Every hour | `"0 * * * *"` |
| Daily at midnight | `"0 0 * * *"` |
| Every 5 minutes | `"*/5 * * * *"` |

### Fire-once on startup (immediate enqueue)

```csharp
// Enqueue immediately on startup so the data is populated without waiting for the first scheduled run
BackgroundJob.Enqueue<<JobName>>(job => job.ExecuteAsync());
```

### Both (recurring + immediate startup run)

```csharp
RecurringJob.AddOrUpdate<<JobName>>(
    "<recurring-job-id>",
    job => job.ExecuteAsync(),
    "<cron-expression>");

BackgroundJob.Enqueue<<JobName>>(job => job.ExecuteAsync());
```

## Step 4: Verify

```powershell
dotnet build WorldCuppy/WorldCuppy.csproj
```

0 errors, 0 warnings. Then start the app and confirm the job appears in the Hangfire dashboard at `/hangfire` (dev only).

## Reference: existing SyncJob pattern

The `SyncJob` in `Features/Sync/SyncJob.cs` is the canonical example:

```csharp
public partial class SyncJob(ISender sender, ILogger<SyncJob> logger)
{
    public async Task ExecuteAsync()
    {
        LogSyncStarted(logger);
        var result = await sender.Send(new SyncCommand());
        LogSyncComplete(logger, result.TeamsUpserted, result.MatchesUpserted);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Football-data sync started")]
    private static partial void LogSyncStarted(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Football-data sync complete — {TeamsUpserted} teams, {MatchesUpserted} matches upserted")]
    private static partial void LogSyncComplete(ILogger logger, int teamsUpserted, int matchesUpserted);
}
```

Its registration in `HangfireExtensions.cs`:
```csharp
// Sync every 15 minutes — fine-grained enough to catch results within a quarter-hour of full time.
RecurringJob.AddOrUpdate<SyncJob>(
    "sync-football-data",
    job => job.ExecuteAsync(),
    "*/15 * * * *");

BackgroundJob.Enqueue<SyncJob>(job => job.ExecuteAsync());
```

Follow this pattern exactly for new jobs.
