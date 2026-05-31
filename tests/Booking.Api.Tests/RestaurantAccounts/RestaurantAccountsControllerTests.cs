using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Booking.Api.Contracts.Auth;
using Booking.Api.Contracts.RestaurantAccounts;
using Booking.Domain.Restaurants;
using Booking.Infrastructure.Identity;
using Booking.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Booking.Api.Tests.RestaurantAccounts;

public sealed class RestaurantAccountsControllerTests
{
    [Fact]
    public async Task Create_CreatesRestaurantOwnerAndOwnerRole()
    {
        await using var factory = new BookingApiFactory();
        await factory.ResetDatabaseAsync();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            await factory.SeedUserAndLoginAsync(client, BookingRoles.SuperAdmin));

        var response = await client.PostAsJsonAsync(
            "api/restaurant-accounts",
            new CreateRestaurantAccountApiRequest(
                "New Bistro",
                "New Owner",
                "new-owner@example.com",
                "Testing!123A",
                "+31 20 123 4567"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var account = await response.Content.ReadFromJsonAsync<RestaurantAccountApiResponse>();
        Assert.NotNull(account);
        Assert.Equal("new-owner@example.com", account.OwnerEmail);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var restaurant = await dbContext.Restaurants.FindAsync(account.RestaurantId);
        var owner = await userManager.FindByEmailAsync("new-owner@example.com");

        Assert.NotNull(restaurant);
        Assert.NotNull(owner);
        Assert.Equal(account.OwnerUserId, owner.Id);
        Assert.Equal(account.RestaurantId, owner.RestaurantId);
        Assert.True(await userManager.IsInRoleAsync(owner, BookingRoles.Owner));
    }

    [Fact]
    public async Task Create_ReturnsConflictWhenOwnerEmailAlreadyExists()
    {
        await using var factory = new BookingApiFactory();
        await factory.ResetDatabaseAsync();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            await factory.SeedUserAndLoginAsync(client, BookingRoles.SuperAdmin));

        var request = new CreateRestaurantAccountApiRequest(
            "Duplicate Bistro",
            "Duplicate Owner",
            "duplicate-owner@example.com",
            "Testing!123A",
            null);

        var firstResponse = await client.PostAsJsonAsync("api/restaurant-accounts", request);
        var secondResponse = await client.PostAsJsonAsync("api/restaurant-accounts", request);

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task Create_ReturnsBadRequestWhenPasswordDoesNotMeetIdentityRules()
    {
        await using var factory = new BookingApiFactory();
        await factory.ResetDatabaseAsync();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            await factory.SeedUserAndLoginAsync(client, BookingRoles.SuperAdmin));

        var response = await client.PostAsJsonAsync(
            "api/restaurant-accounts",
            new CreateRestaurantAccountApiRequest(
                "Weak Password Bistro",
                "Weak Owner",
                "weak-owner@example.com",
                "short",
                null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        Assert.Null(await userManager.FindByEmailAsync("weak-owner@example.com"));
        Assert.False(await dbContext.Restaurants.AnyAsync(restaurant => restaurant.Name == "Weak Password Bistro"));
    }

    [Fact]
    public async Task Create_RejectsAnonymousAndRestaurantOwnerUsers()
    {
        await using var factory = new BookingApiFactory();
        await factory.ResetDatabaseAsync();
        var client = factory.CreateClient();

        var request = new CreateRestaurantAccountApiRequest(
            "Unauthorized Bistro",
            "Unauthorized Owner",
            "unauthorized-owner@example.com",
            "Testing!123A",
            null);

        var anonymousResponse = await client.PostAsJsonAsync("api/restaurant-accounts", request);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            await factory.SeedUserAndLoginAsync(client, BookingRoles.Owner));
        var ownerResponse = await client.PostAsJsonAsync("api/restaurant-accounts", request);

        Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, ownerResponse.StatusCode);
    }

    private sealed class BookingApiFactory : WebApplicationFactory<Program>, IAsyncDisposable
    {
        private const string TestPassword = "Testing!123A";
        private const string JwtSigningKey = "Testing_Signing_Key_For_Restaurant_Account_Tests_1234567890";
        private readonly string? _previousConnectionString;
        private readonly string? _previousDevSeedEnabled;
        private readonly string? _previousJwtSigningKey;
        private readonly string _connectionString =
            $"Server=localhost,1434;Database=BookingApiTests_{Guid.NewGuid():N};User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;Encrypt=False;";

        public BookingApiFactory()
        {
            _previousConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__BookingDatabase");
            _previousDevSeedEnabled = Environment.GetEnvironmentVariable("Authentication__DevSeed__Enabled");
            _previousJwtSigningKey = Environment.GetEnvironmentVariable("Authentication__Jwt__SigningKey");

            Environment.SetEnvironmentVariable(
                "ConnectionStrings__BookingDatabase",
                _connectionString);
            Environment.SetEnvironmentVariable("Authentication__DevSeed__Enabled", "false");
            Environment.SetEnvironmentVariable("Authentication__Jwt__SigningKey", JwtSigningKey);
        }

        public async Task ResetDatabaseAsync()
        {
            using var scope = Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<BookingDbContext>();

            await dbContext.Database.EnsureDeletedAsync();
            await dbContext.Database.EnsureCreatedAsync();
        }

        public async Task<string> SeedUserAndLoginAsync(HttpClient client, string role)
        {
            var email = $"{role.ToLowerInvariant()}-{Guid.NewGuid():N}@example.com";

            using (var scope = Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

                if (!await roleManager.RoleExistsAsync(role))
                {
                    var roleResult = await roleManager.CreateAsync(new IdentityRole<Guid>(role));
                    Assert.True(roleResult.Succeeded);
                }

                var restaurant = new Restaurant($"{role} Test Restaurant");
                dbContext.Restaurants.Add(restaurant);
                await dbContext.SaveChangesAsync();

                var user = new ApplicationUser
                {
                    Email = email,
                    UserName = email,
                    EmailConfirmed = true,
                    DisplayName = $"{role} User",
                    RestaurantId = restaurant.Id
                };

                var createResult = await userManager.CreateAsync(user, TestPassword);
                Assert.True(createResult.Succeeded);

                var roleResultForUser = await userManager.AddToRoleAsync(user, role);
                Assert.True(roleResultForUser.Succeeded);
            }

            var loginResponse = await client.PostAsJsonAsync(
                "api/auth/login",
                new LoginApiRequest(email, TestPassword));

            Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
            var login = await loginResponse.Content.ReadFromJsonAsync<LoginApiResponse>();
            Assert.NotNull(login);

            return login.AccessToken;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:BookingDatabase"] = _connectionString,
                    ["Authentication:Jwt:SigningKey"] = JwtSigningKey,
                    ["Authentication:DevSeed:Enabled"] = "false"
                });
            });

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<DbContextOptions<BookingDbContext>>();
                services.RemoveAll<DbContextOptions>();
                services.RemoveAll<IDbContextOptionsConfiguration<BookingDbContext>>();
                services.AddDbContext<BookingDbContext>(options =>
                    options.UseSqlServer(_connectionString));
            });
        }

        public override async ValueTask DisposeAsync()
        {
            try
            {
                using var scope = Services.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
                await dbContext.Database.EnsureDeletedAsync();
            }
            catch
            {
                // Best effort cleanup for local SQL Server test databases.
            }

            Environment.SetEnvironmentVariable("ConnectionStrings__BookingDatabase", _previousConnectionString);
            Environment.SetEnvironmentVariable("Authentication__DevSeed__Enabled", _previousDevSeedEnabled);
            Environment.SetEnvironmentVariable("Authentication__Jwt__SigningKey", _previousJwtSigningKey);
            await base.DisposeAsync();
        }
    }
}
