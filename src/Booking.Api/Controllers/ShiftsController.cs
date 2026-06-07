using Booking.Api.Contracts.Scheduling;
using Booking.Application.Scheduling;
using Booking.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Booking.Api.Controllers;

public sealed class ShiftsController(
    CreateShiftUseCase createShiftUseCase,
    UpdateShiftUseCase updateShiftUseCase,
    DeleteShiftUseCase deleteShiftUseCase,
    GetShiftsUseCase getShiftsUseCase,
    GetStaffShiftsUseCase getStaffShiftsUseCase,
    UserManager<ApplicationUser> userManager) : ApiControllerBase
{
    [HttpGet("/api/admin/restaurant/shifts")]
    [Authorize(Policy = "RestaurantOwner")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ShiftApiResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<ShiftApiResponse>>> GetForRestaurant(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentRestaurantId(out var restaurantId))
        {
            return Forbid();
        }

        var (fromDate, toDate) = ResolveRange(from, to);

        try
        {
            var shifts = await getShiftsUseCase.ExecuteAsync(
                new GetShiftsRequest(restaurantId, fromDate, toDate),
                cancellationToken);

            return Ok(shifts.Select(ToApiResponse).ToList());
        }
        catch (Exception exception) when (exception is ArgumentException)
        {
            return HandleKnownException(exception);
        }
    }

    [HttpGet("/api/staff/me/shifts")]
    [Authorize(Policy = "RestaurantUser")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ShiftApiResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<ShiftApiResponse>>> GetMine(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentRestaurantId(out var restaurantId) || !TryGetCurrentUserId(out var userId))
        {
            return Forbid();
        }

        var (fromDate, toDate) = ResolveRange(from, to);

        try
        {
            var shifts = await getStaffShiftsUseCase.ExecuteAsync(
                new GetStaffShiftsRequest(restaurantId, userId, fromDate, toDate),
                cancellationToken);

            return Ok(shifts.Select(ToApiResponse).ToList());
        }
        catch (Exception exception) when (exception is ArgumentException)
        {
            return HandleKnownException(exception);
        }
    }

    [HttpPost("/api/admin/restaurant/shifts")]
    [Authorize(Policy = "RestaurantOwner")]
    [ProducesResponseType(typeof(ShiftApiResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ShiftApiResponse>> Create(
        [FromBody] CreateShiftApiRequest? request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentRestaurantId(out var restaurantId))
        {
            return Forbid();
        }

        if (request is null)
        {
            return BadRequest(ToError("Request body is required."));
        }

        if (!await IsMemberOfRestaurantAsync(request.StaffUserId, restaurantId))
        {
            return BadRequest(ToError("Medewerker hoort niet bij dit restaurant."));
        }

        try
        {
            var response = await createShiftUseCase.ExecuteAsync(
                new CreateShiftRequest(
                    restaurantId,
                    request.StaffUserId,
                    request.ShiftDate,
                    request.StartTime,
                    request.EndTime,
                    request.Note),
                cancellationToken);

            return CreatedAtAction(nameof(GetForRestaurant), ToApiResponse(response));
        }
        catch (Exception exception) when (exception is ArgumentException)
        {
            return HandleKnownException(exception);
        }
    }

    [HttpPut("/api/admin/restaurant/shifts/{shiftId:guid}")]
    [Authorize(Policy = "RestaurantOwner")]
    [ProducesResponseType(typeof(ShiftApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ShiftApiResponse>> Update(
        Guid shiftId,
        [FromBody] UpdateShiftApiRequest? request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentRestaurantId(out var restaurantId))
        {
            return Forbid();
        }

        if (request is null)
        {
            return BadRequest(ToError("Request body is required."));
        }

        if (!await IsMemberOfRestaurantAsync(request.StaffUserId, restaurantId))
        {
            return BadRequest(ToError("Medewerker hoort niet bij dit restaurant."));
        }

        try
        {
            var response = await updateShiftUseCase.ExecuteAsync(
                new UpdateShiftRequest(
                    restaurantId,
                    shiftId,
                    request.StaffUserId,
                    request.ShiftDate,
                    request.StartTime,
                    request.EndTime,
                    request.Note),
                cancellationToken);

            return Ok(ToApiResponse(response));
        }
        catch (Exception exception) when (exception is ArgumentException or KeyNotFoundException)
        {
            return HandleKnownException(exception);
        }
    }

    [HttpDelete("/api/admin/restaurant/shifts/{shiftId:guid}")]
    [Authorize(Policy = "RestaurantOwner")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid shiftId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentRestaurantId(out var restaurantId))
        {
            return Forbid();
        }

        try
        {
            await deleteShiftUseCase.ExecuteAsync(
                new DeleteShiftRequest(restaurantId, shiftId),
                cancellationToken);

            return NoContent();
        }
        catch (Exception exception) when (exception is KeyNotFoundException)
        {
            return HandleKnownException(exception);
        }
    }

    private async Task<bool> IsMemberOfRestaurantAsync(Guid userId, Guid restaurantId)
    {
        if (userId == Guid.Empty)
        {
            return false;
        }

        var user = await userManager.FindByIdAsync(userId.ToString());
        return user is not null && user.RestaurantId == restaurantId;
    }

    private static (DateOnly From, DateOnly To) ResolveRange(DateOnly? from, DateOnly? to)
    {
        if (from is not null && to is not null)
        {
            return (from.Value, to.Value);
        }

        // Default to the current week (Monday-Sunday).
        var today = DateOnly.FromDateTime(DateTime.Now);
        var diff = ((int)today.DayOfWeek + 6) % 7; // days since Monday
        var monday = today.AddDays(-diff);

        return (from ?? monday, to ?? monday.AddDays(6));
    }

    private static ShiftApiResponse ToApiResponse(ShiftResponse response)
    {
        return new ShiftApiResponse(
            response.Id,
            response.RestaurantId,
            response.StaffUserId,
            response.ShiftDate,
            response.StartTime,
            response.EndTime,
            response.Note);
    }
}
