using Microsoft.EntityFrameworkCore;
using Wishlist.Application.Interfaces;

namespace Wishlist.Infrastructure.Persistence;

/// <summary>
/// Wraps work in a database transaction on the request's <see cref="WishlistDbContext"/>.
/// Repositories and Identity's <c>UserManager</c> share that scoped context, so
/// their <c>SaveChangesAsync</c> calls all join the transaction.
/// </summary>
public class UnitOfWork(WishlistDbContext db) : IUnitOfWork
{
    public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> work, CancellationToken ct = default)
    {
        // Run through the execution strategy so a retrying strategy replays the
        // whole transaction rather than a fragment of it.
        var strategy = db.Database.CreateExecutionStrategy();
        return strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            await work(ct);
            await transaction.CommitAsync(ct);
        });
    }
}
