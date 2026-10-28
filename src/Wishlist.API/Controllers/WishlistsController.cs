using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wishlist.Application.DTOs;
using Wishlist.Application.UseCases.Buddies;
using Wishlist.Application.UseCases.GiftClaims;
using Wishlist.Application.UseCases.Wishlists;

namespace Wishlist.API.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class WishlistsController(
    GetOwnWishlistUseCase getOwn,
    AddWishlistItemUseCase addItem,
    EditWishlistItemUseCase editItem,
    DeleteWishlistItemUseCase deleteItem,
    GetMemberWishlistUseCase getMember,
    ListGroupWishlistsUseCase listGroup,
    ListConnectedPeopleUseCase listPeople,
    ReserveGiftUseCase reserve,
    MarkGiftAsBoughtUseCase markBought,
    ReleaseGiftClaimUseCase release,
    KeepReservationUseCase keep,
    ManageBuddiesUseCase buddies) : ControllerBase
{
    [HttpGet("groups/{groupId:guid}/wishlists")]
    public async Task<IActionResult> ListGroupWishlists(Guid groupId, CancellationToken ct)
    {
        var result = await listGroup.ExecuteAsync(User.GetUserId(), groupId, ct);
        return Ok(result);
    }

    /// <summary>Everyone I share a circle with, each once, with the circles we share.</summary>
    [HttpGet("wishlists/people")]
    public async Task<IActionResult> ListPeople(CancellationToken ct) =>
        Ok(await listPeople.ExecuteAsync(User.GetUserId(), ct));

    [HttpGet("wishlists/me")]
    public async Task<IActionResult> GetOwn(CancellationToken ct)
    {
        var result = await getOwn.ExecuteAsync(User.GetUserId(), ct);
        return Ok(result);
    }

    [HttpPost("wishlists/me/items")]
    public async Task<IActionResult> AddItem([FromBody] AddWishlistItemRequest request, CancellationToken ct)
    {
        var result = await addItem.ExecuteAsync(User.GetUserId(), request, ct);
        return Ok(result);
    }

    [HttpPatch("wishlists/me/items/{itemId:guid}")]
    public async Task<IActionResult> EditItem(Guid itemId, [FromBody] EditWishlistItemRequest request, CancellationToken ct)
    {
        var result = await editItem.ExecuteAsync(User.GetUserId(), itemId, request, ct);
        return Ok(result);
    }

    [HttpDelete("wishlists/me/items/{itemId:guid}")]
    public async Task<IActionResult> DeleteItem(Guid itemId, CancellationToken ct)
    {
        await deleteItem.ExecuteAsync(User.GetUserId(), itemId, ct);
        return NoContent();
    }

    // --- Buddies: people with full control over my wishlist ---

    [HttpGet("wishlists/me/buddies")]
    public async Task<IActionResult> GetBuddies(CancellationToken ct) =>
        Ok(await buddies.GetBuddiesAsync(User.GetUserId(), ct));

    [HttpGet("wishlists/me/buddy-candidates")]
    public async Task<IActionResult> GetBuddyCandidates(CancellationToken ct) =>
        Ok(await buddies.GetCandidatesAsync(User.GetUserId(), ct));

    /// <summary>The wishlists I am a buddy for.</summary>
    [HttpGet("wishlists/me/buddy-of")]
    public async Task<IActionResult> GetBuddyOf(CancellationToken ct) =>
        Ok(await buddies.GetBuddyOfAsync(User.GetUserId(), ct));

    [HttpPost("wishlists/me/buddies")]
    public async Task<IActionResult> AddBuddy([FromBody] AddBuddyRequest request, CancellationToken ct) =>
        Ok(await buddies.AddAsync(User.GetUserId(), request.UserId, ct));

    [HttpDelete("wishlists/me/buddies/{buddyId:guid}")]
    public async Task<IActionResult> RemoveBuddy(Guid buddyId, CancellationToken ct)
    {
        await buddies.RemoveAsync(User.GetUserId(), buddyId, ct);
        return NoContent();
    }

    // --- A buddy changes the owner's wishlist ---

    [HttpPost("wishlists/{userId:guid}/items")]
    public async Task<IActionResult> AddItemAsBuddy(Guid userId, [FromBody] AddWishlistItemRequest request, CancellationToken ct) =>
        Ok(await addItem.ExecuteAsBuddyAsync(User.GetUserId(), userId, request, ct));

    [HttpPatch("wishlists/{userId:guid}/items/{itemId:guid}")]
    public async Task<IActionResult> EditItemAsBuddy(Guid userId, Guid itemId, [FromBody] EditWishlistItemRequest request, CancellationToken ct) =>
        Ok(await editItem.ExecuteAsBuddyAsync(User.GetUserId(), userId, itemId, request, ct));

    [HttpDelete("wishlists/{userId:guid}/items/{itemId:guid}")]
    public async Task<IActionResult> DeleteItemAsBuddy(Guid userId, Guid itemId, CancellationToken ct)
    {
        await deleteItem.ExecuteAsBuddyAsync(User.GetUserId(), userId, itemId, ct);
        return NoContent();
    }

    [HttpGet("wishlists/{userId:guid}")]
    public async Task<IActionResult> GetMember(Guid userId, CancellationToken ct)
    {
        var result = await getMember.ExecuteAsync(User.GetUserId(), userId, ct);
        return Ok(result);
    }

    [HttpPost("wishlists/{userId:guid}/items/{itemId:guid}/claim")]
    public async Task<IActionResult> Reserve(Guid userId, Guid itemId, CancellationToken ct)
    {
        var result = await reserve.ExecuteAsync(User.GetUserId(), userId, itemId, ct);
        return Ok(result);
    }

    [HttpPatch("wishlists/{userId:guid}/items/{itemId:guid}/claim")]
    public async Task<IActionResult> MarkBought(Guid userId, Guid itemId, CancellationToken ct)
    {
        var result = await markBought.ExecuteAsync(User.GetUserId(), userId, itemId, ct);
        return Ok(result);
    }

    [HttpDelete("wishlists/{userId:guid}/items/{itemId:guid}/claim")]
    public async Task<IActionResult> Release(Guid userId, Guid itemId, CancellationToken ct)
    {
        await release.ExecuteAsync(User.GetUserId(), userId, itemId, ct);
        return NoContent();
    }

    /// <summary>The reserver keeps their reservation for another period.</summary>
    [HttpPost("wishlists/{userId:guid}/items/{itemId:guid}/claim/extend")]
    public async Task<IActionResult> Keep(Guid userId, Guid itemId, CancellationToken ct)
    {
        var result = await keep.ExecuteAsync(User.GetUserId(), userId, itemId, ct);
        return Ok(result);
    }
}
