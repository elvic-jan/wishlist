using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.IdentityModel.Tokens;
using Wishlist.API;
using Wishlist.Application;
using Wishlist.Application.Interfaces;
using Wishlist.Application.UseCases.GiftClaims;
using Wishlist.Application.UseCases.Groups;
using Wishlist.Application.UseCases.LegacyImport;
using Wishlist.API.Controllers;
using Wishlist.API.Middleware;
using Wishlist.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton<InviteCodeGuard>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<PasskeyCeremonyCookie>();
builder.Services.AddSingleton<ISpaTemplate, WebRootSpaTemplate>();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization();

if (!builder.Environment.IsDevelopment())
{
    // Deployed, TLS terminates at the platform ingress and the container sees
    // plain HTTP. Trust the ingress's forwarded scheme and client IP so
    // [RequireHttps] and the per-IP reset limit act on the real connection.
    // The proxy lists are cleared because ingress addresses are not fixed;
    // the container is reachable only through that ingress.
    builder.Services.Configure<ForwardedHeadersOptions>(opts =>
    {
        opts.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        opts.ForwardLimit = 1;
        opts.KnownIPNetworks.Clear();
        opts.KnownProxies.Clear();
    });
}

var app = builder.Build();

// Job mode: the same image runs a job and exits; no web server starts.
// - "--job reservations": the monthly scheduled reservation run.
// - "--job legacy-load --users <xlsx> --items <xlsx>": loads the old wishlist
//   site's export once (see infra/README.md).
var job = app.Configuration["job"];
if (job is not null)
{
    await using var scope = app.Services.CreateAsyncScope();
    switch (job)
    {
        case "reservations":
            await scope.ServiceProvider.GetRequiredService<ProcessReservationsUseCase>().ExecuteAsync();
            break;
        case "legacy-load":
            var usersPath = app.Configuration["users"] ?? throw new InvalidOperationException("Pass --users <path to vl_users.xlsx>.");
            var itemsPath = app.Configuration["items"] ?? throw new InvalidOperationException("Pass --items <path to vl_items.xlsx>.");
            await scope.ServiceProvider.GetRequiredService<LoadLegacyDataUseCase>().ExecuteAsync(usersPath, itemsPath);
            break;
        default:
            throw new InvalidOperationException($"Unknown job \"{job}\". Jobs: \"reservations\", \"legacy-load\".");
    }
    return;
}

// First, so every response (static files, the page, API answers, errors) gets the headers.
app.UseMiddleware<SecurityHeadersMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
else
{
    // No HTTPS redirection here: the ingress already redirects HTTP to HTTPS, and
    // the container only ever sees plain HTTP (health probes included), so the
    // middleware would find no HTTPS port and just log a warning on every start.
    app.UseForwardedHeaders();
}

app.UseMiddleware<ErrorHandlingMiddleware>();
// The SPA's web app manifest must be served as application/manifest+json;
// map it explicitly so static files never refuse the extension.
var contentTypes = new FileExtensionContentTypeProvider();
contentTypes.Mappings[".webmanifest"] = "application/manifest+json";
app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = contentTypes,
    OnPrepareResponse = ctx =>
    {
        if (SpaPage.CacheControlFor(ctx.Context.Request.Path) is { } cacheControl)
            ctx.Context.Response.Headers.CacheControl = cacheControl;
    },
});

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Liveness/readiness probe. Deliberately touches no database so probes neither
// depend on nor keep Postgres busy.
app.MapGet("/healthz", () => Results.Ok()).AllowAnonymous();

// Invite links get a personal link preview ("Jan invited you to Family…").
// Unknown, expired or rate-limited codes get the generic one, so a preview
// never confirms a guessed code. Personal, so never cached.
app.MapGet("/join/{code}", async (
    string code, HttpContext http, ISpaTemplate spa, InviteCodeGuard guard,
    CheckInvitationUseCase checkInvitation, IAppUrlProvider urls, CancellationToken ct) =>
{
    if (spa.Html is not { } html) return Results.NotFound();

    var invitation = await guard.TryAsync(http, () => checkInvitation.ExecuteAsync(code, ct));
    var preview = invitation is null
        ? null
        : new SpaPage.Preview(
            $"{invitation.InviterName} invited you to {invitation.CircleName} on GiftCircles",
            "Join the circle to see each other's wishlists and coordinate gifts.");
    return SpaPage.Html(SpaPage.Render(html, urls.PublicBaseUrl, http.Request.Path, preview), SpaPage.Caching.NoStore);
}).AllowAnonymous();

// Every other page: the SPA, with absolute link-preview image and URL.
app.MapFallback((HttpContext http, ISpaTemplate spa, IAppUrlProvider urls) =>
    spa.Html is { } html
        ? SpaPage.Html(SpaPage.Render(html, urls.PublicBaseUrl, http.Request.Path, null), SpaPage.Caching.Revalidate)
        : Results.NotFound()).AllowAnonymous();

app.Run();

// Exposed for WebApplicationFactory in integration tests.
public partial class Program;
