using Booking.Application.Abstractions;
using Booking.Application.Availability;
using Booking.Infrastructure.Persistence;
using Booking.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Booking.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("BookingDatabase");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'BookingDatabase' is missing.");
        }

        services.AddDbContext<BookingDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IRestaurantRepository, RestaurantRepository>();
        services.AddScoped<IReservationRepository, ReservationRepository>();
        services.AddScoped<IAvailabilityService, AvailabilityService>();

        return services;
    }
}
