using Booking.Domain.Tables;

namespace Booking.Application.Tables;

public sealed record TableResponse(
    Guid Id,
    Guid RestaurantId,
    string Name,
    int Capacity,
    string? Section,
    bool IsActive,
    int PosX,
    int PosY,
    int Rotation,
    TableShape Shape)
{
    public static TableResponse FromTable(Table table) =>
        new(table.Id, table.RestaurantId, table.Name, table.Capacity, table.Section, table.IsActive,
            table.PosX, table.PosY, table.Rotation, table.Shape);
}
