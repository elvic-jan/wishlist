namespace Wishlist.Domain.Entities;

/// <summary>
/// An open invitation for an existing user to join a circle, sent by a member
/// who knows them from another circle. It waits for the invitee's answer;
/// joining or declining removes it.
/// </summary>
public class CircleInvitation
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public Guid InviteeId { get; set; }

    /// <summary>Null once the inviter's account is gone.</summary>
    public Guid? InvitedById { get; set; }

    public DateTime CreatedAt { get; set; }
}
