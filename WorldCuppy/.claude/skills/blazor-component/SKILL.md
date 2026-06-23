---
name: blazor-component
description: Scaffolds a reusable non-routable MudBlazor Blazor component for WorldCuppy — accepts Parameters and EventCallbacks, no @page directive, no ISender injection. Use this skill whenever the user asks to create a reusable component, a card, a section widget, a shared UI piece, or anything that renders inside a page rather than being a page itself. Trigger on phrases like "create a component", "build a reusable widget", "extract this into a component", "I need a <X>Card", or "shared UI for X".
---

# Create Blazor Component

You are scaffolding a reusable MudBlazor Blazor component for WorldCuppy (.NET 10, Blazor Server, MudBlazor 9). This is NOT a routable page — it has no `@page` directive and accepts data through `[Parameter]` properties.

## Step 1: Gather what you need

Confirm before writing code — ask in one question if anything is missing:

- **Component name** — PascalCase (e.g. `PredictionCard`, `LeaderboardRow`, `MatchSummary`)
- **Feature area** — which domain concept it belongs to (`Matches`, `Predictions`, `Leaderboard`)
- **Parameters** — what data does the parent pass in (required vs optional)?
- **Callbacks** — does the parent need to react to user interaction? (e.g. OnVote clicked, OnSelect)
- **CSS class passthrough** — does the caller need to forward a `Class` string for layout spacing?

## Step 2: Place the file

Components live in `WorldCuppy/Components/<FeatureArea>/<ComponentName>.razor`.

Examples from the codebase:
- `Components/Matches/MatchCard.razor`
- `Components/Matches/MatchDaySection.razor`

If the `<FeatureArea>` subfolder does not exist yet, create it.

## Step 3: Create the component

### Display-only component (no callbacks)

```razor
@using WorldCuppy.Features.<FeatureName>

<MudPaper Elevation="2" Class="@($"pa-4 {Class}")">
    <!-- render <EntityName> data here using MudBlazor components -->
    <MudText Typo="Typo.subtitle1">@<EntityName>.PropertyName</MudText>
</MudPaper>

@code {
    /// <summary>The <EntityName> data to render.</summary>
    [Parameter, EditorRequired] public required <FeatureName>Response <EntityName> { get; set; }

    /// <summary>Additional CSS classes forwarded to the outer MudPaper.</summary>
    [Parameter] public string? Class { get; set; }
}
```

### Interactive component (with EventCallback)

```razor
@using WorldCuppy.Features.<FeatureName>

<MudCard Class="@Class">
    <MudCardContent>
        <!-- render data -->
        <MudText Typo="Typo.subtitle1">@<EntityName>.PropertyName</MudText>
    </MudCardContent>
    <MudCardActions>
        <MudButton Variant="Variant.Filled" Color="Color.Primary"
                   OnClick="HandleAction">
            Action Label
        </MudButton>
    </MudCardActions>
</MudCard>

@code {
    /// <summary>The <EntityName> data to render.</summary>
    [Parameter, EditorRequired] public required <FeatureName>Response <EntityName> { get; set; }

    /// <summary>Raised when the user triggers the primary action. Carries the <EntityName> Id.</summary>
    [Parameter] public EventCallback<Guid> OnAction { get; set; }

    /// <summary>Additional CSS classes forwarded to the root element.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>Invokes <see cref="OnAction" /> with the current <EntityName>'s Id.</summary>
    private async Task HandleAction() =>
        await OnAction.InvokeAsync(<EntityName>.Id);
}
```

### Component that renders a collection internally

```razor
@using WorldCuppy.Features.<FeatureName>

<div class="@Class">
    @foreach (var item in Items)
    {
        <div class="mb-3">
            <!-- render each item -->
            <MudText>@item.PropertyName</MudText>
        </div>
    }
    @if (!Items.Any())
    {
        <MudAlert Severity="Severity.Info">No items to display.</MudAlert>
    }
</div>

@code {
    /// <summary>The list of items to render.</summary>
    [Parameter, EditorRequired] public required IReadOnlyList<<FeatureName>Response> Items { get; set; }

    /// <summary>Additional CSS classes forwarded to the wrapper div.</summary>
    [Parameter] public string? Class { get; set; }
}
```

## Step 4: Update the Feature Index

Open `.claude/rules/feature-index.md` and add the component name to the **Shared components** line under Blazor Pages & Components.

## Step 5: Verify usage in the parent page

Show a usage snippet so the caller knows how to consume the component:

```razor
<!-- In the parent .razor page -->
<<ComponentName> <EntityName>="@_item" Class="mb-3" />

<!-- With callback -->
<<ComponentName> <EntityName>="@_item" OnAction="HandleAction" />
```

## Step 6: Verify

Run `dotnet build WorldCuppy/WorldCuppy.csproj` and confirm 0 errors, 0 warnings.

## Style rules — apply to every component, no exceptions

- **No `@page` directive** — components are not routable
- **No `ISender` / MediatR** — components are dumb display units; the parent page fetches data and passes it in
- **No `@rendermode`** — components inherit render mode from the parent; do not declare it on the component
- **`[Parameter, EditorRequired]`** for required data; plain `[Parameter]` for optional (Class, callbacks)
- **`required` keyword** on required parameter properties so the compiler enforces them
- **`EventCallback<T>`** for parent-facing events — never `Action<T>` (EventCallback handles async and state notification correctly)
- **`Class` passthrough string** on any component a caller may need to space/position (`Class="@($"... {Class}")"`)
- Every `[Parameter]` property and every `@code` method gets an XML `<summary>` doc comment
- MudBlazor components only — no raw `<button>`, `<input>`, `<table>`
- Colors via `Color.Primary` / `Color.Secondary` — never hard-coded hex
- `_camelCase` for private fields; PascalCase for parameters
