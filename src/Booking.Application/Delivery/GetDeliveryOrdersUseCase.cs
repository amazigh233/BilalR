using Booking.Application.Abstractions;

namespace Booking.Application.Delivery;

public sealed record GetDeliveryOrdersRequest(Guid RestaurantId);

public sealed class GetDeliveryOrdersUseCase(IDeliveryOrderRepository deliveryOrderRepository)
{
    public async Task<IReadOnlyCollection<DeliveryOrderResponse>> ExecuteAsync(
        GetDeliveryOrdersRequest request,
        CancellationToken cancellationToken = default)
    {
        var orders = await deliveryOrderRepository.GetByRestaurantAsync(
            request.RestaurantId,
            cancellationToken);

        return orders.Select(DeliveryOrderResponse.FromOrder).ToList();
    }
}
