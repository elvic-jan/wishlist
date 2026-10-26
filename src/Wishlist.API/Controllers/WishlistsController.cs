using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wishlist.Application.DTOs;
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
    ReserveGiftUseCase reserve,
    MarkGiftAsBoughtUseCase markBought,
    ReleaseGiftClaimUseCase release,
    KeepReservationUseCase keep) : ControllerBase
{
    [HttpGet("groups/{groupId:guid}/wishlists")]
    public async Task<IActionResult> ListGroupWishlists(Guid groupId, CancellationToken ct)
    {
        var result = await listGroup.ExecuteAsync(GetUserId(), groupId, ct);
        return Ok(result);
    }

    [HttpGet("wishlists/me")]
    public async Task<IActionResult> GetOwn(CancellationToken ct)
    {
        var result = await getOwn.ExecuteAsync(GetUserId(), ct);
        return Ok(result);
    }

    [HttpPost("wishlists/me/items")]
    public async Task<IActionResult> AddItem([FromBody] AddWishlistItemRequest request, CancellationToken ct)
    {
        var result = await addItem.ExecuteAsync(GetUserId(), request, ct);
        return Ok(result);
    }

    [HttpPatch("wishlists/me/items/{itemId:guid}")]
    public async Task<IActionResult> EditItem(Guid itemId, [FromBody] EditWishlistItemRequest request, CancellationToken ct)
    {
        var result = await editItem.ExecuteAsync(GetUserId(), itemId, request, ct);
        return Ok(result);
    }

    [HttpDelete("wishlists/me/items/{itemId:guid}")]
    public async Task<IActionResult> DeleteItem(Guid itemId, CancellationToken ct)
    {
        await deleteItem.ExecuteAsync(GetUserId(), itemId, ct);
        return NoContent();
    }

    [HttpGet("wishlists/{userId:guid}")]
    public async Task<IActionResult> GetMember(Guid userId, CancellationToken ct)
    {
        var result = await getMember.ExecuteAsync(GetUserId(), userId, ct);
        return Ok(result);
    }

    [HttpPost("wishlists/{userId:guid}/items/{itemId:guid}/claim")]
    public async Task<IActionResult> Reserve(Guid userId, Guid itemId, CancellationToken ct)
    {
        var result = await reserve.ExecuteAsync(GetUserId(), userId, itemId, ct);
        return Ok(result);
    }

    [HttpPatch("wishlists/{userId:guid}/items/{itemId:guid}/claim")]
    public async Task<IActionResult> MarkBought(Guid userId, Guid itemId, CancellationToken ct)
    {
        var result = await markBought.ExecuteAsync(GetUserId(), userId, itemId, ct);
        return Ok(result);
    }

    [HttpDelete("wishlists/{userId:guid}/items/{itemId:guid}/claim")]
    public async Task<IActionResult> Release(Guid userId, Guid itemId, CancellationToken ct)
    {
        await release.ExecuteAsync(GetUserId(), userId, itemId, ct);
        return NoContent();
    }

    /// <summary>The reserver keeps their reservation for another period.</summary>
    [HttpPost("wishlists/{userId:guid}/items/{itemId:guid}/claim/extend")]
    public async Task<IActionResult> Keep(Guid userId, Guid itemId, CancellationToken ct)
    {
        var result = await keep.ExecuteAsync(GetUserId(), userId, itemId, ct);
        return Ok(result);
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new InvalidOperationException("User ID claim missing."));
}
