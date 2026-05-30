using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wishlist.Application.Interfaces;
using Wishlist.Infrastructure.Persistence;
using Wishlist.Infrastructure.Repositories;
using Wishlist.Infrastructure.Services;

namespace Wishlist.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
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

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IGroupRepository, GroupRepository>();
        services.AddScoped<IInvitationRepository, InvitationRepository>();
        services.AddScoped<IWishlistRepository, WishlistRepository>();
        services.AddScoped<IGiftClaimRepository, GiftClaimRepository>();
        services.AddScoped<ITokenService, TokenService>();

        return services;
    }
}
