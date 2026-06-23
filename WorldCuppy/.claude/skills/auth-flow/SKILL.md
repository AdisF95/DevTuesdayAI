---
name: auth-flow
description: Scaffolds the PendingAuthStore bridge pattern for a new auth-related Blazor page and its handler. Use whenever a new sign-in or registration flow is needed — the handler validates credentials, stores a ClaimsPrincipal in PendingAuthStore, and returns a one-time token; the Blazor page navigates to the shared complete-auth endpoint. Trigger on phrases like "add a login flow", "scaffold a new auth page", "new sign-in method".
---

# Auth Flow — PendingAuthStore Bridge Pattern

## Why this pattern exists

Blazor Server runs over a SignalR circuit. `HttpContext` is not available inside the circuit, which means `HttpContext.SignInAsync()` cannot be called from a Blazor page or a MediatR handler. The bridge pattern solves this:

1. The handler validates credentials and stores a `ClaimsPrincipal` in `PendingAuthStore` (a singleton).
2. The handler returns a one-time `Guid` token (valid 5 minutes).
3. The Blazor page forces a full HTTP navigation to `/account/complete-auth/{token}`.
4. That minimal API endpoint runs in a normal HTTP context, calls `HttpContext.SignInAsync()`, and redirects to `/`.

The `/account/complete-auth/{token}` endpoint is **already registered** in `Features/Users/UsersEndpoints.cs` and is shared by all auth flows. **Do not add a new endpoint** — just produce the right token from your handler.

---

## What to scaffold

### 1 — The MediatR handler

The handler must:
- Accept a query or command with the user's credentials as input.
- Look up the user in `WorldCuppyDbContext`.
- Validate credentials (use `PasswordHasher.Verify` for password checks).
- Build a `ClaimsPrincipal` via `ClaimsPrincipalFactory.Create(user)`.
- Store it: `authStore.Store(principal)` → returns `Guid`.
- Return the `Guid` token to the caller.
- Throw `UnauthorizedAccessException` on bad credentials (the Blazor page catches this and shows an error).

**Pattern (from `LoginUserHandler`):**

```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using WorldCuppy.Infrastructure.Auth;
using WorldCuppy.Infrastructure.Persistence;

namespace WorldCuppy.Features.<Feature>;

/// <summary>Query/Command that authenticates via <description> and returns a one-time auth token.</summary>
public record <Name>(<inputs>) : IRequest<Guid>;

/// <summary>Handles <see cref="<Name>" />.</summary>
public class <Name>Handler(WorldCuppyDbContext db, PendingAuthStore authStore)
    : IRequestHandler<<Name>, Guid>
{
    /// <summary>Validates credentials and returns a pending-auth token on success.</summary>
    public async Task<Guid> Handle(<Name> request, CancellationToken cancellationToken)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(/* your lookup predicate */, cancellationToken);

        var valid = user is not null && /* your validation logic */;

        if (!valid)
        {
            throw new UnauthorizedAccessException("<error message>");
        }

        return authStore.Store(ClaimsPrincipalFactory.Create(user!));
    }
}
```

**Key rules:**
- Return type is always `Guid` (the pending-auth token).
- Use `PendingAuthStore` from DI — it is registered as a singleton in `Infrastructure/Extensions/AuthExtensions.cs`.
- Use `ClaimsPrincipalFactory.Create(user)` — never build claims manually.
- Always throw `UnauthorizedAccessException` on failure — the Blazor page relies on this specific exception type to show an inline error rather than an unhandled error page.
- For password flows: always run `PasswordHasher.Verify` even when the user lookup returns null (constant-time path, avoids username enumeration).

---

### 2 — The Blazor page

The page must:
- Collect input from the user via MudBlazor form fields.
- Send the query/command via `ISender`.
- On success: call `Nav.NavigateTo($"/account/complete-auth/{token}", forceLoad: true)`.
- On `UnauthorizedAccessException`: show an inline error message (`MudAlert`).
- On any other exception: show a generic "something went wrong" error.
- Track a `_loading` bool to disable inputs and show a spinner while the request is in flight.

**Pattern (from `Login.razor`):**

```razor
@page "/<route>"
@rendermode InteractiveServer
@using MediatR
@using WorldCuppy.Features.<Feature>
@inject ISender Sender
@inject NavigationManager Nav

<PageTitle><Title> — WorldCuppy</PageTitle>

<MudContainer MaxWidth="MaxWidth.Small" Class="mt-8">
    <MudCard Elevation="3">
        <MudCardHeader>
            <CardHeaderContent>
                <MudText Typo="Typo.h5"><Heading></MudText>
            </CardHeaderContent>
        </MudCardHeader>

        <MudCardContent>
            @if (_errorMessage is not null)
            {
                <MudAlert Severity="Severity.Error" Class="mb-4">@_errorMessage</MudAlert>
            }

            @* form fields here *@
        </MudCardContent>

        <MudCardActions Class="px-4 pb-4">
            <MudButton Variant="Variant.Filled" Color="Color.Primary" FullWidth="true"
                       OnClick="SubmitAsync" Disabled="_loading">
                @if (_loading)
                {
                    <MudProgressCircular Size="Size.Small" Indeterminate="true" Class="mr-2" />
                }
                <submit label>
            </MudButton>
        </MudCardActions>
    </MudCard>
</MudContainer>

@code {
    // field declarations

    private string? _errorMessage;
    private bool _loading;

    /// <summary>Sends <see cref="<Name>" /> and redirects through the cookie sign-in endpoint on success.</summary>
    private async Task SubmitAsync()
    {
        _errorMessage = null;
        _loading = true;

        try
        {
            var token = await Sender.Send(new <Name>(/* inputs */));
            Nav.NavigateTo($"/account/complete-auth/{token}", forceLoad: true);
        }
        catch (UnauthorizedAccessException ex)
        {
            _errorMessage = ex.Message;
            _loading = false;
        }
        catch (Exception)
        {
            _errorMessage = "Something went wrong. Please try again.";
            _loading = false;
        }
    }
}
```

**Key rules:**
- `forceLoad: true` is mandatory — it breaks out of the SignalR circuit so the HTTP request reaches the minimal API endpoint.
- Never call `HttpContext.SignInAsync()` or anything auth-cookie-related from Blazor or a handler — that only works in the minimal API endpoint.
- Always keep `_loading = false` in catch blocks so the user can retry.
- Use `@rendermode InteractiveServer` — auth pages always need interactivity.
- No `[Authorize]` attribute — unauthenticated users must be able to reach the page.

---

## What NOT to add

- A new `/account/complete-auth` endpoint — it is shared and already wired.
- A new `ClaimsPrincipalFactory` — use the existing one in `Infrastructure/Auth/`.
- Manual claims construction — always go through `ClaimsPrincipalFactory.Create(user)`.
- Cookie configuration — that is handled in `Infrastructure/Extensions/AuthExtensions.cs`.

---

## Checklist before finishing

- [ ] Handler returns `Guid` (not `UserResponse` or anything else)
- [ ] Handler throws `UnauthorizedAccessException` on bad credentials
- [ ] Handler uses `ClaimsPrincipalFactory.Create(user)` and `authStore.Store(...)`
- [ ] Blazor page uses `forceLoad: true` on `NavigateTo`
- [ ] Blazor page catches `UnauthorizedAccessException` separately from `Exception`
- [ ] `_loading` is reset to `false` in all catch blocks
- [ ] No raw HTML form elements — MudBlazor only
- [ ] XML `<summary>` on the handler class, constructor, and `Handle` method
