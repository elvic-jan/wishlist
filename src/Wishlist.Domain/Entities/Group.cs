using Wishlist.Domain.Enums;

namespace Wishlist.Domain.Entities;

public class Group
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>A permanent circle, or a Secret Santa drawing (with a <see cref="Drawing"/> row).</summary>
    public GroupKind Kind { get; set; } = GroupKind.Circle;
}
