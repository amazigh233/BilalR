using Booking.Api.Contracts.Delivery;
using Booking.Application.Delivery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Booking.Api.Controllers;

public sealed class DeliveryOrdersController(
    GetDeliveryOrdersUseCase getDeliveryOrdersUseCase) : ApiControllerBase
{
    [HttpGet("/api/admin/restaurant/delivery-orders")]
    [Authorize(Policy = "RestaurantUser")]
    [ProducesResponseType(typeof(IReadOnlyCollection<DeliveryOrderApiResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<DeliveryOrderApiResponse>>> GetForRestaurant(
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentRestaurantId(out var restaurantId))
        {
            return Forbid();
        }

        var orders = await getDeliveryOrdersUseCase.ExecuteAsync(
            new GetDeliveryOrdersRequest(restaurantId),
            cancellationToken);

        return Ok(orders.Select(ToApiResponse).ToList());
    }

    private static DeliveryOrderApiResponse ToApiResponse(DeliveryOrderResponse response)
    {
        return new DeliveryOrderApiResponse(
            response.Id,
            response.Provider,
            response.ExternalOrderId,
            response.CustomerName,
            response.CustomerPhone,
            response.DeliveryAddress,
            response.Note,
            response.Status,
            response.TotalAmount,
            response.Currency,
            response.PlacedAtUtc,
            response.CreatedAtUtc,
            response.Items.Select(item => new DeliveryOrderLineApiItem(item.Name, item.Quantity, item.UnitPrice)).ToList());
    }
}
