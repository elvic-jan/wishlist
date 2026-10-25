using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;

namespace Wishlist.API.Controllers;

public enum PasskeyCeremony
{
    SignIn,
    SignUp,
    Add,
    Recovery,
}

/// <summary>
/// Carries a passkey ceremony's state (its challenge, and for registrations the
/// user it was issued for) from the options request to the completion request.
/// The state is encrypted and signed, expires after 5 minutes, names the
/// ceremony it belongs to, and lives in an HttpOnly cookie so page scripts never
/// see it. Each state carries a one-time ID that is remembered once used, so a
/// copied cookie can't be replayed with a captured passkey response (synced
/// passkeys usually keep their signature counter at 0, so the counter check
/// alone wouldn't catch it). The used-ID list is in memory, which matches the
/// single-replica deployment the rate limiters already rely on.
/// </summary>
public sealed class PasskeyCeremonyCookie(IDataProtectionProvider dataProtection, TimeProvider time)
{
    public const string Name = "passkeyCeremony";
    private const string CookiePath = "/api/auth";
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);

    private readonly ITimeLimitedDataProtector _protector = dataProtection
        .CreateProtector("GiftCircles.PasskeyCeremony.v1")
        .ToTimeLimitedDataProtector();

    private readonly ConcurrentDictionary<string, DateTimeOffset> _used = new();

    public void Issue(HttpResponse response, PasskeyCeremony ceremony, string state)
    {
        var payload = JsonSerializer.Serialize(new Payload(ceremony, state, Guid.NewGuid().ToString("N")));
        response.Cookies.Append(Name, _protector.Protect(payload, Lifetime), new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = CookiePath,
            Expires = DateTimeOffset.UtcNow.Add(Lifetime),
        });
    }

    /// <summary>
    /// Returns the state if the request carries a valid, unexpired cookie for
    /// <paramref name="ceremony"/>; otherwise <c>null</c>. Clears the cookie either way.
    /// </summary>
    public string? Take(HttpRequest request, HttpResponse response, PasskeyCeremony ceremony)
    {
        var protectedValue = request.Cookies[Name];
        response.Cookies.Delete(Name, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = CookiePath,
        });

        if (string.IsNullOrEmpty(protectedValue)) return null;

        Payload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<Payload>(_protector.Unprotect(protectedValue));
        }
        catch (Exception ex) when (ex is CryptographicException or JsonException)
        {
            // Tampered, expired, or protected with a key we no longer have.
            return null;
        }

        if (payload is null || payload.Ceremony != ceremony || string.IsNullOrEmpty(payload.Id))
            return null;

        var now = time.GetUtcNow();
        foreach (var (id, expires) in _used)
        {
            if (expires < now) _used.TryRemove(id, out _);
        }

        // A state is good for one attempt, whatever its outcome.
        return _used.TryAdd(payload.Id, now.Add(Lifetime)) ? payload.State : null;
    }

    private sealed record Payload(PasskeyCeremony Ceremony, string State, string Id);
}
