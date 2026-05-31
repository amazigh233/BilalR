using System.Security.Cryptography;
using Booking.Domain.Restaurants;
using Booking.Infrastructure.Identity;
using Booking.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Booking.Api.Identity;

public static class DevelopmentIdentitySeeder
{
    public static async Task SeedDevelopmentIdentityAsync(
        this IServiceProvider services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var options = DevSeedOptions.FromConfiguration(configuration);
        var shouldSeed = environment.IsDevelopment() || options.Enabled;
        if (!shouldSeed)
        {
            return;
        }

        using var scope = services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        var logger = scopedServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("DevelopmentIdentitySeeder");
        var dbContext = scopedServices.GetRequiredService<BookingDbContext>();
        var roleManager = scopedServices.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = scopedServices.GetRequiredService<UserManager<ApplicationUser>>();

        await EnsureRolesAsync(roleManager);

        var platformAdminRestaurant = await EnsureRestaurantAsync(
            dbContext,
            options.PlatformAdminRestaurantName,
            "+31 20 000 0099",
            options.SuperAdminEmail,
            environment);

        var primaryRestaurant = await EnsureRestaurantAsync(
            dbContext,
            options.PrimaryRestaurantName,
            "+31 20 000 0000",
            options.OwnerEmail,
            environment);

        var secondaryRestaurant = await EnsureRestaurantAsync(
            dbContext,
            options.SecondaryRestaurantName,
            "+31 20 000 0001",
            options.SecondOwnerEmail,
            environment);

        var superAdminPassword = ResolvePassword(
            options.SuperAdminPassword,
            environment,
            options.SuperAdminEmail);
        var ownerPassword = ResolvePassword(options.OwnerPassword, environment, options.OwnerEmail);
        var staffPassword = ResolvePassword(options.StaffPassword, environment, options.StaffEmail);
        var secondOwnerPassword = ResolvePassword(options.SecondOwnerPassword, environment, options.SecondOwnerEmail);

        await EnsureUserAsync(
            userManager,
            options.SuperAdminEmail,
            "Demo SuperAdmin",
            platformAdminRestaurant.Id,
            BookingRoles.SuperAdmin,
            superAdminPassword,
            logger);

        await EnsureUserAsync(
            userManager,
            options.OwnerEmail,
            "Demo Owner",
            primaryRestaurant.Id,
            BookingRoles.Owner,
            ownerPassword,
            logger);

        await EnsureUserAsync(
            userManager,
            options.StaffEmail,
            "Demo Staff",
            primaryRestaurant.Id,
            BookingRoles.Staff,
            staffPassword,
            logger);

        await EnsureUserAsync(
            userManager,
            options.SecondOwnerEmail,
            "Demo Owner B",
            secondaryRestaurant.Id,
            BookingRoles.Owner,
            secondOwnerPassword,
            logger);
    }

    private static async Task EnsureRolesAsync(RoleManager<IdentityRole<Guid>> roleManager)
    {
        foreach (var role in BookingRoles.All)
        {
            if (await roleManager.RoleExistsAsync(role))
            {
                continue;
            }

            var result = await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            ThrowIfFailed(result, $"Could not create role '{role}'.");
        }
    }

    private static async Task<Restaurant> EnsureRestaurantAsync(
        BookingDbContext dbContext,
        string name,
        string phoneNumber,
        string email,
        IHostEnvironment environment)
    {
        var restaurant = await dbContext.Restaurants
            .FirstOrDefaultAsync(existing => existing.Name == name);

        if (restaurant is not null)
        {
            return restaurant;
        }

        restaurant = new Restaurant(
            name,
            phoneNumber,
            environment.IsDevelopment() ? email : null);

        dbContext.Restaurants.Add(restaurant);
        await dbContext.SaveChangesAsync();

        return restaurant;
    }

    private static async Task EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string displayName,
        Guid restaurantId,
        string role,
        string password,
        ILogger logger)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                Email = email,
                UserName = email,
                EmailConfirmed = true,
                DisplayName = displayName,
                RestaurantId = restaurantId
            };

            var createResult = await userManager.CreateAsync(user);
            ThrowIfFailed(createResult, $"Could not create user '{email}'.");
        }
        else
        {
            user.DisplayName = displayName;
            user.RestaurantId = restaurantId;
            user.EmailConfirmed = true;

            var updateResult = await userManager.UpdateAsync(user);
            ThrowIfFailed(updateResult, $"Could not update user '{email}'.");
        }

        if (await userManager.HasPasswordAsync(user))
        {
            var removePasswordResult = await userManager.RemovePasswordAsync(user);
            ThrowIfFailed(removePasswordResult, $"Could not reset password for '{email}'.");
        }

        var addPasswordResult = await userManager.AddPasswordAsync(user, password);
        ThrowIfFailed(addPasswordResult, $"Could not set password for '{email}'.");

        if (!await userManager.IsInRoleAsync(user, role))
        {
            var roleResult = await userManager.AddToRoleAsync(user, role);
            ThrowIfFailed(roleResult, $"Could not add user '{email}' to role '{role}'.");
        }

        logger.LogWarning("Development login seeded: {Email} / {Password}", email, password);
    }

    private static string ResolvePassword(string? configuredPassword, IHostEnvironment environment, string email)
    {
        if (!string.IsNullOrWhiteSpace(configuredPassword))
        {
            return configuredPassword;
        }

        if (!environment.IsDevelopment())
        {
            throw new InvalidOperationException(
                $"A seed password is required for '{email}' outside Development.");
        }

        return $"Local!{Convert.ToHexString(RandomNumberGenerator.GetBytes(8))}aA1";
    }

    private static void ThrowIfFailed(IdentityResult result, string message)
    {
        if (result.Succeeded)
        {
            return;
        }

        var errors = string.Join(", ", result.Errors.Select(error => error.Description));
        throw new InvalidOperationException($"{message} {errors}");
    }

    private sealed class DevSeedOptions
    {
        public bool Enabled { get; init; }

        public string PrimaryRestaurantName { get; init; } = "Zambiq Bistro";

        public string PlatformAdminRestaurantName { get; init; } = "Zambiq Platform Admin";

        public string SecondaryRestaurantName { get; init; } = "Zambiq Test Restaurant B";

        public string SuperAdminEmail { get; init; } = "superadmin@zambiq.local";

        public string? SuperAdminPassword { get; init; }

        public string OwnerEmail { get; init; } = "owner@zambiq.local";

        public string? OwnerPassword { get; init; }

        public string StaffEmail { get; init; } = "staff@zambiq.local";

        public string? StaffPassword { get; init; }

        public string SecondOwnerEmail { get; init; } = "owner-b@zambiq.local";

        public string? SecondOwnerPassword { get; init; }

        public static DevSeedOptions FromConfiguration(IConfiguration configuration)
        {
            var section = configuration.GetSection("Authentication:DevSeed");

            return new DevSeedOptions
            {
                Enabled = section.GetValue<bool>("Enabled"),
                PlatformAdminRestaurantName = section["PlatformAdminRestaurantName"] ?? "Zambiq Platform Admin",
                PrimaryRestaurantName = section["PrimaryRestaurantName"] ?? "Zambiq Bistro",
                SecondaryRestaurantName = section["SecondaryRestaurantName"] ?? "Zambiq Test Restaurant B",
                SuperAdminEmail = section["SuperAdminEmail"] ?? "superadmin@zambiq.local",
                SuperAdminPassword = section["SuperAdminPassword"],
                OwnerEmail = section["OwnerEmail"] ?? "owner@zambiq.local",
                OwnerPassword = section["OwnerPassword"],
                StaffEmail = section["StaffEmail"] ?? "staff@zambiq.local",
                StaffPassword = section["StaffPassword"],
                SecondOwnerEmail = section["SecondOwnerEmail"] ?? "owner-b@zambiq.local",
                SecondOwnerPassword = section["SecondOwnerPassword"]
            };
        }
    }
}
