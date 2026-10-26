namespace Wishlist.Application.Interfaces;

public interface IUserMessageStateRepository
{
    Task<IReadOnlySet<string>> GetDismissedAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Records the dismissal; does nothing if it was already dismissed.</summary>
    Task DismissAsync(Guid userId, string messageId, DateTime at, CancellationToken ct = default);
}
