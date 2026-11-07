using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wishlist.Application.UseCases.Admin;
using Wishlist.Domain;

namespace Wishlist.API.Controllers;

/// <summary>Read-only site figures, for accounts with the Admin role only.</summary>
[ApiController]
[Route("api/admin")]
[Authorize(Roles = SiteRoles.Admin)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class AdminController(GetAdminOverviewUseCase overview, ListAdminUsersUseCase listUsers) : ControllerBase
{
    [HttpGet("overview")]
    public async Task<IActionResult> Overview(CancellationToken ct) => Ok(await overview.ExecuteAsync(ct));

    [HttpGet("users")]
    public async Task<IActionResult> Users(CancellationToken ct) => Ok(await listUsers.ExecuteAsync(ct));
}
