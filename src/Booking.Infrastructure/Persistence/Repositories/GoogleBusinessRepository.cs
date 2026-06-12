using Booking.Application.Abstractions;
using Booking.Domain.GoogleBusiness;
using Microsoft.EntityFrameworkCore;

namespace Booking.Infrastructure.Persistence.Repositories;

public sealed class GoogleBusinessRepository(BookingDbContext dbContext) : IGoogleBusinessRepository
{
    public Task<GoogleBusinessConnection?> GetConnectionAsync(Guid restaurantId, CancellationToken ct = default) =>
        dbContext.GoogleBusinessConnections
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.RestaurantId == restaurantId, ct);

    public Task<GoogleBusinessConnection?> GetConnectionByStateAsync(string oAuthState, CancellationToken ct = default) =>
        dbContext.GoogleBusinessConnections
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.OAuthState == oAuthState, ct);

    public async Task AddConnectionAsync(GoogleBusinessConnection connection, CancellationToken ct = default)
    {
        dbContext.GoogleBusinessConnections.Add(connection);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task UpdateConnectionAsync(GoogleBusinessConnection connection, CancellationToken ct = default)
    {
        dbContext.GoogleBusinessConnections.Update(connection);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyCollection<GoogleBusinessConnection>> GetConnectionsDueForReviewSyncAsync(DateTime beforeUtc, CancellationToken ct = default) =>
        await dbContext.GoogleBusinessConnections
            .IgnoreQueryFilters()
            .Where(c => c.Status == GoogleBusinessConnectionStatus.Connected &&
                        (!c.LastReviewSyncAtUtc.HasValue || c.LastReviewSyncAtUtc < beforeUtc))
            .ToListAsync(ct);

    public async Task<IReadOnlyCollection<GoogleReview>> GetReviewsAsync(Guid restaurantId, CancellationToken ct = default) =>
        await dbContext.GoogleReviews
            .IgnoreQueryFilters()
            .Where(r => r.RestaurantId == restaurantId)
            .OrderByDescending(r => r.CreateTime)
            .ToListAsync(ct);

    public Task<GoogleReview?> GetReviewByNameAsync(Guid restaurantId, string reviewName, CancellationToken ct = default) =>
        dbContext.GoogleReviews
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.RestaurantId == restaurantId && r.ReviewName == reviewName, ct);

    public async Task AddReviewAsync(GoogleReview review, CancellationToken ct = default)
    {
        dbContext.GoogleReviews.Add(review);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task UpdateReviewAsync(GoogleReview review, CancellationToken ct = default)
    {
        dbContext.GoogleReviews.Update(review);
        await dbContext.SaveChangesAsync(ct);
    }

    public Task<int> GetUnreadReviewCountAsync(Guid restaurantId, CancellationToken ct = default) =>
        dbContext.GoogleReviews
            .IgnoreQueryFilters()
            .CountAsync(r => r.RestaurantId == restaurantId && !r.IsRead, ct);
}
