using Booking.Domain.Restaurants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Booking.Infrastructure.Persistence.Configurations;

public sealed class RestaurantConfiguration : IEntityTypeConfiguration<Restaurant>
{
    public void Configure(EntityTypeBuilder<Restaurant> builder)
    {
        builder.ToTable("Restaurants");

        builder.HasKey(restaurant => restaurant.Id);

        builder.Property(restaurant => restaurant.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(restaurant => restaurant.PhoneNumber)
            .HasMaxLength(50);

        builder.Property(restaurant => restaurant.Email)
            .HasMaxLength(320);

        builder.Property(restaurant => restaurant.WidgetPrimaryColor)
            .HasMaxLength(7)
            .HasDefaultValue(Restaurant.DefaultWidgetPrimaryColor)
            .IsRequired();

        builder.Property(restaurant => restaurant.WidgetAccentColor)
            .HasMaxLength(7)
            .HasDefaultValue(Restaurant.DefaultWidgetAccentColor)
            .IsRequired();

        builder.Property(restaurant => restaurant.WidgetWelcomeText)
            .HasMaxLength(240);

        builder.Property(restaurant => restaurant.WidgetLogoUrl)
            .HasMaxLength(500);

        builder.HasMany(restaurant => restaurant.OpeningHours)
            .WithOne()
            .HasForeignKey("RestaurantId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(restaurant => restaurant.OpeningHours)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
