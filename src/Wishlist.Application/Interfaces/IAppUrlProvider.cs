namespace Wishlist.Application.Interfaces;

/// <summary>
/// Exposes the public origin the SPA is served from, so application-level
/// link builders (e.g. password-reset emails) do not need to depend on
/// configuration or hosting concerns directly.
/// </summary>
public interface IAppUrlProvider
{
    string PublicBaseUrl { get; }
}
