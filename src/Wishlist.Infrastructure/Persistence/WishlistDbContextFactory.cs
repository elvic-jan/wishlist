using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

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

        var options = new DbContextOptionsBuilder<WishlistDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new WishlistDbContext(options);
    }
}
