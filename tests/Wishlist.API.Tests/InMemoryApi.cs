using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Wishlist.Application.Interfaces;
using Wishlist.Domain.Entities;
using Wishlist.Domain.Enums;

namespace Wishlist.API.Tests;

/// <summary>
/// The API in Production mode with users, groups, invitations and tokens held in
/// memory, so auth and invitation flows run end to end without a database.
/// </summary>
public class InMemoryApi : WebApplicationFactory<Program>
{
    public InMemoryStore Store { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        builder.UseSetting("ConnectionStrings:DefaultConnection", "Host=unused;Database=unused");
        builder.UseSetting("Jwt:Key", "test-signing-key-that-is-long-enough-for-hmac-sha256");
        builder.UseSetting("Smtp:Host", "smtp.example.test");
        builder.UseSetting("App:PublicBaseUrl", "https://wishlist.example.test");

        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton(Store);
            services.AddSingleton<IUserRepository, InMemoryUsers>();
            services.AddSingleton<IGroupRepository, InMemoryGroups>();
            services.AddSingleton<IInvitationRepository, InMemoryInvitations>();
            services.AddSingleton<IUnitOfWork, InMemoryUnitOfWork>();
            services.AddSingleton<ITokenService, InMemoryTokens>();
            services.AddSingleton<IPasskeyService, FakePasskeys>();
            // The real key ring lives in the database; keep test keys in memory.
            services.AddSingleton<Microsoft.AspNetCore.DataProtection.IDataProtectionProvider>(
                new Microsoft.AspNetCore.DataProtection.EphemeralDataProtectionProvider());
            services.AddSingleton<IEmailSender, CapturingEmailSender>();
        });
    }

    /// <summary>Sends a request as if it came through the ingress from <paramref name="ip"/>.</summary>
    public static async Task<HttpResponseMessage> Send(
        HttpClient client, HttpMethod method, string path,
        object? body = null, string? cookie = null, string? bearer = null, string ip = "203.0.113.1",
        string? ceremony = null)
    {
        var request = new HttpRequestMessage(method, path);
        if (method != HttpMethod.Get && method != HttpMethod.Delete) request.Content = JsonContent.Create(body ?? new { });
        request.Headers.Add("X-Forwarded-Proto", "https");
        request.Headers.Add("X-Forwarded-For", ip);
        var cookies = new List<string>();
        if (cookie is not null) cookies.Add($"refreshToken={cookie}");
        if (ceremony is not null) cookies.Add($"passkeyCeremony={ceremony}");
        if (cookies.Count > 0) request.Headers.Add("Cookie", string.Join("; ", cookies));
        if (bearer is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
        return await client.SendAsync(request);
    }

    public static Task<HttpResponseMessage> Post(HttpClient client, string path, object? body = null, string? cookie = null,
        string? bearer = null, string ip = "203.0.113.1", string? ceremony = null) =>
        Send(client, HttpMethod.Post, path, body, cookie, bearer, ip, ceremony);

    /// <summary>The value of a cookie set by <paramref name="response"/>, or null.</summary>
    public static string? SetCookieValue(HttpResponseMessage response, string name) =>
        SetCookieHeader(response, name)?.Split(';')[0][(name.Length + 1)..];

    public static string? SetCookieHeader(HttpResponseMessage response, string name) =>
        response.Headers.TryGetValues("Set-Cookie", out var values)
            ? values.LastOrDefault(v => v.StartsWith(name + "=", StringComparison.Ordinal))
            : null;
}

public sealed class InMemoryStore
{
    public Group AddGroup(string name)
    {
        var group = new Group { Id = Guid.NewGuid(), Name = name, CreatedAt = DateTime.UtcNow };
        Groups[group.Id] = group;
        return group;
    }

    public ConcurrentDictionary<Guid, User> Users { get; } = new();
    public ConcurrentDictionary<Guid, Group> Groups { get; } = new();
    public ConcurrentDictionary<(Guid GroupId, Guid UserId), GroupMember> Members { get; } = new();
    public ConcurrentDictionary<string, GroupInvitation> Invitations { get; } = new();
    public ConcurrentDictionary<string, FakePasskey> Passkeys { get; } = new();
    public ConcurrentQueue<EmailMessage> SentEmails { get; } = new();
    public ConcurrentDictionary<string, RefreshToken> RefreshTokens { get; } = new();

    /// <summary>Changes whenever reset/confirmation tokens must stop working (like Identity's security stamp).</summary>
    public ConcurrentDictionary<Guid, int> SecurityStamps { get; } = new();
    /// <summary>Passwords set through CreateAsync; users added any other way accept any password.</summary>
    public ConcurrentDictionary<Guid, string> Passwords { get; } = new();

    public IEnumerable<EmailMessage> EmailsTo(string address) => SentEmails.Where(m => m.To == address);

    public User AddPasskeyUser(string? email = null, bool emailConfirmed = false)
    {
        var user = AddUser(email);
        user.HasPassword = false;
        user.EmailConfirmed = emailConfirmed;
        AddPasskeyFor(user.Id);
        return user;
    }

    public FakePasskey AddPasskeyFor(Guid userId, string? name = null)
    {
        var passkey = new FakePasskey(Guid.NewGuid().ToString("N"), userId) { Name = name };
        Passkeys[passkey.Id] = passkey;
        return passkey;
    }

    /// <summary>Makes the next membership insert fail, to exercise rollback.</summary>
    public bool FailNextAddMember { get; set; }

    public User AddUser(string? email = null)
    {
        var user = new User
        {
            Id = Guid.NewGuid(), Email = email ?? $"{Guid.NewGuid()}@example.com",
            DisplayName = "Existing", CreatedAt = DateTime.UtcNow, HasPassword = true,
        };
        Users[user.Id] = user;
        return user;
    }

    public GroupInvitation AddInvitation(DateTime? expiresAt = null, Guid? groupId = null, Guid? createdBy = null)
    {
        var invitation = new GroupInvitation
        {
            Id = Guid.NewGuid(), GroupId = groupId ?? Guid.NewGuid(), InviteCode = Guid.NewGuid().ToString("N"),
            CreatedByUserId = createdBy ?? Guid.NewGuid(), CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt ?? DateTime.UtcNow.AddDays(7),
        };
        Invitations[invitation.InviteCode] = invitation;
        return invitation;
    }

    public bool IsMember(Guid groupId, Guid userId) => Members.ContainsKey((groupId, userId));
}

internal sealed class InMemoryUsers(InMemoryStore store) : IUserRepository
{
    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(store.Users.GetValueOrDefault(id));

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        Task.FromResult(store.Users.Values.FirstOrDefault(u => u.Email == email));

    public Task<bool> EmailExistsAsync(string email, CancellationToken ct = default) =>
        Task.FromResult(store.Users.Values.Any(u => u.Email == email));

    public Task<bool> AnyAsync(CancellationToken ct = default) => Task.FromResult(!store.Users.IsEmpty);

    public Task<User> CreateAsync(string email, string displayName, string password, CancellationToken ct = default)
    {
        var user = new User
        {
            Id = Guid.NewGuid(), Email = email, DisplayName = displayName, CreatedAt = DateTime.UtcNow, HasPassword = true,
        };
        store.Users[user.Id] = user;
        store.Passwords[user.Id] = password;
        return Task.FromResult(user);
    }

    public Task<User> CreateWithoutPasswordAsync(Guid id, string email, string displayName, CancellationToken ct = default)
    {
        if (store.Users.ContainsKey(id) || store.Users.Values.Any(u => u.Email == email))
            throw new InvalidOperationException("Duplicate user.");
        var user = new User { Id = id, Email = email, DisplayName = displayName, CreatedAt = DateTime.UtcNow };
        store.Users[user.Id] = user;
        return Task.FromResult(user);
    }

    private string Stamp(Guid userId) => store.SecurityStamps.GetOrAdd(userId, 0).ToString();

    public Task<string> GenerateEmailConfirmationTokenAsync(Guid userId, CancellationToken ct = default) =>
        Task.FromResult($"confirm-{userId:N}-{Stamp(userId)}");

    public Task<bool> ConfirmEmailAsync(Guid userId, string token, CancellationToken ct = default)
    {
        if (!store.Users.TryGetValue(userId, out var user) || token != $"confirm-{userId:N}-{Stamp(userId)}")
            return Task.FromResult(false);
        user.EmailConfirmed = true;
        return Task.FromResult(true);
    }

    public Task MarkEmailConfirmedAsync(Guid userId, CancellationToken ct = default)
    {
        store.Users[userId].EmailConfirmed = true;
        return Task.CompletedTask;
    }

    public Task<bool> VerifyPasswordResetTokenAsync(Guid userId, string token, CancellationToken ct = default) =>
        Task.FromResult(token == $"reset-{userId:N}-{Stamp(userId)}");

    public Task InvalidateSecurityTokensAsync(Guid userId, CancellationToken ct = default)
    {
        store.SecurityStamps.AddOrUpdate(userId, 1, (_, s) => s + 1);
        return Task.CompletedTask;
    }

    public Task UpdateDisplayNameAsync(Guid userId, string displayName, CancellationToken ct = default)
    {
        store.Users[userId].DisplayName = displayName;
        return Task.CompletedTask;
    }
    public Task<bool> CheckPasswordAsync(Guid userId, string password, CancellationToken ct = default) =>
        Task.FromResult(!store.Passwords.TryGetValue(userId, out var expected) || expected == password);
    public Task<string> GeneratePasswordResetTokenAsync(Guid userId, CancellationToken ct = default) =>
        Task.FromResult($"reset-{userId:N}-{Stamp(userId)}");

    public async Task<bool> ResetPasswordAsync(Guid userId, string token, string newPassword, CancellationToken ct = default)
    {
        if (!await VerifyPasswordResetTokenAsync(userId, token, ct)) return false;
        store.Users[userId].HasPassword = true;
        store.Passwords[userId] = newPassword;
        await InvalidateSecurityTokensAsync(userId, ct);
        return true;
    }
}

internal sealed class InMemoryGroups(InMemoryStore store) : IGroupRepository
{
    public Task<Group?> GetByIdAsync(Guid groupId, CancellationToken ct = default) =>
        Task.FromResult(store.Groups.GetValueOrDefault(groupId));

    public Task<bool> IsMemberAsync(Guid groupId, Guid userId, CancellationToken ct = default) =>
        Task.FromResult(store.IsMember(groupId, userId));

    public Task AddMemberAsync(Guid groupId, Guid userId, GroupMemberRole role, CancellationToken ct = default)
    {
        if (store.FailNextAddMember)
        {
            store.FailNextAddMember = false;
            throw new InvalidOperationException("Simulated membership insert failure.");
        }

        store.Members[(groupId, userId)] = new GroupMember { GroupId = groupId, UserId = userId, Role = role, JoinedAt = DateTime.UtcNow };
        return Task.CompletedTask;
    }

    public Task<Group> CreateAsync(string name, Guid createdByUserId, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<IReadOnlyList<GroupMember>> GetMembersAsync(Guid groupId, CancellationToken ct = default) => throw new NotSupportedException();
    public Task RemoveMemberAsync(Guid groupId, Guid userId, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<int> GetMemberCountAsync(Guid groupId, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<GroupMemberRole?> GetMemberRoleAsync(Guid groupId, Guid userId, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<bool> SharesGroupAsync(Guid userId1, Guid userId2, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<IReadOnlyList<Group>> GetUserGroupsAsync(Guid userId, CancellationToken ct = default) => throw new NotSupportedException();

    public Task<IReadOnlyList<(Guid UserId, string DisplayName, Guid GroupId, string GroupName)>> GetCircleMatesAsync(Guid userId, CancellationToken ct = default)
    {
        var mine = store.Members.Keys.Where(k => k.UserId == userId).Select(k => k.GroupId).ToHashSet();
        IReadOnlyList<(Guid, string, Guid, string)> rows = store.Members.Keys
            .Where(k => mine.Contains(k.GroupId) && k.UserId != userId)
            .Select(k => (k.UserId, store.Users[k.UserId].DisplayName, k.GroupId, store.Groups[k.GroupId].Name))
            .ToList();
        return Task.FromResult(rows);
    }
}

internal sealed class InMemoryInvitations(InMemoryStore store) : IInvitationRepository
{
    public Task<GroupInvitation> CreateAsync(Guid groupId, Guid createdByUserId, CancellationToken ct = default) =>
        Task.FromResult(store.AddInvitation(DateTime.UtcNow.AddDays(3), groupId, createdByUserId));

    public Task<GroupInvitation?> GetByCodeAsync(string code, CancellationToken ct = default) =>
        Task.FromResult(store.Invitations.GetValueOrDefault(code));

    public Task MarkUsedAsync(Guid invitationId, CancellationToken ct = default)
    {
        var invitation = store.Invitations.Values.FirstOrDefault(i => i.Id == invitationId);
        if (invitation is not null) invitation.UsedAt = DateTime.UtcNow;
        return Task.CompletedTask;
    }
}

/// <summary>Restores users and memberships to their pre-transaction state if the work throws.</summary>
internal sealed class InMemoryUnitOfWork(InMemoryStore store) : IUnitOfWork
{
    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> work, CancellationToken ct = default)
    {
        var users = store.Users.ToArray();
        var members = store.Members.ToArray();
        try
        {
            await work(ct);
        }
        catch
        {
            store.Users.Clear();
            foreach (var (id, user) in users) store.Users[id] = user;
            store.Members.Clear();
            foreach (var (key, member) in members) store.Members[key] = member;
            throw;
        }
    }
}

/// <summary>Real, verifiable JWTs (so [Authorize] endpoints work) with refresh tokens in memory.</summary>
internal sealed class InMemoryTokens(IConfiguration config, InMemoryStore store) : ITokenService
{
    private ConcurrentDictionary<string, RefreshToken> _tokens => store.RefreshTokens;

    public string GenerateAccessToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: [new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString())],
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // Keyed by the token itself; the real service stores only hashes.
    public Task<string> CreateRefreshTokenAsync(Guid userId, CancellationToken ct = default)
    {
        var token = Guid.NewGuid().ToString("N");
        _tokens[token] = new RefreshToken
        {
            Id = Guid.NewGuid(), UserId = userId, TokenHash = token,
            CreatedAt = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow.AddDays(30),
        };
        return Task.FromResult(token);
    }

    public Task<Guid?> ConsumeRefreshTokenAsync(string token, CancellationToken ct = default)
    {
        if (!_tokens.TryGetValue(token, out var t)) return Task.FromResult<Guid?>(null);
        lock (t)
        {
            if (!t.IsActive) return Task.FromResult<Guid?>(null);
            t.RevokedAt = DateTime.UtcNow;
            return Task.FromResult<Guid?>(t.UserId);
        }
    }

    public Task RevokeRefreshTokenAsync(string token, CancellationToken ct = default)
    {
        if (_tokens.TryGetValue(token, out var t)) t.RevokedAt = DateTime.UtcNow;
        return Task.CompletedTask;
    }

    public Task RevokeAllUserRefreshTokensAsync(Guid userId, CancellationToken ct = default)
    {
        foreach (var t in _tokens.Values.Where(t => t.UserId == userId)) t.RevokedAt = DateTime.UtcNow;
        return Task.CompletedTask;
    }

    public Task RevokeOtherUserRefreshTokensAsync(Guid userId, string? keepToken, CancellationToken ct = default)
    {
        foreach (var t in _tokens.Values.Where(t => t.UserId == userId && t.TokenHash != keepToken)) t.RevokedAt = DateTime.UtcNow;
        return Task.CompletedTask;
    }
}

public sealed record FakePasskey(string Id, Guid UserId)
{
    public string? Name { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public int SignIns { get; set; }
}

/// <summary>
/// Stands in for WebAuthn. Options carry a random challenge; the state remembers
/// it (plus the user for registrations); a "credential" is
/// <c>{ "id": …, "challenge": … }</c> and only verifies against the state it was
/// issued with — just like a real assertion is bound to its challenge.
/// </summary>
internal sealed class FakePasskeys(InMemoryStore store) : IPasskeyService
{
    private sealed record State(string Challenge, PasskeyUser? User);
    private sealed record Credential(string Id, string Challenge);

    public Task<PasskeyOptions> BeginRegistrationAsync(PasskeyUser user, CancellationToken ct = default) =>
        Task.FromResult(Issue(user));

    public Task<VerifiedPasskey> CompleteRegistrationAsync(string credentialJson, string state, CancellationToken ct = default)
    {
        var (s, c) = Read(credentialJson, state);
        if (s?.User is null || c is null || c.Challenge != s.Challenge || store.Passkeys.ContainsKey(c.Id))
            throw new Wishlist.Domain.Exceptions.DomainException(Wishlist.Application.UseCases.Auth.PasskeyMessages.NotSaved);

        var id = Base64UrlDecode(c.Id);
        return Task.FromResult(new VerifiedPasskey(s.User, new PasskeyCredential(
            id, [1], DateTimeOffset.UtcNow, 0, ["internal"], true, true, true, [], [])));
    }

    public Task AddAsync(Guid userId, PasskeyCredential credential, string? name, CancellationToken ct = default)
    {
        var id = Base64UrlEncode(credential.CredentialId);
        store.Passkeys[id] = new FakePasskey(id, userId) { Name = name };
        return Task.CompletedTask;
    }

    public Task<PasskeyOptions> BeginSignInAsync(CancellationToken ct = default) => Task.FromResult(Issue(null));

    public Task<Guid> CompleteSignInAsync(string credentialJson, string state, CancellationToken ct = default)
    {
        var (s, c) = Read(credentialJson, state);
        if (s is null || c is null || c.Challenge != s.Challenge || !store.Passkeys.TryGetValue(c.Id, out var passkey))
            throw new Wishlist.Domain.Exceptions.DomainException(Wishlist.Application.UseCases.Auth.PasskeyMessages.SignInFailed);

        passkey.SignIns++;
        return Task.FromResult(passkey.UserId);
    }

    public Task<IReadOnlyList<PasskeySummary>> ListAsync(Guid userId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<PasskeySummary>>(store.Passkeys.Values
            .Where(p => p.UserId == userId).OrderBy(p => p.CreatedAt)
            .Select(p => new PasskeySummary(p.Id, p.Name, p.CreatedAt, true)).ToList());

    public Task<bool> RenameAsync(Guid userId, string passkeyId, string name, CancellationToken ct = default)
    {
        if (!store.Passkeys.TryGetValue(passkeyId, out var p) || p.UserId != userId) return Task.FromResult(false);
        p.Name = name;
        return Task.FromResult(true);
    }

    public Task<bool> RemoveAsync(Guid userId, string passkeyId, CancellationToken ct = default) =>
        Task.FromResult(store.Passkeys.TryGetValue(passkeyId, out var p) && p.UserId == userId
            && store.Passkeys.TryRemove(passkeyId, out _));

    private static PasskeyOptions Issue(PasskeyUser? user)
    {
        var challenge = Guid.NewGuid().ToString("N");
        var options = System.Text.Json.JsonSerializer.Serialize(new { challenge, user });
        var state = System.Text.Json.JsonSerializer.Serialize(new State(challenge, user));
        return new PasskeyOptions(options, state);
    }

    private static (State?, Credential?) Read(string credentialJson, string state)
    {
        var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return (System.Text.Json.JsonSerializer.Deserialize<State>(state, options),
            System.Text.Json.JsonSerializer.Deserialize<Credential>(credentialJson, options));
    }

    // Fake credential IDs are 32 hex chars; treat them as base64url text so the
    // ID round-trips through PasskeyCredential's byte[] unchanged.
    private static byte[] Base64UrlDecode(string id) => System.Buffers.Text.Base64Url.DecodeFromChars(id);
    private static string Base64UrlEncode(byte[] id) => System.Buffers.Text.Base64Url.EncodeToString(id);
}

/// <summary>Keeps every email the app tries to send, instead of sending it.</summary>
internal sealed class CapturingEmailSender(InMemoryStore store) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        store.SentEmails.Enqueue(message);
        return Task.CompletedTask;
    }
}
