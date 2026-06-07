using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Booking.Api.Contracts.Scheduling;
using Booking.Api.Tests.Support;
using Booking.Infrastructure.Identity;

namespace Booking.Api.Tests.Scheduling;

public sealed class ShiftsControllerTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.Now);

    [Fact]
    public async Task Owner_CanCreateShiftForOwnStaff_AndItAppears()
    {
        await using var factory = new BookingApiFactory();
        await factory.ResetDatabaseAsync();
        var client = factory.CreateClient();

        var owner = await factory.SeedUserAsync(BookingRoles.Owner);
        var staff = await factory.SeedUserAsync(BookingRoles.Staff, restaurantId: owner.Restaurant.Id);
        await AuthenticateAsync(client, factory, owner.User.Email);

        var createResponse = await client.PostAsJsonAsync(
            "api/admin/restaurant/shifts",
            new CreateShiftApiRequest(staff.User.Id, Today, new TimeOnly(17, 0), new TimeOnly(22, 0), "Avond"));

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var shifts = await client.GetFromJsonAsync<IReadOnlyCollection<ShiftApiResponse>>(
            $"api/admin/restaurant/shifts?from={Today.AddDays(-1):yyyy-MM-dd}&to={Today.AddDays(1):yyyy-MM-dd}");

        Assert.NotNull(shifts);
        var shift = Assert.Single(shifts!);
        Assert.Equal(staff.User.Id, shift.StaffUserId);
    }

    [Fact]
    public async Task Staff_CannotCreateShift()
    {
        await using var factory = new BookingApiFactory();
        await factory.ResetDatabaseAsync();
        var client = factory.CreateClient();

        var staff = await factory.SeedUserAsync(BookingRoles.Staff);
        await AuthenticateAsync(client, factory, staff.User.Email);

        var response = await client.PostAsJsonAsync(
            "api/admin/restaurant/shifts",
            new CreateShiftApiRequest(staff.User.Id, Today, new TimeOnly(17, 0), new TimeOnly(22, 0), null));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Owner_CannotCreateShiftForStaffOfAnotherRestaurant()
    {
        await using var factory = new BookingApiFactory();
        await factory.ResetDatabaseAsync();
        var client = factory.CreateClient();

        var ownerA = await factory.SeedUserAsync(BookingRoles.Owner);
        var ownerB = await factory.SeedUserAsync(BookingRoles.Owner);
        var staffB = await factory.SeedUserAsync(BookingRoles.Staff, restaurantId: ownerB.Restaurant.Id);
        await AuthenticateAsync(client, factory, ownerA.User.Email);

        var response = await client.PostAsJsonAsync(
            "api/admin/restaurant/shifts",
            new CreateShiftApiRequest(staffB.User.Id, Today, new TimeOnly(17, 0), new TimeOnly(22, 0), null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task StaffMeShifts_ReturnsOnlyOwnShifts()
    {
        await using var factory = new BookingApiFactory();
        await factory.ResetDatabaseAsync();
        var client = factory.CreateClient();

        var owner = await factory.SeedUserAsync(BookingRoles.Owner);
        var staffOne = await factory.SeedUserAsync(BookingRoles.Staff, restaurantId: owner.Restaurant.Id);
        var staffTwo = await factory.SeedUserAsync(BookingRoles.Staff, restaurantId: owner.Restaurant.Id);

        await AuthenticateAsync(client, factory, owner.User.Email);
        var created = await client.PostAsJsonAsync(
            "api/admin/restaurant/shifts",
            new CreateShiftApiRequest(staffOne.User.Id, Today, new TimeOnly(17, 0), new TimeOnly(22, 0), null));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var range = $"from={Today.AddDays(-1):yyyy-MM-dd}&to={Today.AddDays(1):yyyy-MM-dd}";

        // staffOne sees the shift.
        await AuthenticateAsync(client, factory, staffOne.User.Email);
        var mineOne = await client.GetFromJsonAsync<IReadOnlyCollection<ShiftApiResponse>>($"api/staff/me/shifts?{range}");
        Assert.Single(mineOne!);

        // staffTwo sees none.
        await AuthenticateAsync(client, factory, staffTwo.User.Email);
        var mineTwo = await client.GetFromJsonAsync<IReadOnlyCollection<ShiftApiResponse>>($"api/staff/me/shifts?{range}");
        Assert.Empty(mineTwo!);
    }

    private static async Task AuthenticateAsync(HttpClient client, BookingApiFactory factory, string? email)
    {
        var token = await factory.LoginAsync(client, email ?? string.Empty);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}
