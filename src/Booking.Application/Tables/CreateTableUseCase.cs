using Booking.Application.Abstractions;
using Booking.Domain.Tables;

namespace Booking.Application.Tables;

public sealed record CreateTableRequest(Guid RestaurantId, string Name, int Capacity, string? Section);

public sealed class CreateTableUseCase(ITableRepository tableRepository)
{
    public async Task<TableResponse> ExecuteAsync(
        CreateTableRequest request,
        CancellationToken cancellationToken = default)
    {
        var table = new Table(request.RestaurantId, request.Name, request.Capacity, request.Section);
        await tableRepository.AddAsync(table, cancellationToken);
        return TableResponse.FromTable(table);
    }
}
