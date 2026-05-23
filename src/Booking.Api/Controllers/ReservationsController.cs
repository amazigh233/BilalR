using Booking.Api.Contracts.Reservations;
using Booking.Application.Reservations;
using Microsoft.AspNetCore.Mvc;

namespace Booking.Api.Controllers;

[Route("api/reservations")]
public sealed class ReservationsController(
    CreateReservationUseCase createReservationUseCase,
    GetReservationsUseCase getReservationsUseCase,
    GetReservationUseCase getReservationUseCase,
    ChangeReservationStatusUseCase changeReservationStatusUseCase) : ApiControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(ReservationApiResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReservationApiResponse>> Create(
        [FromBody] CreateReservationApiRequest? request,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateCreateRequest(request);
        if (validationError is not null)
        {
            return BadRequest(ToError(validationError));
        }

        try
        {
            var response = await createReservationUseCase.ExecuteAsync(
                new CreateReservationRequest(
                    request!.RestaurantId,
                    request.ReservationDateTime,
                    request.PartySize,
                    new CustomerRequest(
                        request.Customer.Name,
                        request.Customer.Email,
                        request.Customer.PhoneNumber)),
                cancellationToken);

            var apiResponse = ToApiResponse(response);

            return CreatedAtAction(
                nameof(GetById),
                new { reservationId = apiResponse.Id },
                apiResponse);
        }
        catch (Exception exception) when (
            exception is ArgumentException
                or InvalidOperationException
                or KeyNotFoundException)
        {
            return HandleKnownException(exception);
        }
    }

    [HttpGet("{reservationId}")]
    [ProducesResponseType(typeof(ReservationApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReservationApiResponse>> GetById(
        Guid reservationId,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await getReservationUseCase.ExecuteAsync(
                reservationId,
                cancellationToken);

            return Ok(ToApiResponse(response));
        }
        catch (Exception exception) when (exception is ArgumentException or KeyNotFoundException)
        {
            return HandleKnownException(exception);
        }
    }

    [HttpGet("/api/restaurants/{restaurantId}/reservations")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ReservationApiResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyCollection<ReservationApiResponse>>> GetByRestaurant(
        Guid restaurantId,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await getReservationsUseCase.ExecuteAsync(
                new GetReservationsRequest(restaurantId),
                cancellationToken);

            return Ok(response.Select(ToApiResponse).ToList());
        }
        catch (Exception exception) when (exception is ArgumentException or KeyNotFoundException)
        {
            return HandleKnownException(exception);
        }
    }

    [HttpPatch("{reservationId}/status")]
    [ProducesResponseType(typeof(ReservationApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReservationApiResponse>> ChangeStatus(
        Guid reservationId,
        [FromBody] ChangeReservationStatusApiRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(ToError("Request body is required."));
        }

        try
        {
            var response = await changeReservationStatusUseCase.ExecuteAsync(
                new ChangeReservationStatusRequest(
                    reservationId,
                    request.Status),
                cancellationToken);

            return Ok(ToApiResponse(response));
        }
        catch (Exception exception) when (
            exception is ArgumentException
                or InvalidOperationException
                or KeyNotFoundException)
        {
            return HandleKnownException(exception);
        }
    }

    private static string? ValidateCreateRequest(CreateReservationApiRequest? request)
    {
        if (request is null)
        {
            return "Request body is required.";
        }

        if (request.RestaurantId == Guid.Empty)
        {
            return "Restaurant id is required.";
        }

        if (request.ReservationDateTime == default)
        {
            return "Reservation date/time is required.";
        }

        if (request.PartySize <= 0)
        {
            return "Party size must be greater than 0.";
        }

        if (request.Customer is null)
        {
            return "Customer is required.";
        }

        if (string.IsNullOrWhiteSpace(request.Customer.Name))
        {
            return "Customer name is required.";
        }

        if (string.IsNullOrWhiteSpace(request.Customer.Email))
        {
            return "Customer email is required.";
        }

        return null;
    }

    private static ReservationApiResponse ToApiResponse(ReservationResponse response)
    {
        return new ReservationApiResponse(
            response.Id,
            response.RestaurantId,
            response.CustomerId,
            response.CustomerName,
            response.CustomerEmail,
            response.CustomerPhoneNumber,
            response.ReservationDateTime,
            response.PartySize,
            response.Status,
            response.CreatedAtUtc);
    }
}
