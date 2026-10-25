using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using Wishlist.Application;
using Wishlist.API.Controllers;
using Wishlist.API.Middleware;
using Wishlist.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton<InviteCodeGuard>();
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
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Liveness/readiness probe. Deliberately touches no database so probes neither
// depend on nor keep Postgres busy.
app.MapGet("/healthz", () => Results.Ok()).AllowAnonymous();

app.MapFallbackToFile("index.html");

app.Run();

// Exposed for WebApplicationFactory in integration tests.
public partial class Program;
