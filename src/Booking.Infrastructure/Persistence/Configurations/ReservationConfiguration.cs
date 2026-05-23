using Booking.Domain.Reservations;
using Booking.Domain.Restaurants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Booking.Infrastructure.Persistence.Configurations;

public sealed class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.ToTable("Reservations");

        builder.HasKey(reservation => reservation.Id);

        builder.Property(reservation => reservation.RestaurantId)
            .IsRequired();

        builder.Property(reservation => reservation.CustomerId)
            .IsRequired();

        builder.Property(reservation => reservation.ReservationDateTime)
            .IsRequired();

        builder.Property(reservation => reservation.PartySize)
            .IsRequired();

        builder.Property(reservation => reservation.Note)
            .HasMaxLength(1000);

        builder.Property(reservation => reservation.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(reservation => reservation.CreatedAtUtc)
            .IsRequired();

        builder.HasOne<Restaurant>()
            .WithMany()
            .HasForeignKey(reservation => reservation.RestaurantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(reservation => reservation.Customer)
            .WithMany()
            .HasForeignKey(reservation => reservation.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(reservation => new
        {
            reservation.RestaurantId,
            reservation.ReservationDateTime
        });
    }
}
