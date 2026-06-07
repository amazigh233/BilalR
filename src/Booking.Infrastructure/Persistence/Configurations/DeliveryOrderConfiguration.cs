using Booking.Domain.Delivery;
using Booking.Domain.Restaurants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Booking.Infrastructure.Persistence.Configurations;

public sealed class DeliveryOrderConfiguration : IEntityTypeConfiguration<DeliveryOrder>
{
    public void Configure(EntityTypeBuilder<DeliveryOrder> builder)
    {
        builder.ToTable("DeliveryOrders");

        builder.HasKey(order => order.Id);

        builder.Property(order => order.RestaurantId).IsRequired();
        builder.Property(order => order.Provider)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(order => order.ExternalOrderId).HasMaxLength(100).IsRequired();
        builder.Property(order => order.CustomerName).HasMaxLength(200).IsRequired();
        builder.Property(order => order.CustomerPhone).HasMaxLength(40);
        builder.Property(order => order.DeliveryAddress).HasMaxLength(400);
        builder.Property(order => order.Note).HasMaxLength(1000);
        builder.Property(order => order.Status).HasMaxLength(60).IsRequired();
        builder.Property(order => order.TotalAmount).HasPrecision(18, 2);
        builder.Property(order => order.Currency).HasMaxLength(3).IsRequired();
        builder.Property(order => order.PlacedAtUtc).IsRequired();
        builder.Property(order => order.CreatedAtUtc).IsRequired();

        // Line items are stored inline as JSON; they never need their own table or tenant filter.
        builder.OwnsMany(order => order.Items, items =>
        {
            items.ToJson();
            items.Property(line => line.Name).HasMaxLength(200);
            items.Property(line => line.UnitPrice).HasPrecision(18, 2);
        });

        builder.HasOne<Restaurant>()
            .WithMany()
            .HasForeignKey(order => order.RestaurantId)
            .OnDelete(DeleteBehavior.Restrict);

        // Idempotency: a platform order maps to exactly one row per restaurant.
        builder.HasIndex(order => new { order.RestaurantId, order.Provider, order.ExternalOrderId })
            .IsUnique();
        builder.HasIndex(order => new { order.RestaurantId, order.PlacedAtUtc });
    }
}
