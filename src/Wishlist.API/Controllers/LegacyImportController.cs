using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wishlist.Application.DTOs;
using Wishlist.Application.UseCases.LegacyImport;

namespace Wishlist.API.Controllers;

/// <summary>Importing one's wishes from the old wishlist site, once.</summary>
[ApiController]
[Route("api/legacy-import")]
[Authorize]
public class LegacyImportController(
    GetLegacyImportStatusUseCase getStatus,
    PreviewLegacyImportUseCase preview,
    ImportLegacyItemsUseCase import) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Status(CancellationToken ct) =>
        Ok(await getStatus.ExecuteAsync(GetUserId(), ct));

    [HttpPost("preview")]
    public async Task<IActionResult> Preview([FromBody] LegacyPreviewRequest request, CancellationToken ct) =>
        Ok(await preview.ExecuteAsync(GetUserId(), request.UserName, ct));

    [HttpPost]
    public async Task<IActionResult> Import([FromBody] LegacyImportRequest request, CancellationToken ct) =>
        Ok(await import.ExecuteAsync(GetUserId(), request, ct));

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new InvalidOperationException("User ID claim missing."));
}
