using System.Net.Mail;
using Wishlist.Application.DTOs;
using Wishlist.Application.Interfaces;
using Wishlist.Domain.Entities;
using Wishlist.Domain.Enums;
using Wishlist.Domain.Exceptions;

namespace Wishlist.Application.UseCases.Drawings;

/// <summary>
/// Turns the organizer's picks (circles minus some members, circle-mates, and
/// names with email addresses) into new participants, each person once.
/// A typed address that belongs to an account becomes that account, but keeps
/// the typed name, so the organizer sees the same either way.
/// </summary>
public class DrawingParticipantPicker(IDrawingRepository drawings, IGroupRepository groups, IUserRepository users)
{
    public const int MaxParticipants = 100;

    public async Task<IReadOnlyList<DrawingParticipant>> AddAsync(
        Guid drawingId, Guid organizerId, IReadOnlyCollection<DrawingParticipant> existing,
        AddDrawingParticipantsRequest? request, DateTime now, CancellationToken ct = default)
    {
        if (request is null) return [];

        var userIds = existing.Where(p => p.UserId is not null).Select(p => p.UserId!.Value).ToHashSet();
        var emails = existing.Where(p => p.Email is not null).Select(p => Normalize(p.Email!)).ToHashSet();
        var added = new List<DrawingParticipant>();

        void Add(Guid? userId, string name, string? email)
        {
            if (userId is { } id && !userIds.Add(id)) return;
            if (email is not null && !emails.Add(Normalize(email))) return;
            added.Add(new DrawingParticipant
            {
                Id = Guid.NewGuid(),
                GroupId = drawingId,
                UserId = userId,
                Name = name,
                Email = email,
                Status = DrawingParticipantStatus.Invited,
                InviteCode = Guid.NewGuid().ToString("N"),
                CreatedAt = now,
            });
        }

        // Typed names and addresses are checked first, so a bad one changes nothing.
        var invites = new List<(string Name, string Email)>();
        foreach (var invite in request.Invites ?? [])
        {
            var name = invite.Name?.Trim();
            var email = invite.Email?.Trim();
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email) || !IsEmail(email))
                throw new DomainException("Enter a name and a valid email address for everyone you invite.");
            InputLimits.Check(name, InputLimits.DisplayName, "Name");
            InputLimits.Check(email, InputLimits.Email, "Email address");
            invites.Add((name, email));
        }

        // Whole circles the organizer is in, minus the members they left out.
        foreach (var selection in request.Circles ?? [])
        {
            var circle = await groups.GetByIdAsync(selection.CircleId, ct);
            if (circle is null || circle.Kind != GroupKind.Circle || !await groups.IsMemberAsync(circle.Id, organizerId, ct))
                continue;
            var left = (selection.ExcludedUserIds ?? []).ToHashSet();
            foreach (var member in await groups.GetMembersAsync(circle.Id, ct))
                if (member.UserId != organizerId && !left.Contains(member.UserId))
                    Add(member.UserId, member.DisplayName, null);
        }

        // Individual people the organizer shares a circle with.
        var mates = (await groups.GetCircleMatesAsync(organizerId, ct))
            .GroupBy(m => m.UserId)
            .ToDictionary(g => g.Key, g => g.First().DisplayName);
        foreach (var id in request.UserIds ?? [])
            if (id != organizerId && mates.TryGetValue(id, out var name))
                Add(id, name, null);

        foreach (var (name, email) in invites)
        {
            var account = await users.GetByEmailAsync(email, ct);
            if (account?.Id == organizerId && existing.Any(p => p.UserId == organizerId))
                continue;
            Add(account?.Id, name, email);
        }

        if (existing.Count + added.Count > MaxParticipants)
            throw new DomainException("A drawing can have at most {0} people.", MaxParticipants);

        if (added.Count > 0)
            await drawings.AddParticipantsAsync(added, ct);
        return added;
    }

    private static string Normalize(string email) => email.Trim().ToUpperInvariant();

    private static bool IsEmail(string value) =>
        MailAddress.TryCreate(value, out var address) && address.Address == value && value.Contains('.', StringComparison.Ordinal);
}
