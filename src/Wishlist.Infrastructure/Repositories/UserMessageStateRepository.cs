using Microsoft.EntityFrameworkCore;
using Npgsql;
using Wishlist.Application.Interfaces;
using Wishlist.Domain.Entities;
using Wishlist.Infrastructure.Persistence;

namespace Wishlist.Infrastructure.Repositories;

public class UserMessageStateRepository(WishlistDbContext db) : IUserMessageStateRepository
{
    public async Task<IReadOnlySet<string>> GetDismissedAsync(Guid userId, CancellationToken ct = default) =>
        (await db.UserMessageStates.Where(s => s.UserId == userId).Select(s => s.MessageId).ToListAsync(ct)).ToHashSet();

    public async Task DismissAsync(Guid userId, string messageId, DateTime at, CancellationToken ct = default)
    {
        if (await db.UserMessageStates.AnyAsync(s => s.UserId == userId && s.MessageId == messageId, ct))
            return;

        db.UserMessageStates.Add(new UserMessageState { UserId = userId, MessageId = messageId, DismissedAt = at });
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Dismissed from another device at the same moment: already done.
        }
    }
}
