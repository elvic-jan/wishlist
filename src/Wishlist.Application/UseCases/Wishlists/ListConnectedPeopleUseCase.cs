using Wishlist.Application.DTOs;
using Wishlist.Application.Interfaces;

namespace Wishlist.Application.UseCases.Wishlists;

/// <summary>Everyone whose wishlist the user may see: the people they share a circle with, each once.</summary>
public class ListConnectedPeopleUseCase(IGroupRepository groups)
{
    public async Task<IReadOnlyList<ConnectedPersonDto>> ExecuteAsync(Guid userId, CancellationToken ct = default)
    {
        var rows = await groups.GetCircleMatesAsync(userId, ct);
        return rows
            .Where(r => r.UserId != userId)
            .GroupBy(r => r.UserId)
            .Select(person => new ConnectedPersonDto(
                person.Key,
                person.First().DisplayName,
                person
                    .DistinctBy(r => r.GroupId)
                    .Select(r => new SharedCircleDto(r.GroupId, r.GroupName))
                    .OrderBy(c => c.Name, StringComparer.InvariantCultureIgnoreCase)
                    .ThenBy(c => c.Id)
                    .ToList()))
            .OrderBy(p => p.DisplayName, StringComparer.InvariantCultureIgnoreCase)
            .ThenBy(p => p.UserId)
            .ToList();
    }
}
