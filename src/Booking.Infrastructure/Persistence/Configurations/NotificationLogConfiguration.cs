using Booking.Domain.Notifications;
using Booking.Domain.Reservations;
using Booking.Domain.Restaurants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Booking.Infrastructure.Persistence.Configurations;

public sealed class NotificationLogConfiguration : IEntityTypeConfiguration<NotificationLog>
{
    public void Configure(EntityTypeBuilder<NotificationLog> builder)
    {
        builder.ToTable("NotificationLogs");

        builder.HasKey(notificationLog => notificationLog.Id);

        builder.Property(notificationLog => notificationLog.RecipientEmail)
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(notificationLog => notificationLog.Subject)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(notificationLog => notificationLog.CreatedAtUtc)
            .IsRequired();

        builder.Property(notificationLog => notificationLog.ErrorMessage)
            .HasMaxLength(1000);

        builder.HasOne<Restaurant>()
            .WithMany()
            .HasForeignKey(notificationLog => notificationLog.RestaurantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Reservation>()
            .WithMany()
            .HasForeignKey(notificationLog => notificationLog.ReservationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
