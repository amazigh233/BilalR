using Booking.Domain.Delivery;
using Booking.Domain.Restaurants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Booking.Infrastructure.Persistence.Configurations;

public sealed class DeliveryIntegrationConfiguration : IEntityTypeConfiguration<DeliveryIntegration>
{
    public void Configure(EntityTypeBuilder<DeliveryIntegration> builder)
    {
        builder.ToTable("DeliveryIntegrations");

        builder.HasKey(integration => integration.Id);

        builder.Property(integration => integration.RestaurantId).IsRequired();
        builder.Property(integration => integration.Provider)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(integration => integration.WebhookSecretHash).HasMaxLength(128).IsRequired();
        builder.Property(integration => integration.Enabled).IsRequired();
        builder.Property(integration => integration.CreatedAtUtc).IsRequired();

        builder.HasOne<Restaurant>()
            .WithMany()
            .HasForeignKey(integration => integration.RestaurantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(integration => new { integration.RestaurantId, integration.Provider })
            .IsUnique();
        builder.HasIndex(integration => integration.WebhookSecretHash);
    }
}
