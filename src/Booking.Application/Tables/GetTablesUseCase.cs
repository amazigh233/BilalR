using Booking.Application.Abstractions;

namespace Booking.Application.Tables;

public sealed class GetTablesUseCase(ITableRepository tableRepository)
{
    public async Task<IReadOnlyCollection<TableResponse>> ExecuteAsync(
        Guid restaurantId,
        CancellationToken cancellationToken = default)
    {
        if (restaurantId == Guid.Empty)
        {
            throw new ArgumentException("Restaurant id is required.", nameof(restaurantId));
        }

        var tables = await tableRepository.GetByRestaurantIdAsync(restaurantId, cancellationToken);
        return tables.Select(TableResponse.FromTable).ToList();
    }
}
