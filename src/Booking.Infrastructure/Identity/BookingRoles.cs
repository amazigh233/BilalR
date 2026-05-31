namespace Booking.Infrastructure.Identity;

public static class BookingRoles
{
    public const string Owner = "Owner";

    public const string Staff = "Staff";

    public static readonly string[] All = [Owner, Staff];
}
