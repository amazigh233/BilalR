using Booking.Api.Contracts.Scheduling;
using Booking.Application.Scheduling;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Booking.Api.Controllers;

public sealed class StaffAvailabilityController(
    GetStaffAvailabilityUseCase getStaffAvailabilityUseCase,
    SetStaffAvailabilityUseCase setStaffAvailabilityUseCase,
    GetRestaurantAvailabilityUseCase getRestaurantAvailabilityUseCase) : ApiControllerBase
{
    [HttpGet("/api/staff/me/availability")]
    [Authorize(Policy = "RestaurantUser")]
    [ProducesResponseType(typeof(IReadOnlyCollection<AvailabilitySlotApiResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<AvailabilitySlotApiResponse>>> GetMine(
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentRestaurantId(out var restaurantId) || !TryGetCurrentUserId(out var userId))
        {
            return Forbid();
        }

        var slots = await getStaffAvailabilityUseCase.ExecuteAsync(
            new GetStaffAvailabilityRequest(restaurantId, userId),
            cancellationToken);

        return Ok(slots.Select(ToApiResponse).ToList());
    }

    [HttpPut("/api/staff/me/availability")]
    [Authorize(Policy = "RestaurantUser")]
    [ProducesResponseType(typeof(IReadOnlyCollection<AvailabilitySlotApiResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyCollection<AvailabilitySlotApiResponse>>> SetMine(
        [FromBody] SetAvailabilityApiRequest? request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentRestaurantId(out var restaurantId) || !TryGetCurrentUserId(out var userId))
        {
            return Forbid();
        }

        if (request is null)
        {
            return BadRequest(ToError("Request body is required."));
        }

        var slots = (request.Slots ?? [])
            .Select(slot => new AvailabilitySlotInput(slot.DayOfWeek, slot.StartTime, slot.EndTime))
            .ToList();

        try
        {
            var result = await setStaffAvailabilityUseCase.ExecuteAsync(
                new SetStaffAvailabilityRequest(restaurantId, userId, slots),
                cancellationToken);

            return Ok(result.Select(ToApiResponse).ToList());
        }
        catch (Exception exception) when (exception is ArgumentException)
        {
            return HandleKnownException(exception);
        }
    }

    [HttpGet("/api/admin/restaurant/availability")]
    [Authorize(Policy = "RestaurantOwner")]
    [ProducesResponseType(typeof(IReadOnlyCollection<AvailabilitySlotApiResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<AvailabilitySlotApiResponse>>> GetForRestaurant(
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentRestaurantId(out var restaurantId))
        {
            return Forbid();
        }

        var slots = await getRestaurantAvailabilityUseCase.ExecuteAsync(
            new GetRestaurantAvailabilityRequest(restaurantId),
            cancellationToken);

        return Ok(slots.Select(ToApiResponse).ToList());
    }

    private static AvailabilitySlotApiResponse ToApiResponse(AvailabilitySlotResponse response)
    {
        return new AvailabilitySlotApiResponse(
            response.StaffUserId,
            response.DayOfWeek,
            response.StartTime,
            response.EndTime);
    }
}
