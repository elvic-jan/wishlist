using Wishlist.Application.DTOs;
using Wishlist.Application.Interfaces;
using Wishlist.Domain.Entities;

namespace Wishlist.Application.UseCases.Groups;

/// <summary>
/// Lets the SPA tell a visitor straight away whether their invite link works,
/// and who invited them to which circle (or drawing), before they fill in the
/// sign-up form. Also feeds the invite link preview. Same 404/410 outcomes as
/// register/join.
/// </summary>
public class CheckInvitationUseCase(
    InvitationResolver invitationResolver, IGroupRepository groups, IUserRepository users, Localization.ILanguageContext language)
{
    public const string UnknownInviter = "Someone";

    public async Task<InvitationStatusDto> ExecuteAsync(string code, CancellationToken ct = default)
    {
        var target = await invitationResolver.ResolveAsync(code, ct);
        var group = await groups.GetByIdAsync(target.GroupId, ct);
        var inviterId = target.Drawing is { } drawing ? drawing.OrganizerId : target.Circle!.CreatedByUserId;
        var inviter = inviterId is { } id ? await users.GetByIdAsync(id, ct) : null;

        var expiresAt = target.Drawing is { } d
            ? d.EndDate.AddMonths(Drawing.VisibleMonths).ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc)
            : target.Circle!.ExpiresAt;
        var participant = target.Participant;

        return new InvitationStatusDto(
            expiresAt,
            inviter?.DisplayName ?? Localization.Texts.T(language.Language, UnknownInviter),
            group?.Name ?? Localization.Texts.T(language.Language, "a circle"))
        {
            InviterLanguage = Localization.Languages.Parse(inviter?.Language) ?? Localization.Languages.Default,
            Kind = participant is null ? "circle" : "drawing",
            Name = participant?.Name,
            Email = participant?.Email,
            SignInRequired = participant?.UserId is not null,
        };
    }
}
