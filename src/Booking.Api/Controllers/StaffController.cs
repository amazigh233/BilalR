using System.Net.Mail;
using Booking.Api.Contracts.Staff;
using Booking.Infrastructure.Identity;
using Booking.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Booking.Api.Controllers;

[Route("api/admin/staff")]
public sealed class StaffController(
    BookingDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager) : ApiControllerBase
{
    [HttpGet]
    [Authorize(Policy = "RestaurantOwner")]
    [ProducesResponseType(typeof(IReadOnlyCollection<StaffUserApiResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyCollection<StaffUserApiResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentRestaurantId(out var restaurantId))
        {
            return Forbid();
        }

        var staffUsers = await GetStaffForRestaurantAsync(restaurantId);
        cancellationToken.ThrowIfCancellationRequested();

        return Ok(staffUsers
            .OrderBy(user => user.DisplayName)
            .ThenBy(user => user.Email)
            .Select(ToApiResponse)
            .ToList());
    }

    [HttpPost]
    [Authorize(Policy = "RestaurantOwner")]
    [ProducesResponseType(typeof(StaffUserApiResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StaffUserApiResponse>> Create(
        [FromBody] CreateStaffApiRequest? request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentRestaurantId(out var restaurantId))
        {
            return Forbid();
        }

        var validationError = Validate(request);
        if (validationError is not null)
        {
            return BadRequest(ToError(validationError));
        }

        var email = request!.Email.Trim();
        if (await userManager.FindByEmailAsync(email) is not null)
        {
            return Conflict(ToError("E-mail bestaat al."));
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var staff = new ApplicationUser
        {
            Email = email,
            UserName = email,
            EmailConfirmed = true,
            DisplayName = request.Name.Trim(),
            PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber)
                ? null
                : request.PhoneNumber.Trim(),
            RestaurantId = restaurantId
        };

        var createResult = await userManager.CreateAsync(staff, request.TemporaryPassword);
        if (!createResult.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);
            return BadRequest(ToError(ToIdentityErrorMessage(createResult)));
        }

        var ensureStaffRoleResult = await EnsureRoleAsync(BookingRoles.Staff);
        if (!ensureStaffRoleResult.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);
            return BadRequest(ToError(ToIdentityErrorMessage(ensureStaffRoleResult)));
        }

        var addRoleResult = await userManager.AddToRoleAsync(staff, BookingRoles.Staff);
        if (!addRoleResult.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);
            return BadRequest(ToError(ToIdentityErrorMessage(addRoleResult)));
        }

        await transaction.CommitAsync(cancellationToken);

        return CreatedAtAction(
            nameof(GetAll),
            ToApiResponse(staff));
    }

    [HttpPatch("{userId:guid}/disable")]
    [Authorize(Policy = "RestaurantOwner")]
    [ProducesResponseType(typeof(StaffUserApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StaffUserApiResponse>> Disable(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await SetActiveAsync(userId, isActive: false, cancellationToken);
    }

    [HttpPatch("{userId:guid}/enable")]
    [Authorize(Policy = "RestaurantOwner")]
    [ProducesResponseType(typeof(StaffUserApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StaffUserApiResponse>> Enable(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await SetActiveAsync(userId, isActive: true, cancellationToken);
    }

    private async Task<ActionResult<StaffUserApiResponse>> SetActiveAsync(
        Guid userId,
        bool isActive,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentRestaurantId(out var restaurantId))
        {
            return Forbid();
        }

        var staff = await userManager.FindByIdAsync(userId.ToString());
        if (staff is null ||
            staff.RestaurantId != restaurantId ||
            !await userManager.IsInRoleAsync(staff, BookingRoles.Staff))
        {
            return NotFound(ToError("Medewerker is niet gevonden."));
        }

        staff.LockoutEnabled = true;
        staff.LockoutEnd = isActive
            ? null
            : DateTimeOffset.MaxValue;

        var updateResult = await userManager.UpdateAsync(staff);
        cancellationToken.ThrowIfCancellationRequested();

        if (!updateResult.Succeeded)
        {
            return BadRequest(ToError(ToIdentityErrorMessage(updateResult)));
        }

        return Ok(ToApiResponse(staff));
    }

    private async Task<IReadOnlyCollection<ApplicationUser>> GetStaffForRestaurantAsync(Guid restaurantId)
    {
        var staffUsers = await userManager.GetUsersInRoleAsync(BookingRoles.Staff);

        return staffUsers
            .Where(user => user.RestaurantId == restaurantId)
            .ToList();
    }

    private async Task<IdentityResult> EnsureRoleAsync(string role)
    {
        if (await roleManager.RoleExistsAsync(role))
        {
            return IdentityResult.Success;
        }

        return await roleManager.CreateAsync(new IdentityRole<Guid>(role));
    }

    private static StaffUserApiResponse ToApiResponse(ApplicationUser staff)
    {
        return new StaffUserApiResponse(
            staff.Id,
            staff.DisplayName,
            staff.Email ?? string.Empty,
            staff.PhoneNumber,
            BookingRoles.Staff,
            staff.RestaurantId,
            IsActive(staff));
    }

    private static bool IsActive(ApplicationUser staff)
    {
        return !staff.LockoutEnd.HasValue ||
            staff.LockoutEnd.Value <= DateTimeOffset.UtcNow;
    }

    private static string? Validate(CreateStaffApiRequest? request)
    {
        if (request is null)
        {
            return "Request body is required.";
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return "Naam is verplicht.";
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return "E-mail is verplicht.";
        }

        if (!MailAddress.TryCreate(request.Email.Trim(), out _))
        {
            return "Gebruik een geldig e-mailadres.";
        }

        if (string.IsNullOrWhiteSpace(request.TemporaryPassword))
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
            ? "Medewerker kon niet worden aangemaakt."
            : string.Join(" ", errors);
    }
}
