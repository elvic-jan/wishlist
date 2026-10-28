using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Wishlist.Application.DTOs;
using Wishlist.Application.UseCases.Auth;
using Wishlist.Domain.Entities;
using Wishlist.Domain.Exceptions;
using Wishlist.Infrastructure.Services;

namespace Wishlist.API.Tests;

/// <summary>Password sign-in stops after too many wrong passwords per account or per IP.</summary>
public class LoginAttemptLimitTests
{
    private const string Password = "Correct-horse-1";
    private const string Ip = "203.0.113.7";

    private readonly InMemoryStore _store = new();
    private readonly LoginUserUseCase _login;

    public LoginAttemptLimitTests()
    {
        var limiter = new LoginAttemptLimiter(NullLogger<LoginAttemptLimiter>.Instance);
        _login = new LoginUserUseCase(new InMemoryUsers(_store), new NoTokens(), limiter);
    }

    private async Task<User> Account(string email) =>
        await new InMemoryUsers(_store).CreateAsync(email, "Someone", Password);

    private Task Attempt(string email, string password, string ip = Ip) =>
        _login.ExecuteAsync(new LoginRequest(email, password), ip);

    [Fact]
    public async Task Ten_wrong_passwords_block_the_account_even_for_the_right_one()
    {
        await Account("anna@example.test");
        for (var i = 0; i < LoginAttemptLimiter.DefaultFailuresPerEmail; i++)
            await Assert.ThrowsAsync<DomainException>(() => Attempt("anna@example.test", "wrong", $"198.51.100.{i}"));

        var blocked = await Assert.ThrowsAsync<TooManyRequestsException>(() => Attempt("Anna@Example.test ", Password, "198.51.100.200"));
        Assert.Contains("Face ID", blocked.Message);
    }

    [Fact]
    public async Task Thirty_failures_from_one_ip_block_that_ip_for_every_account()
    {
        await Account("bob@example.test");
        for (var i = 0; i < LoginAttemptLimiter.DefaultFailuresPerIp; i++)
            await Assert.ThrowsAsync<DomainException>(() => Attempt($"guess{i}@example.test", "wrong"));

        await Assert.ThrowsAsync<TooManyRequestsException>(() => Attempt("bob@example.test", Password));
        await Attempt("bob@example.test", Password, ip: "192.0.2.1"); // another IP is fine
    }

    [Fact]
    public async Task A_few_typos_then_the_right_password_signs_in()
    {
        await Account("carl@example.test");
        for (var i = 0; i < 3; i++)
            await Assert.ThrowsAsync<DomainException>(() => Attempt("carl@example.test", "typo"));

        await Attempt("carl@example.test", Password);
    }

    [Fact]
    public async Task Login_endpoint_answers_429_once_blocked()
    {
        await using var api = new InMemoryApi();
        var client = api.CreateClient();
        await new InMemoryUsers(api.Store).CreateAsync("dana@example.test", "Dana", Password);

        Task<HttpResponseMessage> Post(string password) =>
            InMemoryApi.Post(client, "/api/auth/login", new { email = "dana@example.test", password }, ip: Ip);

        for (var i = 0; i < LoginAttemptLimiter.DefaultFailuresPerEmail; i++)
            Assert.Equal(HttpStatusCode.BadRequest, (await Post("wrong")).StatusCode);

        var response = await Post(Password);
        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorBody>();
        Assert.Equal(LoginUserUseCase.TooManyAttempts, body?.Error);
        Assert.Null(InMemoryApi.SetCookieValue(response, "refreshToken"));
    }

    private sealed record ErrorBody(string Error);

    private sealed class NoTokens : Application.Interfaces.ITokenService
    {
        public string GenerateAccessToken(User user) => "access";
        public Task<string> CreateRefreshTokenAsync(Guid userId, CancellationToken ct = default) => Task.FromResult("refresh");
        public Task<Guid?> ConsumeRefreshTokenAsync(string token, CancellationToken ct = default) => throw new NotSupportedException();
        public Task RevokeRefreshTokenAsync(string token, CancellationToken ct = default) => throw new NotSupportedException();
        public Task RevokeAllUserRefreshTokensAsync(Guid userId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task RevokeOtherUserRefreshTokensAsync(Guid userId, string? keepToken, CancellationToken ct = default) => throw new NotSupportedException();
    }
}
