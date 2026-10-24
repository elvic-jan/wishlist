using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Wishlist.Application.Interfaces;
using Wishlist.Domain.Entities;
using Wishlist.Infrastructure.Services;

namespace Wishlist.API.Tests;

/// <summary>
/// Wrong invite codes are budgeted per client IP across register and join; once
/// the budget is spent every code from that IP is refused, even a correct one.
/// </summary>
public class InviteCodeGuessingTests : IClassFixture<InMemoryApi>
{
    private const int Limit = 10;
    private readonly InMemoryApi _api;

    public InviteCodeGuessingTests(InMemoryApi api)
    {
        _api = api;
        if (_api.Store.Users.IsEmpty) _api.Store.AddUser();
    }

    [Fact]
    public async Task Exhausted_budget_blocks_both_endpoints_even_for_a_valid_code()
    {
        const string ip = "192.0.2.10";
        var member = _api.Store.AddUser();
        var bearer = AccessTokenFor(member);
        var valid = _api.Store.AddInvitation();

        // Spend the budget across both endpoints: they share one counter.
        for (var i = 0; i < Limit; i++)
        {
            var guess = i % 2 == 0
                ? await Register(RandomCode(), ip)
                : await Join(RandomCode(), bearer, ip);
            Assert.Equal(HttpStatusCode.NotFound, guess.StatusCode);
        }

        var email = $"{Guid.NewGuid()}@example.com";
        var register = await Register(valid.InviteCode, ip, email);
        var join = await Join(valid.InviteCode, bearer, ip);

        Assert.Equal(HttpStatusCode.TooManyRequests, register.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, join.StatusCode);
        Assert.Contains("Too many invitation attempts", await register.Content.ReadAsStringAsync());
        Assert.DoesNotContain(_api.Store.Users.Values, u => u.Email == email);
        Assert.False(_api.Store.IsMember(valid.GroupId, member.Id));
        Assert.Null(valid.UsedAt);
    }

    [Fact]
    public async Task Invitation_check_shares_the_budget_and_is_blocked_too()
    {
        const string ip = "192.0.2.60";
        var valid = _api.Store.AddInvitation();

        for (var i = 0; i < Limit; i++)
            Assert.Equal(HttpStatusCode.NotFound, (await Check(RandomCode(), ip)).StatusCode);

        Assert.Equal(HttpStatusCode.TooManyRequests, (await Check(valid.InviteCode, ip)).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await Register(valid.InviteCode, ip)).StatusCode);
    }

    [Fact]
    public async Task Expired_codes_do_not_count_toward_the_limit()
    {
        const string ip = "192.0.2.20";
        var expired = _api.Store.AddInvitation(expiresAt: DateTime.UtcNow.AddMinutes(-1));

        for (var i = 0; i < Limit + 5; i++)
            Assert.Equal(HttpStatusCode.Gone, (await Register(expired.InviteCode, ip)).StatusCode);

        var valid = await Register(_api.Store.AddInvitation().InviteCode, ip);
        Assert.Equal(HttpStatusCode.OK, valid.StatusCode);
    }

    [Fact]
    public async Task Malformed_codes_are_404_and_count_toward_the_limit()
    {
        const string ip = "192.0.2.30";

        for (var i = 0; i < Limit; i++)
            Assert.Equal(HttpStatusCode.NotFound, (await Register("not-a-code", ip)).StatusCode);

        Assert.Equal(HttpStatusCode.TooManyRequests, (await Register(RandomCode(), ip)).StatusCode);
    }

    [Fact]
    public async Task A_blocked_ip_does_not_affect_other_ips()
    {
        const string blockedIp = "192.0.2.40";
        for (var i = 0; i < Limit; i++) await Register(RandomCode(), blockedIp);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await Register(RandomCode(), blockedIp)).StatusCode);

        var other = await Register(_api.Store.AddInvitation().InviteCode, "192.0.2.41");

        Assert.Equal(HttpStatusCode.OK, other.StatusCode);
    }

    [Fact]
    public async Task Block_lifts_after_the_window()
    {
        using var limiter = new InviteCodeAttemptLimiter(
            NullLogger<InviteCodeAttemptLimiter>.Instance, failuresPerWindow: 2, window: TimeSpan.FromMilliseconds(300));

        limiter.RecordFailure("192.0.2.50");
        Assert.False(limiter.IsBlocked("192.0.2.50"));
        limiter.RecordFailure("192.0.2.50");
        Assert.True(limiter.IsBlocked("192.0.2.50"));

        await Task.Delay(TimeSpan.FromMilliseconds(700));

        Assert.False(limiter.IsBlocked("192.0.2.50"));
    }

    private Task<HttpResponseMessage> Register(string code, string ip, string? email = null) =>
        InMemoryApi.Post(_api.CreateClient(), "/api/auth/register",
            new { email = email ?? $"{Guid.NewGuid()}@example.com", displayName = "Guess", password = "Passw0rd!", inviteCode = code },
            ip: ip);

    private Task<HttpResponseMessage> Join(string code, string bearer, string ip) =>
        InMemoryApi.Post(_api.CreateClient(), $"/api/invitations/{code}/join", bearer: bearer, ip: ip);

    private Task<HttpResponseMessage> Check(string code, string ip) =>
        InMemoryApi.Send(_api.CreateClient(), HttpMethod.Get, $"/api/invitations/{code}", ip: ip);

    private string AccessTokenFor(User user) =>
        _api.Services.GetRequiredService<ITokenService>().GenerateAccessToken(user);

    private static string RandomCode() => Guid.NewGuid().ToString("N");
}
