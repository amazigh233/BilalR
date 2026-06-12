using Booking.Application.Abstractions;

namespace Booking.Application.Tables;

public sealed class DeactivateTableUseCase(ITableRepository tableRepository)
{
    public async Task ExecuteAsync(
        Guid restaurantId,
        Guid tableId,
        CancellationToken cancellationToken = default)
    {
        var table = await tableRepository.GetByIdAsync(tableId, cancellationToken)
            ?? throw new KeyNotFoundException($"Tafel '{tableId}' bestaat niet.");

        if (table.RestaurantId != restaurantId)
        {
            throw new KeyNotFoundException($"Tafel '{tableId}' bestaat niet.");
        }

        table.Deactivate();
        await tableRepository.UpdateAsync(table, cancellationToken);
    }
}

public sealed class ReactivateTableUseCase(ITableRepository tableRepository)
{
    public async Task ExecuteAsync(
        Guid restaurantId,
        Guid tableId,
        CancellationToken cancellationToken = default)
    {
        var table = await tableRepository.GetByIdAsync(tableId, cancellationToken)
            ?? throw new KeyNotFoundException($"Tafel '{tableId}' bestaat niet.");

        if (table.RestaurantId != restaurantId)
        {
            throw new KeyNotFoundException($"Tafel '{tableId}' bestaat niet.");
        }

        table.Reactivate();
        await tableRepository.UpdateAsync(table, cancellationToken);
    }
}
