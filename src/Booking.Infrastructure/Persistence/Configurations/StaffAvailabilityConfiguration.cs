using Booking.Domain.Restaurants;
using Booking.Domain.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Booking.Infrastructure.Persistence.Configurations;

public sealed class StaffAvailabilityConfiguration : IEntityTypeConfiguration<StaffAvailability>
{
    public void Configure(EntityTypeBuilder<StaffAvailability> builder)
    {
        builder.ToTable("StaffAvailabilities");

        builder.HasKey(availability => availability.Id);

        builder.Property(availability => availability.RestaurantId).IsRequired();
        builder.Property(availability => availability.StaffUserId).IsRequired();
        builder.Property(availability => availability.DayOfWeek)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(availability => availability.StartTime).IsRequired();
        builder.Property(availability => availability.EndTime).IsRequired();

        builder.HasOne<Restaurant>()
            .WithMany()
            .HasForeignKey(availability => availability.RestaurantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(availability => new { availability.RestaurantId, availability.StaffUserId });
    }
}
