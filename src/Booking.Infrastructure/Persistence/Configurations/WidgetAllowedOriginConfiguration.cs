using Booking.Domain.Restaurants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Booking.Infrastructure.Persistence.Configurations;

public sealed class WidgetAllowedOriginConfiguration : IEntityTypeConfiguration<WidgetAllowedOrigin>
{
    public void Configure(EntityTypeBuilder<WidgetAllowedOrigin> builder)
    {
        builder.ToTable("WidgetAllowedOrigins");

        builder.HasKey(origin => origin.Id);

        builder.Property(origin => origin.Origin)
            .HasMaxLength(500)
            .IsRequired();

        builder.HasIndex(origin => new { origin.RestaurantId, origin.Origin })
            .IsUnique();

        builder.HasOne<Restaurant>()
            .WithMany()
            .HasForeignKey(origin => origin.RestaurantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
