using Booking.Domain.Restaurants;
using Booking.Domain.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Booking.Infrastructure.Persistence.Configurations;

public sealed class LeaveRequestConfiguration : IEntityTypeConfiguration<LeaveRequest>
{
    public void Configure(EntityTypeBuilder<LeaveRequest> builder)
    {
        builder.ToTable("LeaveRequests");

        builder.HasKey(leave => leave.Id);

        builder.Property(leave => leave.RestaurantId).IsRequired();
        builder.Property(leave => leave.StaffUserId).IsRequired();
        builder.Property(leave => leave.FromDate).IsRequired();
        builder.Property(leave => leave.ToDate).IsRequired();
        builder.Property(leave => leave.Reason).HasMaxLength(500);
        builder.Property(leave => leave.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(leave => leave.CreatedAtUtc).IsRequired();

        builder.HasOne<Restaurant>()
            .WithMany()
            .HasForeignKey(leave => leave.RestaurantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(leave => new { leave.RestaurantId, leave.Status });
        builder.HasIndex(leave => new { leave.StaffUserId, leave.FromDate });
    }
}
