namespace Booking.Application.Abstractions;

/// <summary>
/// Provides the restaurant (tenant) that the current request operates on.
/// Used by the persistence layer to enforce tenant isolation via global query filters.
/// Returns <c>null</c> when there is no authenticated tenant (for example anonymous
/// public requests or design-time tooling).
/// </summary>
public interface ITenantProvider
{
    Guid? CurrentRestaurantId { get; }
}
