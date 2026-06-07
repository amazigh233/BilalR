using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Booking.Api.Contracts.Scheduling;
using Booking.Api.Tests.Support;
using Booking.Domain.Scheduling;
using Booking.Infrastructure.Identity;

namespace Booking.Api.Tests.Scheduling;

public sealed class LeaveControllerTests
{
    private static readonly DateOnly From = DateOnly.FromDateTime(DateTime.Now).AddDays(7);
    private static readonly DateOnly To = From.AddDays(3);

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    [Fact]
    public async Task ApproveFlow_StaffRequests_OwnerApproves_StaffSeesApproved()
    {
        await using var factory = new BookingApiFactory();
        await factory.ResetDatabaseAsync();
        var client = factory.CreateClient();

        var owner = await factory.SeedUserAsync(BookingRoles.Owner);
        var staff = await factory.SeedUserAsync(BookingRoles.Staff, restaurantId: owner.Restaurant.Id);

        // Staff requests leave.
        await AuthenticateAsync(client, factory, staff.User.Email);
        var requestResponse = await client.PostAsJsonAsync(
            "api/staff/me/leave",
            new CreateLeaveApiRequest(From, To, "Vakantie"));
        Assert.Equal(HttpStatusCode.Created, requestResponse.StatusCode);
        var created = await requestResponse.Content.ReadFromJsonAsync<LeaveRequestApiResponse>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal(LeaveStatus.Pending, created!.Status);

        // Owner sees the pending request and approves it.
        await AuthenticateAsync(client, factory, owner.User.Email);
        var list = await client.GetFromJsonAsync<IReadOnlyCollection<LeaveRequestApiResponse>>(
            "api/admin/restaurant/leave", JsonOptions);
        var pending = Assert.Single(list!);
        Assert.Equal(LeaveStatus.Pending, pending.Status);

        var approveResponse = await client.PatchAsync(
            $"api/admin/restaurant/leave/{pending.Id}/approve", content: null);
        Assert.Equal(HttpStatusCode.OK, approveResponse.StatusCode);

        // Staff now sees it approved.
        await AuthenticateAsync(client, factory, staff.User.Email);
        var mine = await client.GetFromJsonAsync<IReadOnlyCollection<LeaveRequestApiResponse>>(
            "api/staff/me/leave", JsonOptions);
        var mineItem = Assert.Single(mine!);
        Assert.Equal(LeaveStatus.Approved, mineItem.Status);
    }

    [Fact]
    public async Task Owner_DoesNotSeeLeaveOfAnotherRestaurant()
    {
        await using var factory = new BookingApiFactory();
        await factory.ResetDatabaseAsync();
        var client = factory.CreateClient();

        var ownerA = await factory.SeedUserAsync(BookingRoles.Owner);
        var ownerB = await factory.SeedUserAsync(BookingRoles.Owner);
        var staffB = await factory.SeedUserAsync(BookingRoles.Staff, restaurantId: ownerB.Restaurant.Id);

        await AuthenticateAsync(client, factory, staffB.User.Email);
        var requestResponse = await client.PostAsJsonAsync(
            "api/staff/me/leave",
            new CreateLeaveApiRequest(From, To, null));
        Assert.Equal(HttpStatusCode.Created, requestResponse.StatusCode);

        await AuthenticateAsync(client, factory, ownerA.User.Email);
        var list = await client.GetFromJsonAsync<IReadOnlyCollection<LeaveRequestApiResponse>>(
            "api/admin/restaurant/leave", JsonOptions);

        Assert.Empty(list!);
    }

    [Fact]
    public async Task Staff_CannotApproveLeave()
    {
        await using var factory = new BookingApiFactory();
        await factory.ResetDatabaseAsync();
        var client = factory.CreateClient();

        var staff = await factory.SeedUserAsync(BookingRoles.Staff);
        await AuthenticateAsync(client, factory, staff.User.Email);

        var response = await client.PatchAsync(
            $"api/admin/restaurant/leave/{Guid.NewGuid()}/approve", content: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static async Task AuthenticateAsync(HttpClient client, BookingApiFactory factory, string? email)
    {
        var token = await factory.LoginAsync(client, email ?? string.Empty);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}
