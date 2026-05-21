using Booking.Domain.Restaurants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Booking.Infrastructure.Persistence.Configurations;

public sealed class OpeningHourConfiguration : IEntityTypeConfiguration<OpeningHour>
{
    public void Configure(EntityTypeBuilder<OpeningHour> builder)
    {
        builder.ToTable("OpeningHours");

        builder.HasKey(openingHour => openingHour.Id);

        builder.Property<Guid>("RestaurantId")
            .IsRequired();

        builder.Property(openingHour => openingHour.DayOfWeek)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(openingHour => openingHour.OpensAt)
            .HasColumnType("time")
            .IsRequired();

        builder.Property(openingHour => openingHour.ClosesAt)
            .HasColumnType("time")
            .IsRequired();

        builder.HasIndex("RestaurantId", nameof(OpeningHour.DayOfWeek));
    }
}
