namespace Booking.BlazorApp.Authentication;

public sealed class LoginForm
{
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string ReturnUrl { get; set; } = "/admin";
}
