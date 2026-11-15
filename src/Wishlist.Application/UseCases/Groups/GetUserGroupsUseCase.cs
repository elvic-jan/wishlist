using Wishlist.Application.DTOs;
using Wishlist.Application.Interfaces;

namespace Wishlist.Application.UseCases.Groups;

/// <summary>
/// The user's circles: unanswered Secret Santa drawings first, then the
/// permanent circles, then the other drawings (including ended ones in their
/// last month, and cancelled ones for their organizer).
/// </summary>
public class GetUserGroupsUseCase(IGroupRepository groups, IDrawingRepository drawings, TimeProvider time)
{
    public async Task<IReadOnlyList<GroupDto>> ExecuteAsync(Guid userId, CancellationToken ct = default)
    {
        var now = time.GetUtcNow().UtcDateTime;
        var today = DateOnly.FromDateTime(now);
        var circles = (await groups.GetUserGroupsAsync(userId, ct))
            .Select(g => new GroupDto(g.Id, g.Name, g.CreatedAt));

        var drawingDtos = (await drawings.GetForUserAsync(userId, now, ct))
            .Select(d => new GroupDto(d.Group.Id, d.Group.Name, d.Group.CreatedAt, "drawing", new DrawingSummaryDto(
                d.Drawing.EndDate,
                d.IsOrganizer,
                d.Me is null ? null : DrawingStatuses.Of(d.Me.Status),
                d.Drawing.IsDrawn,
                d.Drawing.IsEnded(today),
                d.Drawing.IsCancelled)))
            .OrderBy(g => g.Drawing!.EndDate)
            .ToList();

        static bool Unanswered(GroupDto g) => g.Drawing!.MyStatus == DrawingStatuses.Invited && !g.Drawing.Cancelled;
        return [.. drawingDtos.Where(Unanswered), .. circles, .. drawingDtos.Where(g => !Unanswered(g))];
    }
}
