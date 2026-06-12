using Booking.Domain.Tables;

namespace Booking.Application.Abstractions;

public interface ITableRepository
{
    Task AddAsync(Table table, CancellationToken cancellationToken = default);

    Task<Table?> GetByIdAsync(Guid tableId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Table>> GetByRestaurantIdAsync(
        Guid restaurantId,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(Table table, CancellationToken cancellationToken = default);
}
