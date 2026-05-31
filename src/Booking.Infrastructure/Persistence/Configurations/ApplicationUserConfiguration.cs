using Booking.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Booking.Infrastructure.Persistence.Configurations;

public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(user => user.DisplayName)
            .HasMaxLength(200);

        builder.HasOne(user => user.Restaurant)
            .WithMany()
            .HasForeignKey(user => user.RestaurantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
