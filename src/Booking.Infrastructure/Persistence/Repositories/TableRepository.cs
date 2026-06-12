using Booking.Application.Abstractions;
using Booking.Domain.Tables;
using Microsoft.EntityFrameworkCore;

namespace Booking.Infrastructure.Persistence.Repositories;

public sealed class TableRepository(BookingDbContext dbContext) : ITableRepository
{
    public async Task AddAsync(Table table, CancellationToken cancellationToken = default)
    {
        dbContext.Tables.Add(table);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Table?> GetByIdAsync(Guid tableId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Tables
            .FirstOrDefaultAsync(table => table.Id == tableId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Table>> GetByRestaurantIdAsync(
        Guid restaurantId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Tables
            .Where(table => table.RestaurantId == restaurantId)
            .OrderBy(table => table.Section)
            .ThenBy(table => table.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(Table table, CancellationToken cancellationToken = default)
    {
        dbContext.Tables.Update(table);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
