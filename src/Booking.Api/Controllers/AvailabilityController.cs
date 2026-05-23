using Booking.Api.Contracts.Availability;
using Booking.Application.Availability;
using Microsoft.AspNetCore.Mvc;

namespace Booking.Api.Controllers;

[Route("api/restaurants/{restaurantId}/availability")]
public sealed class AvailabilityController(
    CheckAvailabilityUseCase checkAvailabilityUseCase) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(AvailabilityApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AvailabilityApiResponse>> Check(
        Guid restaurantId,
        [FromQuery] DateTime dateTime,
        [FromQuery] int partySize,
        CancellationToken cancellationToken)
    {
        if (dateTime == default)
        {
            return BadRequest(ToError("Reservation date/time is required."));
        }

        if (partySize <= 0)
        {
            return BadRequest(ToError("Party size must be greater than 0."));
        }

        try
        {
            var response = await checkAvailabilityUseCase.ExecuteAsync(
                new CheckAvailabilityRequest(
                    restaurantId,
                    dateTime,
                    partySize),
                cancellationToken);

            return Ok(new AvailabilityApiResponse(
                response.IsAvailable,
                response.Reason));
        }
        catch (Exception exception) when (exception is ArgumentException or KeyNotFoundException)
        {
            return HandleKnownException(exception);
        }
    }
}
