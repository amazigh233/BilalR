using Booking.Api.Contracts.Reservations;
using Booking.Application.Reservations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Booking.Api.Controllers;

[Route("api/reservations")]
public sealed class ReservationsController(
    CreateReservationUseCase createReservationUseCase,
    GetReservationsUseCase getReservationsUseCase,
    GetReservationUseCase getReservationUseCase,
    ChangeRestaurantReservationStatusUseCase changeRestaurantReservationStatusUseCase) : ApiControllerBase
{
    [HttpPost]
    [AllowAnonymous]
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
                        request.Customer.PhoneNumber),
                    request.Note),
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
    [Authorize(Policy = "RestaurantUser")]
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

            if (EnsureCurrentRestaurant(response.RestaurantId, out _) is { } accessError)
            {
                return accessError;
            }

            return Ok(ToApiResponse(response));
        }
        catch (Exception exception) when (exception is ArgumentException or KeyNotFoundException)
        {
            return HandleKnownException(exception);
        }
    }

    [HttpGet("/api/restaurants/{restaurantId}/reservations")]
    [Authorize(Policy = "RestaurantUser")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ReservationApiResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyCollection<ReservationApiResponse>>> GetByRestaurant(
        Guid restaurantId,
        CancellationToken cancellationToken)
    {
        if (EnsureCurrentRestaurant(restaurantId, out var currentRestaurantId) is { } accessError)
        {
            return accessError;
        }

        try
        {
            var response = await getReservationsUseCase.ExecuteAsync(
                new GetReservationsRequest(currentRestaurantId),
                cancellationToken);

            return Ok(response.Select(ToApiResponse).ToList());
        }
        catch (Exception exception) when (exception is ArgumentException or KeyNotFoundException)
        {
            return HandleKnownException(exception);
        }
    }

    [HttpPatch("{reservationId}/status")]
    [Authorize(Policy = "RestaurantUser")]
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

        if (!TryGetCurrentRestaurantId(out var restaurantId))
        {
            return Forbid();
        }

        try
        {
            var response = await changeRestaurantReservationStatusUseCase.ExecuteAsync(
                new ChangeRestaurantReservationStatusRequest(
                    restaurantId,
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

    [HttpGet("/api/admin/restaurant/reservations")]
    [Authorize(Policy = "RestaurantUser")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ReservationApiResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyCollection<ReservationApiResponse>>> GetCurrentRestaurantReservations(
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentRestaurantId(out var restaurantId))
        {
            return Forbid();
        }

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

    [HttpPatch("/api/admin/restaurant/reservations/{reservationId}/status")]
    [Authorize(Policy = "RestaurantUser")]
    [ProducesResponseType(typeof(ReservationApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReservationApiResponse>> ChangeCurrentRestaurantReservationStatus(
        Guid reservationId,
        [FromBody] ChangeReservationStatusApiRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(ToError("Request body is required."));
        }

        if (!TryGetCurrentRestaurantId(out var restaurantId))
        {
            return Forbid();
        }

        try
        {
            var response = await changeRestaurantReservationStatusUseCase.ExecuteAsync(
                new ChangeRestaurantReservationStatusRequest(
                    restaurantId,
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
            response.Note,
            response.Status,
            response.CreatedAtUtc);
    }
}
