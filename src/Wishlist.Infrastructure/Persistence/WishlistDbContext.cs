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
    }
}
