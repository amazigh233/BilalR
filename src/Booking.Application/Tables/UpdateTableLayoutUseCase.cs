using Booking.Application.Abstractions;
using Booking.Domain.Tables;

namespace Booking.Application.Tables;

public sealed record UpdateTableLayoutRequest(
    Guid RestaurantId,
    Guid TableId,
    int PosX,
    int PosY,
    int Rotation,
    TableShape Shape);

public sealed class UpdateTableLayoutUseCase(ITableRepository tableRepository)
{
    public async Task ExecuteAsync(UpdateTableLayoutRequest request, CancellationToken cancellationToken = default)
    {
        var table = await tableRepository.GetByIdAsync(request.TableId, cancellationToken)
            ?? throw new KeyNotFoundException($"Tafel '{request.TableId}' bestaat niet.");

        if (table.RestaurantId != request.RestaurantId)
        {
            throw new KeyNotFoundException($"Tafel '{request.TableId}' bestaat niet.");
        }

        table.UpdateLayout(request.PosX, request.PosY, request.Rotation, request.Shape);
        await tableRepository.UpdateAsync(table, cancellationToken);
    }
}
