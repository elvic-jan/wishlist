namespace Wishlist.Application.Interfaces;

/// <summary>
/// Runs several repository writes as one atomic unit: either all of them are
/// persisted or, if the work throws, none are.
/// </summary>
public interface IUnitOfWork
{
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> work, CancellationToken ct = default);
}
