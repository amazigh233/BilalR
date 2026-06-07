using Booking.Api.Contracts.Scheduling;
using Booking.Application.Scheduling;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Booking.Api.Controllers;

public sealed class LeaveController(
    RequestLeaveUseCase requestLeaveUseCase,
    GetStaffLeaveUseCase getStaffLeaveUseCase,
    GetRestaurantLeaveUseCase getRestaurantLeaveUseCase,
    DecideLeaveUseCase decideLeaveUseCase) : ApiControllerBase
{
    [HttpPost("/api/staff/me/leave")]
    [Authorize(Policy = "RestaurantUser")]
    [ProducesResponseType(typeof(LeaveRequestApiResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LeaveRequestApiResponse>> RequestLeave(
        [FromBody] CreateLeaveApiRequest? request,
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

        try
        {
            var response = await requestLeaveUseCase.ExecuteAsync(
                new RequestLeaveRequest(restaurantId, userId, request.FromDate, request.ToDate, request.Reason),
                cancellationToken);

            return CreatedAtAction(nameof(GetMine), ToApiResponse(response));
        }
        catch (Exception exception) when (exception is ArgumentException)
        {
            return HandleKnownException(exception);
        }
    }

    [HttpGet("/api/staff/me/leave")]
    [Authorize(Policy = "RestaurantUser")]
    [ProducesResponseType(typeof(IReadOnlyCollection<LeaveRequestApiResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<LeaveRequestApiResponse>>> GetMine(
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentRestaurantId(out var restaurantId) || !TryGetCurrentUserId(out var userId))
        {
            return Forbid();
        }

        var leave = await getStaffLeaveUseCase.ExecuteAsync(
            new GetStaffLeaveRequest(restaurantId, userId),
            cancellationToken);

        return Ok(leave.Select(ToApiResponse).ToList());
    }

    [HttpGet("/api/admin/restaurant/leave")]
    [Authorize(Policy = "RestaurantOwner")]
    [ProducesResponseType(typeof(IReadOnlyCollection<LeaveRequestApiResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<LeaveRequestApiResponse>>> GetForRestaurant(
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentRestaurantId(out var restaurantId))
        {
            return Forbid();
        }

        var leave = await getRestaurantLeaveUseCase.ExecuteAsync(
            new GetRestaurantLeaveRequest(restaurantId),
            cancellationToken);

        return Ok(leave.Select(ToApiResponse).ToList());
    }

    [HttpPatch("/api/admin/restaurant/leave/{leaveId:guid}/approve")]
    [Authorize(Policy = "RestaurantOwner")]
    [ProducesResponseType(typeof(LeaveRequestApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status404NotFound)]
    public Task<ActionResult<LeaveRequestApiResponse>> Approve(Guid leaveId, CancellationToken cancellationToken)
    {
        return DecideAsync(leaveId, approve: true, cancellationToken);
    }

    [HttpPatch("/api/admin/restaurant/leave/{leaveId:guid}/deny")]
    [Authorize(Policy = "RestaurantOwner")]
    [ProducesResponseType(typeof(LeaveRequestApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status404NotFound)]
    public Task<ActionResult<LeaveRequestApiResponse>> Deny(Guid leaveId, CancellationToken cancellationToken)
    {
        return DecideAsync(leaveId, approve: false, cancellationToken);
    }

    private async Task<ActionResult<LeaveRequestApiResponse>> DecideAsync(
        Guid leaveId,
        bool approve,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentRestaurantId(out var restaurantId))
        {
            return Forbid();
        }

        try
        {
            var response = await decideLeaveUseCase.ExecuteAsync(
                new DecideLeaveRequest(restaurantId, leaveId, approve),
                cancellationToken);

            return Ok(ToApiResponse(response));
        }
        catch (Exception exception) when (
            exception is KeyNotFoundException or InvalidOperationException)
        {
            return HandleKnownException(exception);
        }
    }

    private static LeaveRequestApiResponse ToApiResponse(LeaveRequestResponse response)
    {
        return new LeaveRequestApiResponse(
            response.Id,
            response.StaffUserId,
            response.FromDate,
            response.ToDate,
            response.Reason,
            response.Status,
            response.CreatedAtUtc,
            response.DecidedAtUtc);
    }
}
