namespace Booking.Domain.Restaurants;

public sealed class Restaurant
{
    private readonly List<OpeningHour> _openingHours = [];

    private Restaurant()
    {
        Name = string.Empty;
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
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public string? PhoneNumber { get; private set; }

    public string? Email { get; private set; }

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
}
