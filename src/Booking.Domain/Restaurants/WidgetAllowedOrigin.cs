namespace Booking.Domain.Restaurants;

public sealed class WidgetAllowedOrigin
{
    private WidgetAllowedOrigin()
    {
        Origin = string.Empty;
    }

    public WidgetAllowedOrigin(Guid restaurantId, string origin)
    {
        if (restaurantId == Guid.Empty)
        {
            throw new ArgumentException("Restaurant id is required.", nameof(restaurantId));
        }

        Id = Guid.NewGuid();
        RestaurantId = restaurantId;
        Origin = Normalize(origin);
    }

    public Guid Id { get; private set; }

    public Guid RestaurantId { get; private set; }

    public string Origin { get; private set; }

    public static string Normalize(string origin)
    {
        if (string.IsNullOrWhiteSpace(origin) ||
            !Uri.TryCreate(origin.Trim(), UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
            string.IsNullOrWhiteSpace(uri.Host) ||
            !string.IsNullOrEmpty(uri.UserInfo) ||
            uri.AbsolutePath != "/" ||
            !string.IsNullOrEmpty(uri.Query) ||
            !string.IsNullOrEmpty(uri.Fragment))
        {
            throw new ArgumentException(
                "Gebruik een volledige website-origin zonder pad, bijvoorbeeld https://restaurant.nl.",
                nameof(origin));
        }

        return uri.GetLeftPart(UriPartial.Authority).ToLowerInvariant();
    }
}
