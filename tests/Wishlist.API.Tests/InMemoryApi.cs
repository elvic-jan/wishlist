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
        });
    }

    /// <summary>Sends a request as if it came through the ingress from <paramref name="ip"/>.</summary>
    public static async Task<HttpResponseMessage> Send(
        HttpClient client, HttpMethod method, string path,
        object? body = null, string? cookie = null, string? bearer = null, string ip = "203.0.113.1")
    {
        var request = new HttpRequestMessage(method, path);
        if (method != HttpMethod.Get) request.Content = JsonContent.Create(body ?? new { });
        request.Headers.Add("X-Forwarded-Proto", "https");
        request.Headers.Add("X-Forwarded-For", ip);
        if (cookie is not null) request.Headers.Add("Cookie", $"refreshToken={cookie}");
        if (bearer is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
        return await client.SendAsync(request);
    }

    public static Task<HttpResponseMessage> Post(HttpClient client, string path, object? body = null, string? cookie = null,
        string? bearer = null, string ip = "203.0.113.1") =>
        Send(client, HttpMethod.Post, path, body, cookie, bearer, ip);
}

public sealed class InMemoryStore
{
    public ConcurrentDictionary<Guid, User> Users { get; } = new();
    public ConcurrentDictionary<(Guid GroupId, Guid UserId), GroupMember> Members { get; } = new();
    public ConcurrentDictionary<string, GroupInvitation> Invitations { get; } = new();

    /// <summary>Makes the next membership insert fail, to exercise rollback.</summary>
    public bool FailNextAddMember { get; set; }

    public User AddUser(string? email = null)
    {
        var user = new User
        {
            Id = Guid.NewGuid(), Email = email ?? $"{Guid.NewGuid()}@example.com",
            DisplayName = "Existing", CreatedAt = DateTime.UtcNow,
        };
        Users[user.Id] = user;
        return user;
    }

    public GroupInvitation AddInvitation(DateTime? expiresAt = null)
    {
        var invitation = new GroupInvitation
        {
            Id = Guid.NewGuid(), GroupId = Guid.NewGuid(), InviteCode = Guid.NewGuid().ToString("N"),
            CreatedByUserId = Guid.NewGuid(), CreatedAt = DateTime.UtcNow,
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
        var user = new User { Id = Guid.NewGuid(), Email = email, DisplayName = displayName, CreatedAt = DateTime.UtcNow };
        store.Users[user.Id] = user;
        return Task.FromResult(user);
    }

    public Task UpdateDisplayNameAsync(Guid userId, string displayName, CancellationToken ct = default) => Task.CompletedTask;
    public Task<bool> CheckPasswordAsync(Guid userId, string password, CancellationToken ct = default) => Task.FromResult(true);
    public Task<string> GeneratePasswordResetTokenAsync(Guid userId, CancellationToken ct = default) => Task.FromResult("reset");
    public Task<bool> ResetPasswordAsync(Guid userId, string token, string newPassword, CancellationToken ct = default) => Task.FromResult(false);
}

internal sealed class InMemoryGroups(InMemoryStore store) : IGroupRepository
{
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
}

internal sealed class InMemoryInvitations(InMemoryStore store) : IInvitationRepository
{
    public Task<GroupInvitation> CreateAsync(Guid groupId, Guid createdByUserId, CancellationToken ct = default) =>
        throw new NotSupportedException();

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
internal sealed class InMemoryTokens(IConfiguration config) : ITokenService
{
    private readonly ConcurrentDictionary<string, RefreshToken> _tokens = new();

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

    public Task<RefreshToken> CreateRefreshTokenAsync(Guid userId, CancellationToken ct = default)
    {
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(), UserId = userId, Token = Guid.NewGuid().ToString("N"),
            CreatedAt = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow.AddDays(30),
        };
        _tokens[token.Token] = token;
        return Task.FromResult(token);
    }

    public Task<RefreshToken?> GetActiveRefreshTokenAsync(string token, CancellationToken ct = default) =>
        Task.FromResult(_tokens.TryGetValue(token, out var t) && t.IsActive ? t : null);

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
}
