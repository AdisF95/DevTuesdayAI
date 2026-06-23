---
name: domain-event
description: Scaffolds an INotification domain event and one or more INotificationHandler consumers for WorldCuppy. Use this skill whenever a state change in one feature must trigger independent reactions in other features — e.g. "when a match result is recorded, award points AND rebuild the leaderboard", "notify users when X happens", "fan out to multiple handlers". Do NOT use for a command with a single consumer — use dotnet-command for that.
---

# Create Domain Event

You are wiring a MediatR `INotification` domain event to fan out a state change to multiple independent handlers in WorldCuppy.

## When to use this pattern

Use `INotification` only when **one state change needs to trigger reactions in multiple independent features**. Examples:
- `MatchResultRecordedEvent` → `AwardPointsHandler` + `LeaderboardRebuildHandler`
- `UserRegisteredEvent` → `WelcomeEmailHandler` + `AuditLogHandler`

If there is only one consumer, use a `dotnet-command` instead — a command with one handler is simpler and more explicit than a notification.

## Step 1: Confirm what you need

If the user hasn't provided these, ask (one question):

- **Event name** — what state change occurred, past tense (e.g. `MatchResultRecorded`, `PredictionSubmitted`)
- **Publisher feature** — which feature fires the event
- **Consumer features** — which features need to react, and what each one does
- **Event payload** — the minimal data each consumer needs (IDs + timestamps only; consumers fetch details themselves)

## Step 2: Create the event record

File: `WorldCuppy/Features/<PublisherFeature>/<EventName>Event.cs`

```csharp
using MediatR;

namespace WorldCuppy.Features.<PublisherFeature>;

/// <summary>Published when <describe the state change — past tense>.</summary>
/// <param name="<EntityId>">Id of the affected <entity>.</param>
/// <param name="OccurredAtUtc">UTC timestamp when the event occurred.</param>
public record <EventName>Event(
    Guid <EntityId>,
    // include only the data consumers actually need — keep the payload minimal
    DateTimeOffset OccurredAtUtc
) : INotification;
```

**Rules:**
- Name ends in `Event`, not `Notification` or `Message`
- Payload contains IDs and timestamps only — handlers fetch full data from the DB themselves
- Record (immutable) — never a mutable class

## Step 3: Create a handler per consumer feature

One handler file per consuming feature. Keep handlers in the consumer's own feature folder — not in the publisher's folder.

File: `WorldCuppy/Features/<ConsumerFeature>/<EventName>Handler.cs`

```csharp
using MediatR;
using WorldCuppy.Features.<PublisherFeature>;
using WorldCuppy.Infrastructure.Persistence;

namespace WorldCuppy.Features.<ConsumerFeature>;

/// <summary>
/// Handles <see cref="<EventName>Event" /> by <describe what this specific handler does>.
/// </summary>
public class <EventName>Handler(WorldCuppyDbContext db)
    : INotificationHandler<<EventName>Event>
{
    /// <summary>Reacts to the <EventName> event.</summary>
    public async Task Handle(<EventName>Event notification, CancellationToken cancellationToken)
    {
        // fetch what you need from the DB using notification.<EntityId>
        // do the work
        await db.SaveChangesAsync(cancellationToken);
    }
}
```

Repeat for each consumer feature. Each handler is independent — they do not call each other.

## Step 4: Publish from the originating command handler

In the command handler that triggers the event, inject `IPublisher` alongside any other dependencies:

```csharp
using MediatR;
using WorldCuppy.Infrastructure.Persistence;

namespace WorldCuppy.Features.<PublisherFeature>;

/// <summary>Handles <see cref="<CommandName>" />.</summary>
public class <CommandName>Handler(WorldCuppyDbContext db, IPublisher publisher)
    : IRequestHandler<<CommandName>, <ResponseType>>
{
    /// <summary>Persists the state change, then publishes the domain event.</summary>
    public async Task<<ResponseType>> Handle(<CommandName> request, CancellationToken cancellationToken)
    {
        // ... do the work and persist ...
        await db.SaveChangesAsync(cancellationToken);

        // Publish AFTER the state change is committed
        await publisher.Publish(
            new <EventName>Event(entity.Id, DateTimeOffset.UtcNow),
            cancellationToken);

        return new <ResponseType>(/* ... */);
    }
}
```

**Critical rules:**
- Always publish **after** `SaveChangesAsync` — the state change must be committed before consumers react
- Inject `IPublisher` (not `ISender`) — `ISender` is for commands/queries, `IPublisher` is for notifications
- MediatR calls all `INotificationHandler` implementations in sequence by default. If one throws, subsequent handlers do not run. Design handlers to be idempotent if retries are possible.

## Step 5: Verify

```powershell
dotnet build WorldCuppy/WorldCuppy.csproj
```

0 errors, 0 warnings.

Then run tests to confirm the fan-out behavior works end-to-end (use the `integration-test` skill to write a handler-level test).

## Style rules

- File-scoped namespace
- Every public class, method, and constructor gets an XML `<summary>` doc comment
- Event record name ends in `Event`
- Handler class name: `<EventName>Handler` (same name as event, different class)
- Each handler lives in its own consuming feature folder — never in the publisher's folder
- `IPublisher` for notifications; `ISender` for commands/queries — never swap them
