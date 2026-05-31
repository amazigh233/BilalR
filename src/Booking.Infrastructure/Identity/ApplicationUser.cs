using Booking.Domain.Restaurants;
using Microsoft.AspNetCore.Identity;

namespace Booking.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;

    public Guid RestaurantId { get; set; }

    public Restaurant Restaurant { get; set; } = null!;
}
