using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Booking.Api.Contracts.Scheduling;
using Booking.Api.Tests.Support;
using Booking.Infrastructure.Identity;

namespace Booking.Api.Tests.Scheduling;

public sealed class StaffAvailabilityControllerTests
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    [Fact]
    public async Task Staff_SetsAvailability_AndOwnerSeesIt()
    {
        await using var factory = new BookingApiFactory();
        await factory.ResetDatabaseAsync();
        var client = factory.CreateClient();

        var owner = await factory.SeedUserAsync(BookingRoles.Owner);
        var staff = await factory.SeedUserAsync(BookingRoles.Staff, restaurantId: owner.Restaurant.Id);

        await AuthenticateAsync(client, factory, staff.User.Email);
        var setResponse = await client.PutAsJsonAsync(
            "api/staff/me/availability",
            new SetAvailabilityApiRequest(
            [
                new AvailabilitySlotApiItem(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(17, 0)),
                new AvailabilitySlotApiItem(DayOfWeek.Wednesday, new TimeOnly(12, 0), new TimeOnly(20, 0))
            ]));
        Assert.Equal(HttpStatusCode.OK, setResponse.StatusCode);

        var mine = await client.GetFromJsonAsync<IReadOnlyCollection<AvailabilitySlotApiResponse>>(
            "api/staff/me/availability", JsonOptions);
        Assert.Equal(2, mine!.Count);

        await AuthenticateAsync(client, factory, owner.User.Email);
        var team = await client.GetFromJsonAsync<IReadOnlyCollection<AvailabilitySlotApiResponse>>(
            "api/admin/restaurant/availability", JsonOptions);
        Assert.Equal(2, team!.Count);
        Assert.All(team!, slot => Assert.Equal(staff.User.Id, slot.StaffUserId));
    }

    [Fact]
    public async Task SetAvailability_ReplacesPreviousSet()
    {
        await using var factory = new BookingApiFactory();
        await factory.ResetDatabaseAsync();
        var client = factory.CreateClient();

        var staff = await factory.SeedUserAsync(BookingRoles.Staff);
        await AuthenticateAsync(client, factory, staff.User.Email);

        await client.PutAsJsonAsync("api/staff/me/availability", new SetAvailabilityApiRequest(
            [new AvailabilitySlotApiItem(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(17, 0))]));
        await client.PutAsJsonAsync("api/staff/me/availability", new SetAvailabilityApiRequest(
            [new AvailabilitySlotApiItem(DayOfWeek.Friday, new TimeOnly(18, 0), new TimeOnly(23, 0))]));

        var mine = await client.GetFromJsonAsync<IReadOnlyCollection<AvailabilitySlotApiResponse>>(
            "api/staff/me/availability", JsonOptions);
        var slot = Assert.Single(mine!);
        Assert.Equal(DayOfWeek.Friday, slot.DayOfWeek);
    }

    [Fact]
    public async Task Owner_DoesNotSeeAvailabilityOfAnotherRestaurant()
    {
        await using var factory = new BookingApiFactory();
        await factory.ResetDatabaseAsync();
        var client = factory.CreateClient();

        var ownerA = await factory.SeedUserAsync(BookingRoles.Owner);
        var ownerB = await factory.SeedUserAsync(BookingRoles.Owner);
        var staffB = await factory.SeedUserAsync(BookingRoles.Staff, restaurantId: ownerB.Restaurant.Id);

        await AuthenticateAsync(client, factory, staffB.User.Email);
        await client.PutAsJsonAsync("api/staff/me/availability", new SetAvailabilityApiRequest(
            [new AvailabilitySlotApiItem(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(17, 0))]));

        await AuthenticateAsync(client, factory, ownerA.User.Email);
        var team = await client.GetFromJsonAsync<IReadOnlyCollection<AvailabilitySlotApiResponse>>(
            "api/admin/restaurant/availability", JsonOptions);

        Assert.Empty(team!);
    }

    private static async Task AuthenticateAsync(HttpClient client, BookingApiFactory factory, string? email)
    {
        var token = await factory.LoginAsync(client, email ?? string.Empty);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}
