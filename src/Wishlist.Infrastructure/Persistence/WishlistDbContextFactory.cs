using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;

namespace Wishlist.Infrastructure.Persistence;

/// <summary>
/// Lets EF Core tooling (<c>dotnet ef</c>, migration bundles) build the context
/// without booting the API host, whose startup validation requires runtime-only
/// settings such as SMTP. Bundles pass the real target via <c>--connection</c>.
/// </summary>
public class WishlistDbContextFactory : IDesignTimeDbContextFactory<WishlistDbContext>
{
    private const string LocalDevConnection =
        "Host=localhost;Port=5432;Database=wishlist;Username=wishlist;Password=wishlist";

    public WishlistDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") ?? LocalDevConnection;

        // Identity picks its table layout from IdentityOptions in the app's
        // service provider; give the tooling the same schema version as runtime.
        var identityServices = new ServiceCollection()
            .Configure<IdentityOptions>(o => o.Stores.SchemaVersion = IdentitySchema.Version)
            .BuildServiceProvider();

        var options = new DbContextOptionsBuilder<WishlistDbContext>()
            .UseNpgsql(connectionString)
            .UseApplicationServiceProvider(identityServices)
            .Options;

        return new WishlistDbContext(options);
    }
}
