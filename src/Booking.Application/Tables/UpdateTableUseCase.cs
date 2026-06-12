using Booking.Application.Abstractions;

namespace Booking.Application.Tables;

public sealed record UpdateTableRequest(Guid RestaurantId, Guid TableId, string Name, int Capacity, string? Section);

public sealed class UpdateTableUseCase(ITableRepository tableRepository)
{
    public async Task<TableResponse> ExecuteAsync(
        UpdateTableRequest request,
        CancellationToken cancellationToken = default)
    {
        var table = await tableRepository.GetByIdAsync(request.TableId, cancellationToken)
            ?? throw new KeyNotFoundException($"Tafel '{request.TableId}' bestaat niet.");

        if (table.RestaurantId != request.RestaurantId)
        {
            throw new KeyNotFoundException($"Tafel '{request.TableId}' bestaat niet.");
        }

        table.UpdateDetails(request.Name, request.Capacity, request.Section);
        await tableRepository.UpdateAsync(table, cancellationToken);
        return TableResponse.FromTable(table);
    }
}
