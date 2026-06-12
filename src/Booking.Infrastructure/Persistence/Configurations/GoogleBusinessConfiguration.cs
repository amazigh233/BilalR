using Booking.Domain.GoogleBusiness;
using Booking.Domain.Restaurants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Booking.Infrastructure.Persistence.Configurations;

public sealed class GoogleBusinessConnectionConfiguration : IEntityTypeConfiguration<GoogleBusinessConnection>
{
    public void Configure(EntityTypeBuilder<GoogleBusinessConnection> builder)
    {
        builder.ToTable("GoogleBusinessConnections");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(c => c.OAuthState).HasMaxLength(200).IsRequired();
        builder.Property(c => c.GbpAccountName).HasMaxLength(200);
        builder.Property(c => c.GbpLocationName).HasMaxLength(200);
        builder.Property(c => c.EncryptedAccessToken).HasColumnType("nvarchar(max)");
        builder.Property(c => c.EncryptedRefreshToken).HasColumnType("nvarchar(max)");
        builder.Property(c => c.LastSyncError).HasMaxLength(1000);
        builder.HasOne<Restaurant>().WithMany().HasForeignKey(c => c.RestaurantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(c => c.RestaurantId).IsUnique();
    }
}

public sealed class GoogleReviewConfiguration : IEntityTypeConfiguration<GoogleReview>
{
    public void Configure(EntityTypeBuilder<GoogleReview> builder)
    {
        builder.ToTable("GoogleReviews");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.ReviewName).HasMaxLength(500).IsRequired();
        builder.Property(r => r.ReviewerDisplayName).HasMaxLength(200);
        builder.Property(r => r.Comment).HasColumnType("nvarchar(max)");
        builder.Property(r => r.ReplyComment).HasColumnType("nvarchar(max)");
        builder.HasOne<Restaurant>().WithMany().HasForeignKey(r => r.RestaurantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(r => new { r.RestaurantId, r.ReviewName }).IsUnique();
        builder.HasIndex(r => new { r.RestaurantId, r.CreateTime });
    }
}
