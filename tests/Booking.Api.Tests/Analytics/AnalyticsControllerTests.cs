using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Booking.Api.Tests.Support;
using Booking.Infrastructure.Identity;

namespace Booking.Api.Tests.Analytics;

public sealed class AnalyticsControllerTests
{
    [Fact]
    public async Task Get_ReturnsAnalyticsScopedToOwnRestaurant()
    {
        await using var factory = new BookingApiFactory();
        await factory.ResetDatabaseAsync();
        var client = factory.CreateClient();

        var ownerA = await factory.SeedUserAsync(BookingRoles.Owner);
        var ownerB = await factory.SeedUserAsync(BookingRoles.Owner);

        await factory.SeedReservationAsync(ownerA.Restaurant.Id, "a1@example.com");
        await factory.SeedReservationAsync(ownerA.Restaurant.Id, "a2@example.com");
        await factory.SeedReservationAsync(ownerB.Restaurant.Id, "b1@example.com");

        await AuthenticateAsync(client, factory, ownerA.User.Email);

        var today = DateOnly.FromDateTime(DateTime.Now);
        var from = today.AddDays(-1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var to = today.AddDays(2).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        var response = await client.GetAsync($"api/admin/restaurant/analytics?from={from}&to={to}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var analytics = await response.Content.ReadFromJsonAsync<AnalyticsResult>();

        Assert.NotNull(analytics);
        // Only restaurant A's two reservations are counted, not restaurant B's.
        Assert.Equal(2, analytics!.TotalReservations);
        Assert.Equal(4, analytics.TotalGuests); // SeedReservationAsync uses party size 2.
    }

    [Fact]
    public async Task Get_ReturnsForbiddenForStaffUser()
    {
        await using var factory = new BookingApiFactory();
        await factory.ResetDatabaseAsync();
        var client = factory.CreateClient();

        var staff = await factory.SeedUserAsync(BookingRoles.Staff);
        await AuthenticateAsync(client, factory, staff.User.Email);

        var response = await client.GetAsync("api/admin/restaurant/analytics");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static async Task AuthenticateAsync(HttpClient client, BookingApiFactory factory, string? email)
    {
        var token = await factory.LoginAsync(client, email ?? string.Empty);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private sealed record AnalyticsResult(int TotalReservations, int TotalGuests);
}
