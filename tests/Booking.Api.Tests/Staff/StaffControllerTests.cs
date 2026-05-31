using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Booking.Api.Contracts.Staff;
using Booking.Api.Tests.Support;
using Booking.Infrastructure.Identity;
using Booking.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Booking.Api.Tests.Staff;

public sealed class StaffControllerTests
{
    [Fact]
    public async Task Create_CreatesStaffForOwnersRestaurant()
    {
        await using var factory = new BookingApiFactory();
        await factory.ResetDatabaseAsync();
        var client = factory.CreateClient();
        var owner = await factory.SeedUserAsync(BookingRoles.Owner);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            await factory.LoginAsync(client, owner.User.Email ?? string.Empty));

        var response = await client.PostAsJsonAsync(
            "api/admin/staff",
            new CreateStaffApiRequest(
                "New Staff",
                "new-staff@example.com",
                BookingApiFactory.TestPassword,
                "+31 20 000 1000"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var staffResponse = await response.Content.ReadFromJsonAsync<StaffUserApiResponse>();
        Assert.NotNull(staffResponse);
        Assert.Equal(owner.Restaurant.Id, staffResponse.RestaurantId);
        Assert.Equal(BookingRoles.Staff, staffResponse.Role);
        Assert.True(staffResponse.IsActive);

        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var staff = await userManager.FindByEmailAsync("new-staff@example.com");

        Assert.NotNull(staff);
        Assert.Equal(owner.Restaurant.Id, staff.RestaurantId);
        Assert.True(await userManager.IsInRoleAsync(staff, BookingRoles.Staff));
    }

    [Fact]
    public async Task Create_ReturnsForbiddenForStaffUser()
    {
        await using var factory = new BookingApiFactory();
        await factory.ResetDatabaseAsync();
        var client = factory.CreateClient();
        var staff = await factory.SeedUserAsync(BookingRoles.Staff);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            await factory.LoginAsync(client, staff.User.Email ?? string.Empty));

        var response = await client.PostAsJsonAsync(
            "api/admin/staff",
            new CreateStaffApiRequest(
                "Other Staff",
                "other-staff@example.com",
                BookingApiFactory.TestPassword,
                null));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_ReturnsOnlyStaffForOwnersRestaurant()
    {
        await using var factory = new BookingApiFactory();
        await factory.ResetDatabaseAsync();
        var client = factory.CreateClient();
        var ownerA = await factory.SeedUserAsync(BookingRoles.Owner);
        var staffA = await factory.SeedUserAsync(
            BookingRoles.Staff,
            "staff-a@example.com",
            ownerA.Restaurant.Id);
        await factory.SeedUserAsync(BookingRoles.Staff, "staff-b@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            await factory.LoginAsync(client, ownerA.User.Email ?? string.Empty));

        var response = await client.GetAsync("api/admin/staff");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var staff = await response.Content.ReadFromJsonAsync<IReadOnlyCollection<StaffUserApiResponse>>();

        Assert.NotNull(staff);
        var onlyStaff = Assert.Single(staff);
        Assert.Equal(staffA.User.Id, onlyStaff.UserId);
        Assert.Equal(ownerA.Restaurant.Id, onlyStaff.RestaurantId);
    }

    [Fact]
    public async Task Create_ReturnsConflictWhenEmailAlreadyExists()
    {
        await using var factory = new BookingApiFactory();
        await factory.ResetDatabaseAsync();
        var client = factory.CreateClient();
        var owner = await factory.SeedUserAsync(BookingRoles.Owner);
        await factory.SeedUserAsync(BookingRoles.Staff, "duplicate-staff@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            await factory.LoginAsync(client, owner.User.Email ?? string.Empty));

        var response = await client.PostAsJsonAsync(
            "api/admin/staff",
            new CreateStaffApiRequest(
                "Duplicate Staff",
                "duplicate-staff@example.com",
                BookingApiFactory.TestPassword,
                null));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Disable_ReturnsNotFoundForStaffFromAnotherRestaurant()
    {
        await using var factory = new BookingApiFactory();
        await factory.ResetDatabaseAsync();
        var client = factory.CreateClient();
        var ownerA = await factory.SeedUserAsync(BookingRoles.Owner);
        var staffB = await factory.SeedUserAsync(BookingRoles.Staff, "staff-b-disable@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            await factory.LoginAsync(client, ownerA.User.Email ?? string.Empty));

        var response = await client.PatchAsync(
            $"api/admin/staff/{staffB.User.Id}/disable",
            null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        var unchangedStaff = await dbContext.Users.SingleAsync(user => user.Id == staffB.User.Id);
        Assert.Null(unchangedStaff.LockoutEnd);
    }

    [Fact]
    public async Task Disable_BlocksStaffLoginAndEnableAllowsLoginAgain()
    {
        await using var factory = new BookingApiFactory();
        await factory.ResetDatabaseAsync();
        var client = factory.CreateClient();
        var owner = await factory.SeedUserAsync(BookingRoles.Owner);
        var staff = await factory.SeedUserAsync(
            BookingRoles.Staff,
            "staff-toggle@example.com",
            owner.Restaurant.Id);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            await factory.LoginAsync(client, owner.User.Email ?? string.Empty));

        var disableResponse = await client.PatchAsync(
            $"api/admin/staff/{staff.User.Id}/disable",
            null);
        client.DefaultRequestHeaders.Authorization = null;
        var disabledLoginResponse = await client.PostAsJsonAsync(
            "api/auth/login",
            new { email = "staff-toggle@example.com", password = BookingApiFactory.TestPassword });

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            await factory.LoginAsync(client, owner.User.Email ?? string.Empty));
        var enableResponse = await client.PatchAsync(
            $"api/admin/staff/{staff.User.Id}/enable",
            null);
        client.DefaultRequestHeaders.Authorization = null;
        var enabledToken = await factory.LoginAsync(client, "staff-toggle@example.com");

        Assert.Equal(HttpStatusCode.OK, disableResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, disabledLoginResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, enableResponse.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(enabledToken));
    }
}
