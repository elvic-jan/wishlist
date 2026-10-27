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

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Group>(e =>
        {
            e.HasKey(g => g.Id);
            e.Property(g => g.Name).IsRequired().HasMaxLength(200);
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
            e.Property(r => r.Token).IsRequired().HasMaxLength(512);
            e.HasIndex(r => r.Token).IsUnique();
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

        builder.Entity<WishlistBuddy>(e =>
        {
            e.HasKey(b => new { b.OwnerId, b.BuddyId });
            e.HasIndex(b => b.BuddyId);
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(b => b.OwnerId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(b => b.BuddyId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
