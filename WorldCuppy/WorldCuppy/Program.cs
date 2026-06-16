using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using Scalar.AspNetCore;
using WorldCuppy.Components;
using WorldCuppy.Infrastructure.Extensions;
using WorldCuppy.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();

builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddAuth();
builder.Services.AddFootballData(builder.Configuration);
builder.Services.AddHangfireWithPostgres(builder.Configuration);
builder.Services.AddOpenApi();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider
        .GetRequiredService<WorldCuppyDbContext>()
        .Database.MigrateAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
else
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

// HTTPS is terminated outside the container in production; skip redirection when running in Docker.
if (!app.Environment.IsEnvironment("Development") || !IsRunningInContainer())
{
    app.UseHttpsRedirection();
}

static bool IsRunningInContainer() =>
    Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapAllEndpoints();
app.UseHangfire();

app.Run();
