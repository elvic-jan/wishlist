using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Wishlist.Domain.Entities;
using Wishlist.Domain.Enums;

namespace Wishlist.Infrastructure.Persistence;

public class WishlistDbContext(DbContextOptions<WishlistDbContext> options)
    : IdentityDbContext<ApplicationUser, Microsoft.AspNetCore.Identity.IdentityRole<Guid>, Guid>(options),
      IDataProtectionKeyContext
{
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<GroupMember> GroupMembers => Set<GroupMember>();
    public DbSet<GroupInvitation> GroupInvitations => Set<GroupInvitation>();
    public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();
    public DbSet<WishlistItemUrl> WishlistItemUrls => Set<WishlistItemUrl>();
    public DbSet<GiftClaim> GiftClaims => Set<GiftClaim>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<LegacyUser> LegacyUsers => Set<LegacyUser>();
    public DbSet<LegacyItem> LegacyItems => Set<LegacyItem>();
    public DbSet<UserMessageState> UserMessageStates => Set<UserMessageState>();
    public DbSet<WishlistBuddy> WishlistBuddies => Set<WishlistBuddy>();
    public DbSet<CircleInvitation> CircleInvitations => Set<CircleInvitation>();
    public DbSet<SignInEvent> SignInEvents => Set<SignInEvent>();
    public DbSet<UserActivityDay> UserActivityDays => Set<UserActivityDay>();
    public DbSet<Drawing> Drawings => Set<Drawing>();
    public DbSet<DrawingParticipant> DrawingParticipants => Set<DrawingParticipant>();
    public DbSet<DrawingExclusion> DrawingExclusions => Set<DrawingExclusion>();
    public DbSet<DrawingAssignment> DrawingAssignments => Set<DrawingAssignment>();

    /// <summary>
    /// The circles that connect their members today: every permanent circle, and
    /// drawings that are neither cancelled nor more than a month past their end date.
    /// Wishlist access, reservations, buddies and the people list go through these.
    /// </summary>
    public IQueryable<Guid> ConnectingGroupIds(DateOnly today)
    {
        var oldestActiveEnd = today.AddMonths(-Drawing.VisibleMonths);
        return Groups
            .Where(g => g.Kind == GroupKind.Circle || Drawings.Any(d =>
                d.GroupId == g.Id && d.CancelledAt == null && d.EndDate >= oldestActiveEnd))
            .Select(g => g.Id);
    }

    /// <summary>Memberships of <see cref="ConnectingGroupIds"/>.</summary>
    public IQueryable<GroupMember> ConnectingMemberships(DateOnly today)
    {
        var groupIds = ConnectingGroupIds(today);
        return GroupMembers.Where(m => groupIds.Contains(m.GroupId));
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(e => e.Property(u => u.Language).HasMaxLength(8));

        builder.Entity<Group>(e =>
        {
            e.HasKey(g => g.Id);
            e.Property(g => g.Name).IsRequired().HasMaxLength(200);
            e.Property(g => g.Kind).HasConversion<string>().HasMaxLength(20);
        });

        // Secret Santa drawings: everything goes with the drawing's circle.
        builder.Entity<Drawing>(e =>
        {
            e.HasKey(d => d.GroupId);
            e.Property(d => d.Budget).HasMaxLength(200);
            e.HasOne<Group>().WithOne().HasForeignKey<Drawing>(d => d.GroupId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(d => d.OrganizerId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(d => d.OrganizerId);
        });

        builder.Entity<DrawingParticipant>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Name).IsRequired().HasMaxLength(100);
            e.Property(p => p.Email).HasMaxLength(256);
            e.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(p => p.InviteCode).IsRequired().HasMaxLength(64);
            e.HasIndex(p => p.InviteCode).IsUnique();
            e.HasIndex(p => new { p.GroupId, p.UserId }).IsUnique();
            e.HasIndex(p => p.UserId);
            e.HasOne<Drawing>().WithMany().HasForeignKey(p => p.GroupId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(p => p.UserId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<DrawingExclusion>(e =>
        {
            e.HasKey(x => new { x.GroupId, x.FirstParticipantId, x.SecondParticipantId });
            e.HasOne<Drawing>().WithMany().HasForeignKey(x => x.GroupId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<DrawingParticipant>().WithMany().HasForeignKey(x => x.FirstParticipantId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<DrawingParticipant>().WithMany().HasForeignKey(x => x.SecondParticipantId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<DrawingAssignment>(e =>
        {
            e.HasKey(a => a.GiverParticipantId);
            e.HasIndex(a => a.ReceiverParticipantId).IsUnique();
            e.HasIndex(a => a.GroupId);
            e.HasOne<Drawing>().WithMany().HasForeignKey(a => a.GroupId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<DrawingParticipant>().WithMany().HasForeignKey(a => a.GiverParticipantId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<DrawingParticipant>().WithMany().HasForeignKey(a => a.ReceiverParticipantId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<GroupMember>(e =>
        {
            e.HasKey(gm => new { gm.GroupId, gm.UserId });
            e.Property(gm => gm.Role).HasConversion<string>();
            e.Ignore(gm => gm.DisplayName);
            e.HasOne<Group>().WithMany().HasForeignKey(gm => gm.GroupId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<GroupInvitation>(e =>
        {
            e.HasKey(i => i.Id);
            e.Property(i => i.InviteCode).IsRequired().HasMaxLength(64);
            e.HasIndex(i => i.InviteCode).IsUnique();
            e.HasOne<Group>().WithMany().HasForeignKey(i => i.GroupId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<WishlistItem>(e =>
        {
            e.HasKey(w => w.Id);
            e.Property(w => w.Title).IsRequired().HasMaxLength(300);
            e.Ignore(w => w.AddedByName);
            // The buddy who added the wish; the wish stays when their account goes.
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(w => w.AddedById).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(w => w.GiftClaim).WithOne().HasForeignKey<GiftClaim>(c => c.WishlistItemId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(w => w.Urls)
                .WithOne()
                .HasForeignKey(u => u.WishlistItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<WishlistItemUrl>(e =>
        {
            e.HasKey(u => u.Id);
            e.Property(u => u.Url).IsRequired().HasMaxLength(2048);
            e.Property(u => u.WishlistItemId).IsRequired();
            e.Property(u => u.DisplayOrder).IsRequired();
            e.HasIndex(u => new { u.WishlistItemId, u.DisplayOrder });
        });

        builder.Entity<GiftClaim>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasIndex(c => c.WishlistItemId).IsUnique();
            e.Property(c => c.Status).HasConversion<string>();
        });

        builder.Entity<RefreshToken>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.TokenHash).IsRequired().HasMaxLength(64);
            e.HasIndex(r => r.TokenHash).IsUnique();
            e.HasIndex(r => r.UserId);
        });

        // The old wishlist site's usernames and live items, loaded once by the
        // legacy-load job. A deleted account releases its claim.
        builder.Entity<LegacyUser>(e =>
        {
            e.HasKey(u => u.LegacyId);
            e.Property(u => u.LegacyId).ValueGeneratedNever();
            e.Property(u => u.UserName).IsRequired().HasMaxLength(100);
            e.HasIndex(u => u.UserName).IsUnique();
            // One import per account; Postgres allows many NULLs in a unique index.
            e.HasIndex(u => u.ClaimedByUserId).IsUnique();
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(u => u.ClaimedByUserId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<LegacyItem>(e =>
        {
            e.HasKey(i => i.LegacyId);
            e.Property(i => i.LegacyId).ValueGeneratedNever();
            e.Property(i => i.Title).IsRequired().HasMaxLength(300);
            e.HasIndex(i => i.LegacyOwnerId);
            e.HasOne<LegacyUser>().WithMany().HasForeignKey(i => i.LegacyOwnerId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<UserMessageState>(e =>
        {
            e.HasKey(s => new { s.UserId, s.MessageId });
            e.Property(s => s.MessageId).HasMaxLength(100);
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        // An open invitation to join a circle, addressed to one existing user.
        builder.Entity<CircleInvitation>(e =>
        {
            e.HasKey(i => i.Id);
            e.HasIndex(i => new { i.GroupId, i.InviteeId }).IsUnique();
            e.HasIndex(i => i.InviteeId);
            e.HasOne<Group>().WithMany().HasForeignKey(i => i.GroupId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(i => i.InviteeId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(i => i.InvitedById).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<WishlistBuddy>(e =>
        {
            e.HasKey(b => new { b.OwnerId, b.BuddyId });
            e.HasIndex(b => b.BuddyId);
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(b => b.OwnerId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(b => b.BuddyId).OnDelete(DeleteBehavior.Cascade);
        });

        // Sign-ins and active days for the admin page; go with the account.
        builder.Entity<SignInEvent>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.Method).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(s => new { s.UserId, s.OccurredAt });
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<UserActivityDay>(e =>
        {
            e.HasKey(a => new { a.UserId, a.Day });
            e.HasIndex(a => a.Day);
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
