using Booking.Application.Abstractions;
using Booking.Domain.Customers;
using Booking.Domain.Notifications;
using Booking.Domain.Reservations;
using Booking.Domain.Restaurants;
using Booking.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Booking.Infrastructure.Persistence;

public sealed class BookingDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    private readonly ITenantProvider? _tenantProvider;

    public BookingDbContext(
        DbContextOptions<BookingDbContext> options,
        ITenantProvider? tenantProvider = null)
        : base(options)
    {
        _tenantProvider = tenantProvider;
    }

    public DbSet<Restaurant> Restaurants => Set<Restaurant>();

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Reservation> Reservations => Set<Reservation>();

    public DbSet<OpeningHour> OpeningHours => Set<OpeningHour>();

    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BookingDbContext).Assembly);

        // Tenant isolation safety net: even if a query forgets to filter on RestaurantId,
        // tenant-private data is never returned for a different restaurant. Deny-by-default
        // when there is no authenticated tenant (CurrentRestaurantId == null).
        // Public/anonymous read paths that intentionally cross tenants must opt out with
        // IgnoreQueryFilters(). Opening hours are public data and are deliberately not filtered
        // (they are loaded on the anonymous public booking page).
        modelBuilder.Entity<Reservation>()
            .HasQueryFilter(reservation => reservation.RestaurantId == CurrentTenantId);
        modelBuilder.Entity<NotificationLog>()
            .HasQueryFilter(notificationLog => notificationLog.RestaurantId == CurrentTenantId);
    }

    private Guid? CurrentTenantId => _tenantProvider?.CurrentRestaurantId;
}
