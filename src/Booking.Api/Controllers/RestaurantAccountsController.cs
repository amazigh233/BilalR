using System.Net.Mail;
using Booking.Api.Contracts.RestaurantAccounts;
using Booking.Domain.Restaurants;
using Booking.Infrastructure.Identity;
using Booking.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Booking.Api.Controllers;

[Route("api/restaurant-accounts")]
public sealed class RestaurantAccountsController(
    BookingDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager) : ApiControllerBase
{
    [HttpPost]
    [Authorize(Policy = "SuperAdmin")]
    [ProducesResponseType(typeof(RestaurantAccountApiResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RestaurantAccountApiResponse>> Create(
        [FromBody] CreateRestaurantAccountApiRequest? request,
        CancellationToken cancellationToken)
    {
        var validationError = Validate(request);
        if (validationError is not null)
        {
            return BadRequest(ToError(validationError));
        }

        var restaurantName = request!.RestaurantName.Trim();
        var ownerName = request.OwnerName.Trim();
        var ownerEmail = request.OwnerEmail.Trim();
        var ownerPassword = request.OwnerPassword;
        var phoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber)
            ? null
            : request.PhoneNumber.Trim();

        if (await userManager.FindByEmailAsync(ownerEmail) is not null)
        {
            return Conflict(ToError("E-mail bestaat al."));
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var restaurant = new Restaurant(
            restaurantName,
            phoneNumber,
            ownerEmail);

        dbContext.Restaurants.Add(restaurant);
        await dbContext.SaveChangesAsync(cancellationToken);

        var owner = new ApplicationUser
        {
            Email = ownerEmail,
            UserName = ownerEmail,
            EmailConfirmed = true,
            DisplayName = ownerName,
            RestaurantId = restaurant.Id
        };

        var createUserResult = await userManager.CreateAsync(owner, ownerPassword);
        if (!createUserResult.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);
            return BadRequest(ToError(ToIdentityErrorMessage(createUserResult)));
        }

        var ensureOwnerRoleResult = await EnsureRoleAsync(BookingRoles.Owner);
        if (!ensureOwnerRoleResult.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);
            return BadRequest(ToError(ToIdentityErrorMessage(ensureOwnerRoleResult)));
        }

        var addRoleResult = await userManager.AddToRoleAsync(owner, BookingRoles.Owner);
        if (!addRoleResult.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);
            return BadRequest(ToError(ToIdentityErrorMessage(addRoleResult)));
        }

        await transaction.CommitAsync(cancellationToken);

        var response = new RestaurantAccountApiResponse(
            restaurant.Id,
            owner.Id,
            owner.Email ?? ownerEmail);

        return CreatedAtAction(
            nameof(RestaurantsController.GetById),
            "Restaurants",
            new { restaurantId = restaurant.Id },
            response);
    }

    private async Task<IdentityResult> EnsureRoleAsync(string role)
    {
        if (await roleManager.RoleExistsAsync(role))
        {
            return IdentityResult.Success;
        }

        return await roleManager.CreateAsync(new IdentityRole<Guid>(role));
    }

    private static string? Validate(CreateRestaurantAccountApiRequest? request)
    {
        if (request is null)
        {
            return "Request body is required.";
        }

        if (string.IsNullOrWhiteSpace(request.RestaurantName))
        {
            return "Restaurantnaam is verplicht.";
        }

        if (string.IsNullOrWhiteSpace(request.OwnerName))
        {
            return "Naam van de eigenaar is verplicht.";
        }

        if (string.IsNullOrWhiteSpace(request.OwnerEmail))
        {
            return "E-mailadres van de eigenaar is verplicht.";
        }

        if (!MailAddress.TryCreate(request.OwnerEmail.Trim(), out _))
        {
            return "Gebruik een geldig e-mailadres.";
        }

        if (string.IsNullOrWhiteSpace(request.OwnerPassword))
        {
            return "Tijdelijk wachtwoord is verplicht.";
        }

        return null;
    }

    private static string ToIdentityErrorMessage(IdentityResult result)
    {
        var errors = result.Errors
            .Select(error => error.Description)
            .Where(description => !string.IsNullOrWhiteSpace(description))
            .ToList();

        return errors.Count == 0
            ? "Account kon niet worden aangemaakt."
            : string.Join(" ", errors);
    }
}
