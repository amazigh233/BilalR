using Booking.Api.Contracts.Restaurants;
using Booking.Application.Restaurants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Booking.Api.Controllers;

[Route("api/restaurants")]
public sealed class RestaurantsController(
    CreateRestaurantUseCase createRestaurantUseCase,
    GetRestaurantUseCase getRestaurantUseCase,
    UpdateRestaurantUseCase updateRestaurantUseCase) : ApiControllerBase
{
    [HttpPost]
    [Authorize(Policy = "RestaurantOwner")]
    [ProducesResponseType(typeof(RestaurantApiResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RestaurantApiResponse>> Create(
        [FromBody] CreateRestaurantApiRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(ToError("Request body is required."));
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(ToError("Restaurant name is required."));
        }

        try
        {
            var response = await createRestaurantUseCase.ExecuteAsync(
                new CreateRestaurantRequest(
                    request.Name,
                    request.PhoneNumber,
                    request.Email),
                cancellationToken);

            var apiResponse = ToApiResponse(response);

            return CreatedAtAction(
                nameof(GetById),
                new { restaurantId = apiResponse.Id },
                apiResponse);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return HandleKnownException(exception);
        }
    }

    [HttpGet]
    [Authorize(Policy = "RestaurantOwner")]
    [ProducesResponseType(typeof(IReadOnlyCollection<RestaurantApiResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<RestaurantApiResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentRestaurantId(out var restaurantId))
        {
            return Forbid();
        }

        var response = await getRestaurantUseCase.ExecuteAsync(restaurantId, cancellationToken);

        return Ok(new[] { ToApiResponse(response) });
    }

    [HttpGet("{restaurantId}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RestaurantApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RestaurantApiResponse>> GetById(
        Guid restaurantId,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await getRestaurantUseCase.ExecuteAsync(restaurantId, cancellationToken);

            return Ok(ToApiResponse(response));
        }
        catch (Exception exception) when (exception is ArgumentException or KeyNotFoundException)
        {
            return HandleKnownException(exception);
        }
    }

    [HttpGet("/api/admin/restaurant")]
    [Authorize(Policy = "RestaurantUser")]
    [ProducesResponseType(typeof(RestaurantApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RestaurantApiResponse>> GetCurrent(
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentRestaurantId(out var restaurantId))
        {
            return Forbid();
        }

        try
        {
            var response = await getRestaurantUseCase.ExecuteAsync(restaurantId, cancellationToken);

            return Ok(ToApiResponse(response));
        }
        catch (Exception exception) when (exception is ArgumentException or KeyNotFoundException)
        {
            return HandleKnownException(exception);
        }
    }

    [HttpPut("/api/admin/restaurant")]
    [Authorize(Policy = "RestaurantOwner")]
    [ProducesResponseType(typeof(RestaurantApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RestaurantApiResponse>> UpdateCurrent(
        [FromBody] UpdateRestaurantApiRequest? request,
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

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(ToError("Restaurant name is required."));
        }

        try
        {
            var response = await updateRestaurantUseCase.ExecuteAsync(
                new UpdateRestaurantRequest(
                    restaurantId,
                    request.Name,
                    request.PhoneNumber,
                    request.Email),
                cancellationToken);

            return Ok(ToApiResponse(response));
        }
        catch (Exception exception) when (exception is ArgumentException or KeyNotFoundException)
        {
            return HandleKnownException(exception);
        }
    }

    private static RestaurantApiResponse ToApiResponse(RestaurantResponse response)
    {
        return new RestaurantApiResponse(
            response.Id,
            response.Name,
            response.PhoneNumber,
            response.Email);
    }

    private static RestaurantApiResponse ToApiResponse(CreateRestaurantResponse response)
    {
        return new RestaurantApiResponse(
            response.Id,
            response.Name,
            response.PhoneNumber,
            response.Email);
    }
}
