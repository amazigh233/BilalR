using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Booking.Api.Contracts.Reservations;
using Booking.Api.Tests.Support;
using Booking.Infrastructure.Identity;
using Booking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Booking.Api.Tests.Tenancy;

public sealed class TenantIsolationTests
{
    [Fact]
    public async Task GetReservation_ForAnotherRestaurant_ReturnsNotFound()
    {
        await using var factory = new BookingApiFactory();
        await factory.ResetDatabaseAsync();
        var client = factory.CreateClient();

        var ownerA = await factory.SeedUserAsync(BookingRoles.Owner);
        var ownerB = await factory.SeedUserAsync(BookingRoles.Owner);

        // Reservation belongs to restaurant B.
        var reservationIdB = await factory.SeedReservationAsync(ownerB.Restaurant.Id);

        await AuthenticateAsync(client, factory, ownerA.User.Email);

        // Owner A reads by id; the global query filter (tenant = A) must hide B's reservation.
        var response = await client.GetAsync($"api/reservations/{reservationIdB}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ListReservations_ReturnsOnlyOwnRestaurant()
    {
        await using var factory = new BookingApiFactory();
        await factory.ResetDatabaseAsync();
        var client = factory.CreateClient();

        var ownerA = await factory.SeedUserAsync(BookingRoles.Owner);
        var ownerB = await factory.SeedUserAsync(BookingRoles.Owner);

        await factory.SeedReservationAsync(ownerA.Restaurant.Id, "a@example.com");
        await factory.SeedReservationAsync(ownerB.Restaurant.Id, "b@example.com");

        await AuthenticateAsync(client, factory, ownerA.User.Email);

        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() }
        };
        var reservations = await client.GetFromJsonAsync<IReadOnlyCollection<ReservationApiResponse>>(
            "api/admin/restaurant/reservations",
            jsonOptions);

        Assert.NotNull(reservations);
        var reservation = Assert.Single(reservations);
        Assert.Equal(ownerA.Restaurant.Id, reservation.RestaurantId);
    }

    [Fact]
    public async Task PublicBooking_Anonymous_StillWorks_AndLogsNotification()
    {
        await using var factory = new BookingApiFactory();
        await factory.ResetDatabaseAsync();
        var client = factory.CreateClient();

        var owner = await factory.SeedUserAsync(BookingRoles.Owner);
        var reservationDateTime = DateTime.Now.Date.AddDays(7).AddHours(18);
        await factory.SeedOpeningHoursAsync(
            owner.Restaurant.Id,
            reservationDateTime.DayOfWeek,
            new TimeOnly(17, 0),
            new TimeOnly(22, 0));

        // No Authorization header: anonymous public booking flow.
        var response = await client.PostAsJsonAsync(
            "api/reservations",
            new CreateReservationApiRequest(
                owner.Restaurant.Id,
                reservationDateTime,
                2,
                new CustomerApiRequest("Public Customer", "public@example.com", null)));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        // A notification log was created and marked as sent (LoggingEmailSender in tests).
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        var log = await dbContext.NotificationLogs
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(entry => entry.RecipientEmail == "public@example.com");

        Assert.NotNull(log);
        Assert.NotNull(log!.SentAtUtc);
    }

    private static async Task AuthenticateAsync(HttpClient client, BookingApiFactory factory, string? email)
    {
        var token = await factory.LoginAsync(client, email ?? string.Empty);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}
