using Booking.Domain.Restaurants;
using Booking.Domain.Tables;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace Booking.Infrastructure.Persistence.Configurations;

public sealed class TableConfiguration : IEntityTypeConfiguration<Table>
{
    public void Configure(EntityTypeBuilder<Table> builder)
    {
        builder.ToTable("Tables");

        builder.HasKey(table => table.Id);

        builder.Property(table => table.RestaurantId)
            .IsRequired();

        builder.Property(table => table.Name)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(table => table.Section)
            .HasMaxLength(50);

        builder.Property(table => table.Capacity)
            .IsRequired();

        builder.Property(table => table.IsActive)
            .IsRequired();

        builder.Property(table => table.PosX)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(table => table.PosY)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(table => table.Rotation)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(table => table.Shape)
            .HasDefaultValue(TableShape.Square)
            .IsRequired();

        builder.HasOne<Restaurant>()
            .WithMany()
            .HasForeignKey(table => table.RestaurantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(table => new { table.RestaurantId, table.IsActive });
    }
}
