using System.Net;
using System.Net.Http.Json;
using Booking.Api.Contracts.Auth;
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

namespace Booking.Api.Tests.Support;

public sealed class BookingApiFactory : WebApplicationFactory<Program>, IAsyncDisposable
{
    public const string TestPassword = "Testing!123A";
    private const string JwtSigningKey = "Testing_Signing_Key_For_Booking_Api_Tests_1234567890";
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

        Environment.SetEnvironmentVariable("ConnectionStrings__BookingDatabase", _connectionString);
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

    public async Task<(ApplicationUser User, Restaurant Restaurant)> SeedUserAsync(
        string role,
        string? email = null,
        Guid? restaurantId = null)
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        if (!await roleManager.RoleExistsAsync(role))
        {
            var roleResult = await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            Assert.True(roleResult.Succeeded);
        }

        Restaurant restaurant;
        if (restaurantId.HasValue)
        {
            restaurant = await dbContext.Restaurants.FindAsync(restaurantId.Value)
                ?? throw new InvalidOperationException("Restaurant was not found.");
        }
        else
        {
            restaurant = new Restaurant($"{role} Test Restaurant {Guid.NewGuid():N}");
            dbContext.Restaurants.Add(restaurant);
            await dbContext.SaveChangesAsync();
        }

        email ??= $"{role.ToLowerInvariant()}-{Guid.NewGuid():N}@example.com";
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

        var addRoleResult = await userManager.AddToRoleAsync(user, role);
        Assert.True(addRoleResult.Succeeded);

        return (user, restaurant);
    }

    public async Task<string> LoginAsync(
        HttpClient client,
        string email,
        string password = TestPassword)
    {
        var loginResponse = await client.PostAsJsonAsync(
            "api/auth/login",
            new LoginApiRequest(email, password));

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginApiResponse>();
        Assert.NotNull(login);

        return login.AccessToken;
    }

    public async Task<string> SeedUserAndLoginAsync(HttpClient client, string role)
    {
        var seeded = await SeedUserAsync(role);

        return await LoginAsync(client, seeded.User.Email ?? string.Empty);
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
