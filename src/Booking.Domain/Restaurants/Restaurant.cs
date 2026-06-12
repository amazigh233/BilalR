namespace Booking.Domain.Restaurants;

public sealed class Restaurant
{
    public const string DefaultWidgetPrimaryColor = "#1f6655";
    public const string DefaultWidgetAccentColor = "#b97742";

    private readonly List<OpeningHour> _openingHours = [];

    private Restaurant()
    {
        Name = string.Empty;
        WidgetPrimaryColor = DefaultWidgetPrimaryColor;
        WidgetAccentColor = DefaultWidgetAccentColor;
    }

    public Restaurant(string name, string? phoneNumber = null, string? email = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Restaurant name is required.", nameof(name));
        }

        Id = Guid.NewGuid();
        Name = name.Trim();
        PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        WidgetPrimaryColor = DefaultWidgetPrimaryColor;
        WidgetAccentColor = DefaultWidgetAccentColor;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public string? PhoneNumber { get; private set; }

    public string? Email { get; private set; }

    public string WidgetPrimaryColor { get; private set; }

    public string WidgetAccentColor { get; private set; }

    public string? WidgetWelcomeText { get; private set; }

    public string? WidgetLogoUrl { get; private set; }

    public IReadOnlyCollection<OpeningHour> OpeningHours => _openingHours.AsReadOnly();

    public void UpdateDetails(string name, string? phoneNumber = null, string? email = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Restaurant name is required.", nameof(name));
        }

        Name = name.Trim();
        PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
    }

    public void AddOpeningHour(OpeningHour openingHour)
    {
        ArgumentNullException.ThrowIfNull(openingHour);

        _openingHours.Add(openingHour);
    }

    public void UpdateWidgetBranding(
        string primaryColor,
        string accentColor,
        string? welcomeText,
        string? logoUrl)
    {
        WidgetPrimaryColor = NormalizeColor(primaryColor, nameof(primaryColor));
        WidgetAccentColor = NormalizeColor(accentColor, nameof(accentColor));

        var normalizedWelcomeText = string.IsNullOrWhiteSpace(welcomeText)
            ? null
            : welcomeText.Trim();
        if (normalizedWelcomeText?.Length > 240)
        {
            throw new ArgumentException(
                "Welkomsttekst mag maximaal 240 tekens bevatten.",
                nameof(welcomeText));
        }

        WidgetWelcomeText = normalizedWelcomeText;
        WidgetLogoUrl = NormalizeLogoUrl(logoUrl);
    }

    private static string NormalizeColor(string color, string parameterName)
    {
        var normalized = color?.Trim().ToLowerInvariant();
        if (normalized is null ||
            normalized.Length != 7 ||
            normalized[0] != '#' ||
            normalized[1..].Any(character => !Uri.IsHexDigit(character)))
        {
            throw new ArgumentException(
                "Gebruik een geldige hexkleur, bijvoorbeeld #1f6655.",
                parameterName);
        }

        return normalized;
    }

    private static string? NormalizeLogoUrl(string? logoUrl)
    {
        if (string.IsNullOrWhiteSpace(logoUrl))
        {
            return null;
        }

        var normalized = logoUrl.Trim();
        if (normalized.Length > 500 ||
            !Uri.TryCreate(normalized, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
            !string.IsNullOrEmpty(uri.UserInfo))
        {
            throw new ArgumentException(
                "Gebruik een geldige publieke http- of https-logo-URL.",
                nameof(logoUrl));
        }

        return normalized;
    }
}
