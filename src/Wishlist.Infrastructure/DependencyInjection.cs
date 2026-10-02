using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wishlist.Application.Interfaces;
using Wishlist.Infrastructure.Options;
using Wishlist.Infrastructure.Persistence;
using Wishlist.Infrastructure.Repositories;
using Wishlist.Infrastructure.Services;

namespace Wishlist.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration config,
        IHostEnvironment environment)
    {
        services.AddDbContext<WishlistDbContext>(opts =>
            opts.UseNpgsql(config.GetConnectionString("DefaultConnection")));

        services.AddIdentityCore<ApplicationUser>(opts =>
            {
                opts.Password.RequiredLength = 8;
                opts.Password.RequireNonAlphanumeric = false;
                opts.Password.RequireUppercase = false;
                opts.Password.RequireDigit = false;
                opts.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<WishlistDbContext>()
            .AddDefaultTokenProviders();

        // Keys live in the database so tokens protected before a restart (the
        // deployed container scales to zero) stay valid after it.
        services.AddDataProtection()
            .PersistKeysToDbContext<WishlistDbContext>()
            .SetApplicationName("wishlist");

        // Identity's data-protected reset tokens use this lifespan.
        services.Configure<DataProtectionTokenProviderOptions>(opts =>
        {
            opts.TokenLifespan = TimeSpan.FromMinutes(30);
        });

        // SMTP + App options, bound + validated at startup so a misconfigured
        // prod deploy fails loud instead of silently failing to send mail.
        services.Configure<SmtpOptions>(config.GetSection(SmtpOptions.SectionName));
        services.Configure<AppOptions>(config.GetSection(AppOptions.SectionName));

        ValidateRequiredOptions(config, environment);

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IGroupRepository, GroupRepository>();
        services.AddScoped<IInvitationRepository, InvitationRepository>();
        services.AddScoped<IWishlistRepository, WishlistRepository>();
        services.AddScoped<IGiftClaimRepository, GiftClaimRepository>();
        services.AddScoped<ITokenService, TokenService>();

        services.AddScoped<IAppUrlProvider, AppUrlProvider>();
        services.AddSingleton<IEmailSender, SmtpEmailSender>();
        services.AddSingleton<IForgotPasswordRateLimiter, ForgotPasswordRateLimiter>();

        return services;
    }

    private static void ValidateRequiredOptions(IConfiguration config, IHostEnvironment environment)
    {
        var smtp = new SmtpOptions();
        config.GetSection(SmtpOptions.SectionName).Bind(smtp);

        var app = new AppOptions();
        config.GetSection(AppOptions.SectionName).Bind(app);

        if (string.IsNullOrWhiteSpace(smtp.Host))
        {
            throw new InvalidOperationException(
                "Configuration error: 'Smtp:Host' is required. " +
                "In Development, set it to your local SMTP catcher (e.g. localhost:1025). " +
                "In other environments, set it to your transactional-email provider.");
        }

        if (string.IsNullOrWhiteSpace(app.PublicBaseUrl))
        {
            throw new InvalidOperationException(
                "Configuration error: 'App:PublicBaseUrl' is required. " +
                "Set it to the public origin under which the SPA is served " +
                "(e.g. https://localhost:5173 in development).");
        }

        if (!environment.IsDevelopment()
            && !app.PublicBaseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Configuration error: 'App:PublicBaseUrl' must use HTTPS in '{environment.EnvironmentName}' " +
                "environments. Refusing to start so reset links cannot be sent over an insecure origin.");
        }
    }
}
