using Booking.Domain.Tables;

namespace Booking.Api.Contracts.Tables;

public sealed record CreateTableApiRequest(string? Name, int? Capacity, string? Section);

public sealed record UpdateTableApiRequest(string? Name, int? Capacity, string? Section);

public sealed record AssignTableApiRequest(Guid? TableId);

public sealed record UpdateTableLayoutApiRequest(int PosX, int PosY, int Rotation, TableShape Shape);

public sealed record TableApiResponse(
    Guid Id,
    string Name,
    int Capacity,
    string? Section,
    bool IsActive,
    int PosX,
    int PosY,
    int Rotation,
    TableShape Shape);
