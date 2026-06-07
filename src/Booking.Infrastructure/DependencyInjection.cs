using Booking.Application.Abstractions;
using Booking.Application.Availability;
using Booking.Infrastructure.Notifications;
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
        services.AddScoped<INotificationLogRepository, NotificationLogRepository>();
        services.AddScoped<IShiftRepository, ShiftRepository>();
        services.AddScoped<IStaffAvailabilityRepository, StaffAvailabilityRepository>();
        services.AddScoped<ILeaveRequestRepository, LeaveRequestRepository>();
        services.AddScoped<IDeliveryOrderRepository, DeliveryOrderRepository>();
        services.AddScoped<IDeliveryIntegrationRepository, DeliveryIntegrationRepository>();
        services.AddScoped<IAvailabilityService, AvailabilityService>();

        var emailOptions = new EmailOptions
        {
            SmtpHost = configuration["Email:SmtpHost"],
            SmtpPort = int.TryParse(configuration["Email:SmtpPort"], out var smtpPort) ? smtpPort : 587,
            SmtpUsername = configuration["Email:SmtpUsername"],
            SmtpPassword = configuration["Email:SmtpPassword"],
            UseSsl = !bool.TryParse(configuration["Email:UseSsl"], out var useSsl) || useSsl,
            FromAddress = configuration["Email:FromAddress"] ?? "no-reply@zambiq.local",
            FromName = configuration["Email:FromName"] ?? "Zambiq"
        };
        services.AddSingleton(emailOptions);

        // Real SMTP when configured; otherwise a logging-only fallback so dev/tests run without secrets.
        if (string.IsNullOrWhiteSpace(emailOptions.SmtpHost))
        {
            services.AddScoped<IEmailSender, LoggingEmailSender>();
        }
        else
        {
            services.AddScoped<IEmailSender, SmtpEmailSender>();
        }

        return services;
    }
}
