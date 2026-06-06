using Microsoft.Extensions.Options;
using Wishlist.Application.Interfaces;
using Wishlist.Infrastructure.Options;

namespace Wishlist.Infrastructure.Services;

public class AppUrlProvider(IOptions<AppOptions> options) : IAppUrlProvider
{
    public string PublicBaseUrl => options.Value.PublicBaseUrl;
}
