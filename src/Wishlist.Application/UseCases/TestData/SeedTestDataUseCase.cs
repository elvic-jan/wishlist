using Wishlist.Application.DTOs;
using Wishlist.Application.Interfaces;
using Wishlist.Application.UseCases.GiftClaims;
using Wishlist.Application.UseCases.Groups;
using Wishlist.Application.UseCases.Wishlists;

namespace Wishlist.Application.UseCases.TestData;

/// <summary>
/// Fills a test environment (staging) with a few people, two circles, wishes and a
/// reservation, through the same use cases the app uses. Only ever called on a
/// test environment (see Program.cs). The people get <c>+tag</c> addresses of one
/// allow-listed inbox, so their mail arrives there, and no password: sign in
/// through "Forgot password". Does nothing when the first test user exists.
/// </summary>
public class SeedTestDataUseCase(
    IUserRepository users,
    CreateGroupUseCase createGroup,
    GenerateGroupInvitationUseCase generateInvitation,
    JoinGroupViaInvitationUseCase joinGroup,
    AddWishlistItemUseCase addItem,
    ReserveGiftUseCase reserve)
{
    public static readonly IReadOnlyList<(string Tag, string Name)> People =
    [
        ("anna", "Anna Test"),
        ("ben", "Ben Test"),
        ("carla", "Carla Test"),
        ("david", "David Test"),
    ];

    /// <summary><c>you@example.com</c> + <c>anna</c> → <c>you+anna@example.com</c>.</summary>
    public static string AddressFor(string inbox, string tag)
    {
        var at = inbox.LastIndexOf('@');
        return $"{inbox[..at]}+{tag}@{inbox[(at + 1)..]}";
    }

    /// <returns>True when test data was created; false when it already existed.</returns>
    public async Task<bool> ExecuteAsync(string inbox, CancellationToken ct = default)
    {
        if (await users.GetByEmailAsync(AddressFor(inbox, People[0].Tag), ct) is not null)
            return false;

        var ids = new Dictionary<string, Guid>();
        foreach (var (tag, name) in People)
        {
            var user = await users.GetByEmailAsync(AddressFor(inbox, tag), ct)
                ?? await users.CreateWithoutPasswordAsync(Guid.NewGuid(), AddressFor(inbox, tag), name, ct: ct);
            await users.MarkEmailConfirmedAsync(user.Id, ct);
            ids[tag] = user.Id;
        }

        await CircleAsync("Test family", ids["anna"], [ids["ben"], ids["carla"]], ct);
        await CircleAsync("Test friends", ids["ben"], [ids["david"]], ct);

        var bike = await addItem.ExecuteAsync(ids["anna"],
            new AddWishlistItemRequest("Bike bell", "Something loud, any colour.", ["https://example.com/bike-bell"]), ct);
        await addItem.ExecuteAsync(ids["anna"], new AddWishlistItemRequest("Crime novel", null, null), ct);
        await addItem.ExecuteAsync(ids["ben"], new AddWishlistItemRequest("Board game", "Two to four players.", null), ct);
        await addItem.ExecuteAsync(ids["ben"], new AddWishlistItemRequest("Warm socks", null, null), ct);
        await addItem.ExecuteAsync(ids["carla"], new AddWishlistItemRequest("Houseplant", "Hard to kill, please.", null), ct);
        await addItem.ExecuteAsync(ids["david"], new AddWishlistItemRequest("Coffee beans", null, null), ct);

        // Ben has reserved Anna's bike bell, so reservations and their emails can be tried.
        await reserve.ExecuteAsync(ids["ben"], ids["anna"], bike.Id, ct);
        return true;
    }

    private async Task CircleAsync(string name, Guid owner, Guid[] members, CancellationToken ct)
    {
        var circle = await createGroup.ExecuteAsync(owner, new CreateGroupRequest(name), ct);
        foreach (var member in members)
        {
            var invitation = await generateInvitation.ExecuteAsync(owner, circle.Id, ct);
            var code = invitation.InviteUrl[(invitation.InviteUrl.LastIndexOf('/') + 1)..];
            await joinGroup.ExecuteAsync(member, code, ct);
        }
    }
}
