using Booking.Api.Authentication;
using Booking.Application.Abstractions;

namespace Booking.Api.Tenancy;

/// <summary>
/// Resolves the current tenant from the authenticated user's <c>restaurant_id</c> JWT claim.
/// </summary>
public sealed class HttpContextTenantProvider(IHttpContextAccessor httpContextAccessor) : ITenantProvider
{
    public Guid? CurrentRestaurantId
    {
        get
        {
            var value = httpContextAccessor.HttpContext?.User.FindFirst(BookingClaimTypes.RestaurantId)?.Value;

            return Guid.TryParse(value, out var restaurantId) ? restaurantId : null;
        }
    }
}
