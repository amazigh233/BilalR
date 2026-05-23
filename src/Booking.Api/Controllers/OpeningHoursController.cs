using Booking.Api.Contracts.OpeningHours;
using Booking.Application.OpeningHours;
using Microsoft.AspNetCore.Mvc;

namespace Booking.Api.Controllers;

[Route("api/restaurants/{restaurantId}/opening-hours")]
public sealed class OpeningHoursController(
    SetOpeningHoursUseCase setOpeningHoursUseCase,
    GetOpeningHoursUseCase getOpeningHoursUseCase) : ApiControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(OpeningHoursApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OpeningHoursApiResponse>> Set(
        Guid restaurantId,
        [FromBody] SetOpeningHoursApiRequest? request,
        CancellationToken cancellationToken)
    {
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

    [HttpGet]
    [ProducesResponseType(typeof(OpeningHoursApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OpeningHoursApiResponse>> Get(
        Guid restaurantId,
        CancellationToken cancellationToken)
    {
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
