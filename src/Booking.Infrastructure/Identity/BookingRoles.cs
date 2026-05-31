namespace Booking.Infrastructure.Identity;

public static class BookingRoles
{
    public const string SuperAdmin = "SuperAdmin";

    public const string Owner = "Owner";

    public const string Staff = "Staff";

    public static readonly string[] All = [SuperAdmin, Owner, Staff];
}
