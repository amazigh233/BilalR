using Booking.Domain.Restaurants;
using Booking.Domain.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Booking.Infrastructure.Persistence.Configurations;

public sealed class ShiftConfiguration : IEntityTypeConfiguration<Shift>
{
    public void Configure(EntityTypeBuilder<Shift> builder)
    {
        builder.ToTable("Shifts");

        builder.HasKey(shift => shift.Id);

        builder.Property(shift => shift.RestaurantId).IsRequired();
        builder.Property(shift => shift.StaffUserId).IsRequired();
        builder.Property(shift => shift.ShiftDate).IsRequired();
        builder.Property(shift => shift.StartTime).IsRequired();
        builder.Property(shift => shift.EndTime).IsRequired();
        builder.Property(shift => shift.Note).HasMaxLength(500);

        builder.HasOne<Restaurant>()
            .WithMany()
            .HasForeignKey(shift => shift.RestaurantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(shift => new { shift.RestaurantId, shift.ShiftDate });
        builder.HasIndex(shift => new { shift.StaffUserId, shift.ShiftDate });
    }
}
