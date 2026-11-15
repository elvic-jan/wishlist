using Wishlist.Application.Interfaces;
using Wishlist.Domain.Enums;
using Wishlist.Domain.Exceptions;

namespace Wishlist.Application.UseCases.Drawings;

/// <summary>
/// Loads a drawing for the person asking. Someone who may not see it gets the
/// same 404 as for a drawing that doesn't exist.
/// </summary>
public class DrawingAccess(IDrawingRepository drawings, TimeProvider time)
{
    public DateTime Now => time.GetUtcNow().UtcDateTime;
    public DateOnly Today => DateOnly.FromDateTime(Now);

    /// <summary>The organizer, or a participant who hasn't declined, while it is visible to them.</summary>
    public async Task<DrawingState> LoadVisibleAsync(Guid userId, Guid drawingId, CancellationToken ct = default)
    {
        var state = await drawings.GetAsync(drawingId, ct) ?? throw NotFound();
        if (IsOrganizer(state, userId) && state.Drawing.IsVisibleToOrganizer(Now))
            return state;
        var me = state.ParticipantOf(userId);
        if (me is not null && me.Status != DrawingParticipantStatus.Declined && state.Drawing.IsActive(Today))
            return state;
        throw NotFound();
    }

    /// <summary>Only the organizer, while the drawing is visible to them.</summary>
    public async Task<DrawingState> LoadAsOrganizerAsync(Guid userId, Guid drawingId, CancellationToken ct = default)
    {
        var state = await LoadVisibleAsync(userId, drawingId, ct);
        if (!IsOrganizer(state, userId))
            throw new ForbiddenException("Only the organizer can do this.");
        return state;
    }

    /// <summary>A participant acting on the drawing: it must not be cancelled or expired.</summary>
    public async Task<DrawingState> LoadActiveAsync(Guid userId, Guid drawingId, CancellationToken ct = default)
    {
        var state = await LoadVisibleAsync(userId, drawingId, ct);
        if (!state.Drawing.IsActive(Today))
            throw new ConflictException("This drawing is cancelled.");
        return state;
    }

    public static bool IsOrganizer(DrawingState state, Guid userId) => state.Drawing.OrganizerId == userId;

    public static NotFoundException NotFound() => new NotFoundException("Drawing not found.");
}
