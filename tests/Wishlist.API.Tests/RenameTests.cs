using Wishlist.Application.Messages;
using Wishlist.Domain.Entities;
using Wishlist.Infrastructure.Options;

namespace Wishlist.API.Tests;

/// <summary>The move from GiftCircles (giftcircles.app) to Kadoozle (kadoozle.app).</summary>
public class RenameTests
{
    [Theory]
    [InlineData("https://www.kadoozle.app", "kadoozle.app")]
    [InlineData("https://kadoozle.app/", "kadoozle.app")]
    public void Passkeys_belong_to_kadoozle_app(string publicBaseUrl, string expected) =>
        Assert.Equal(expected, PasskeyConfigOptions.ResolveServerDomain(null, publicBaseUrl));

    [Fact]
    public async Task Move_message_is_for_accounts_from_before_the_move()
    {
        var store = new InMemoryStore();
        var message = new RenamedToKadoozleMessage(new InMemoryUsers(store));
        var before = AddUser(store, RenamedToKadoozleMessage.MovedAt.AddDays(-30));
        var after = AddUser(store, RenamedToKadoozleMessage.MovedAt.AddDays(1));

        Assert.True(await message.AppliesToAsync(before));
        Assert.False(await message.AppliesToAsync(after));
        Assert.False(await message.AppliesToAsync(Guid.NewGuid()));
        Assert.Equal(new MessageAction("Add a passkey", "/settings/security"), message.Action);
        Assert.Contains("kadoozle.app", message.Text);
    }

    private static Guid AddUser(InMemoryStore store, DateTime createdAt)
    {
        var user = new User { Id = Guid.NewGuid(), Email = $"{Guid.NewGuid():N}@example.test", DisplayName = "U", CreatedAt = createdAt };
        store.Users[user.Id] = user;
        return user.Id;
    }
}
