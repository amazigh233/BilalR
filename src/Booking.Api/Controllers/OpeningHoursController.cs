using Booking.Api.Contracts.OpeningHours;
using Booking.Application.OpeningHours;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Booking.Api.Controllers;

[Route("api/restaurants/{restaurantId}/opening-hours")]
public sealed class OpeningHoursController(
    SetOpeningHoursUseCase setOpeningHoursUseCase,
    GetOpeningHoursUseCase getOpeningHoursUseCase) : ApiControllerBase
{
    [HttpPost]
    [Authorize(Policy = "RestaurantOwner")]
    [ProducesResponseType(typeof(OpeningHoursApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OpeningHoursApiResponse>> Set(
        Guid restaurantId,
        [FromBody] SetOpeningHoursApiRequest? request,
        CancellationToken cancellationToken)
    {
        if (EnsureCurrentRestaurant(restaurantId, out var currentRestaurantId) is { } accessError)
        {
            return accessError;
        }

        if (request is null)
        {
            return BadRequest(ToError("Request body is required."));
        }

        if (request.OpeningHours is null || request.OpeningHours.Count == 0)
        {
            return BadRequest(ToError("At least one opening hour is required."));
        }

        try
        {
            var response = await setOpeningHoursUseCase.ExecuteAsync(
                new SetOpeningHoursRequest(
                    currentRestaurantId,
                    request.OpeningHours
                        .Select(openingHour => new OpeningHourRequest(
                            openingHour.DayOfWeek,
                            openingHour.OpensAt,
                            openingHour.ClosesAt))
                        .ToList()),
                cancellationToken);

            return Ok(ToApiResponse(response.RestaurantId, response.OpeningHours));
        }
        catch (Exception exception) when (exception is ArgumentException or KeyNotFoundException)
        {
            return HandleKnownException(exception);
        }
    }

    [HttpGet]
    [Authorize(Policy = "RestaurantOwner")]
    [ProducesResponseType(typeof(OpeningHoursApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OpeningHoursApiResponse>> Get(
        Guid restaurantId,
        CancellationToken cancellationToken)
    {
        if (EnsureCurrentRestaurant(restaurantId, out var currentRestaurantId) is { } accessError)
        {
            return accessError;
        }

        try
        {
            var response = await getOpeningHoursUseCase.ExecuteAsync(
                currentRestaurantId,
                cancellationToken);

            return Ok(ToApiResponse(response.RestaurantId, response.OpeningHours));
        }
        catch (Exception exception) when (exception is ArgumentException or KeyNotFoundException)
        {
            return HandleKnownException(exception);
        }
    }

    [HttpPost("/api/admin/restaurant/opening-hours")]
    [Authorize(Policy = "RestaurantOwner")]
    [ProducesResponseType(typeof(OpeningHoursApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OpeningHoursApiResponse>> SetCurrentRestaurant(
        [FromBody] SetOpeningHoursApiRequest? request,
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

        if (request.OpeningHours is null || request.OpeningHours.Count == 0)
        {
            return BadRequest(ToError("At least one opening hour is required."));
        }

        try
        {
            var response = await setOpeningHoursUseCase.ExecuteAsync(
                new SetOpeningHoursRequest(
                    restaurantId,
                    request.OpeningHours
                        .Select(openingHour => new OpeningHourRequest(
                            openingHour.DayOfWeek,
                            openingHour.OpensAt,
                            openingHour.ClosesAt))
                        .ToList()),
                cancellationToken);

            return Ok(ToApiResponse(response.RestaurantId, response.OpeningHours));
        }
        catch (Exception exception) when (exception is ArgumentException or KeyNotFoundException)
        {
            return HandleKnownException(exception);
        }
    }

    [HttpGet("/api/admin/restaurant/opening-hours")]
    [Authorize(Policy = "RestaurantOwner")]
    [ProducesResponseType(typeof(OpeningHoursApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OpeningHoursApiResponse>> GetCurrentRestaurant(
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentRestaurantId(out var restaurantId))
        {
            return Forbid();
        }

        try
        {
            var response = await getOpeningHoursUseCase.ExecuteAsync(
                restaurantId,
                cancellationToken);

            return Ok(ToApiResponse(response.RestaurantId, response.OpeningHours));
        }
        catch (Exception exception) when (exception is ArgumentException or KeyNotFoundException)
        {
            return HandleKnownException(exception);
        }
    }

    private static OpeningHoursApiResponse ToApiResponse(
        Guid restaurantId,
        IReadOnlyCollection<OpeningHourResponse> openingHours)
    {
        return new OpeningHoursApiResponse(
            restaurantId,
            openingHours
                .Select(openingHour => new OpeningHourApiResponse(
                    openingHour.Id,
                    openingHour.DayOfWeek,
                    openingHour.OpensAt,
                    openingHour.ClosesAt))
                .ToList());
    }
}
