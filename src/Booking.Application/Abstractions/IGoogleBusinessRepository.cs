using Booking.Domain.GoogleBusiness;

namespace Booking.Application.Abstractions;

public interface IGoogleBusinessRepository
{
    Task<GoogleBusinessConnection?> GetConnectionAsync(Guid restaurantId, CancellationToken ct = default);
    Task<GoogleBusinessConnection?> GetConnectionByStateAsync(string oAuthState, CancellationToken ct = default);
    Task AddConnectionAsync(GoogleBusinessConnection connection, CancellationToken ct = default);
    Task UpdateConnectionAsync(GoogleBusinessConnection connection, CancellationToken ct = default);
    Task<IReadOnlyCollection<GoogleBusinessConnection>> GetConnectionsDueForReviewSyncAsync(DateTime beforeUtc, CancellationToken ct = default);
    Task<IReadOnlyCollection<GoogleReview>> GetReviewsAsync(Guid restaurantId, CancellationToken ct = default);
    Task<GoogleReview?> GetReviewByNameAsync(Guid restaurantId, string reviewName, CancellationToken ct = default);
    Task AddReviewAsync(GoogleReview review, CancellationToken ct = default);
    Task UpdateReviewAsync(GoogleReview review, CancellationToken ct = default);
    Task<int> GetUnreadReviewCountAsync(Guid restaurantId, CancellationToken ct = default);
}
