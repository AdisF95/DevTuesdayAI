---
name: blazor-page
description: Scaffolds a MudBlazor Blazor page for the WorldCuppy project — routable @page component, correct rendermode, ISender injection, MudBlazor layout structure, and optional loading/empty states. Use this skill whenever the user asks to create a new page, add a route to the UI, build a screen or view, or scaffold the frontend for a feature. Trigger even if the user only describes the screen (e.g. "I need a predictions page", "show the leaderboard", "add a match schedule view"). For reusable non-routable components (cards, widgets, sections), use the blazor-component skill instead.
---

# Create Blazor Page

> **Reusable components vs pages:** This skill creates routable pages (`@page "/route"`). If you need a non-routable, parameter-driven component (like a card or a section widget), use the **blazor-component** skill instead.

You are scaffolding a MudBlazor Blazor page for WorldCuppy (.NET 10, Blazor Server, MudBlazor 9).

## Step 1: Gather what you need

Confirm before writing code — ask in one question if anything is missing:

- **Page name** — PascalCase (e.g. `Predictions`, `Leaderboard`, `MatchSchedule`)
- **Route** — kebab-case URL (e.g. `/predictions`, `/leaderboard`, `/matches`)
- **Data shown** — what the page fetches and displays
- **Interactivity** — does the user submit anything, or is it read-only?
- **Render mode** — default is static SSR; use `@rendermode InteractiveServer` only if the page has real-time updates or event callbacks that need SignalR

## Step 2: Choose the render mode

| Scenario | Render mode |
|---|---|
| Read-only data display | Static SSR (no `@rendermode` attribute) |
| Forms, button clicks, real-time updates | `@rendermode InteractiveServer` |

Default to static SSR. Only add `@rendermode InteractiveServer` when the page genuinely needs it.

## Step 3: Create the file

File goes in `WorldCuppy/Components/Pages/<PageName>.razor`.

### Read-only page (static SSR)

```razor
@page "/<route>"
@using WorldCuppy.Features.<FeatureName>
@inject ISender Sender

<PageTitle><PageName> — WorldCuppy</PageTitle>

<MudText Typo="Typo.h4" GutterBottom="true"><Page heading></MudText>

@if (_items is null)
{
    <MudProgressLinear Indeterminate="true" Color="Color.Primary" Class="mb-4" />
}
else if (_items.Count == 0)
{
    <MudAlert Severity="Severity.Info">No <items> found.</MudAlert>
}
else
{
    <!-- main content here -->
}

@code {
    private List<<FeatureName>Response>? _items;

    /// <summary>Fetches <description> on page load.</summary>
    protected override async Task OnInitializedAsync()
    {
        _items = await Sender.Send(new Get<Name>Query());
    }
}
```

### Interactive page (forms / button callbacks)

```razor
@page "/<route>"
@rendermode InteractiveServer
@using WorldCuppy.Features.<FeatureName>
@inject ISender Sender

<PageTitle><PageName> — WorldCuppy</PageTitle>

<MudText Typo="Typo.h4" GutterBottom="true"><Page heading></MudText>

<MudPaper Class="pa-4 mb-4" Elevation="1">
    <!-- form fields -->
    <MudTextField @bind-Value="_model.FieldName" Label="Label" />

    <MudButton Variant="Variant.Filled" Color="Color.Primary"
               OnClick="HandleSubmit" Disabled="_submitting" Class="mt-3">
        @(_submitting ? "Saving…" : "Save")
    </MudButton>
</MudPaper>

@code {
    private readonly <Command>Model _model = new();
    private bool _submitting;

    /// <summary>Handles form submission, sends the command, and resets state.</summary>
    private async Task HandleSubmit()
    {
        _submitting = true;
        try
        {
            await Sender.Send(new Create<Name>Command(/* map from _model */));
            // reset or navigate
        }
        finally
        {
            _submitting = false;
        }
    }
}
```

## Step 4: Register in nav (if it's a top-level page)

Open `Components/Layout/MainLayout.razor` and add a `MudNavLink` in the `MudNavMenu`:

```razor
<MudNavLink Href="/<route>" Icon="@Icons.Material.Filled.<Icon>"><Page Name></MudNavLink>
```

Pick an icon from `Icons.Material.Filled` that fits the page concept.

## Step 5: Verify

Run `dotnet build WorldCuppy/WorldCuppy.csproj` and confirm 0 errors, 0 warnings before declaring done.

## Style rules — apply to every page, no exceptions

- `@page` directive first, then `@rendermode` (if needed), then `@using`, then `@inject`
- `<PageTitle>` always set to `<Page Name> — WorldCuppy`
- Always show a `MudProgressLinear` while data is loading (`_items is null`)
- Always show a `MudAlert` for the empty state (`_items.Count == 0`)
- Use `MudGrid`/`MudItem` for multi-column layouts
- Use `MudCard` for content blocks
- Use `MudDataGrid` for tabular data (not `<table>`)
- Refer to theme colors via `Color.Primary` / `Color.Secondary` — never hard-code hex values
- Every `@code` method gets an XML `<summary>` doc comment
- No `HttpClient` calls — use `ISender` to dispatch MediatR queries/commands directly
- Field names in `@code` use `_camelCase` with leading underscore
