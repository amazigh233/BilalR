using Booking.BlazorApp.Components;
using Booking.BlazorApp.ApiClients;
using Booking.BlazorApp.Authentication;
using Booking.BlazorApp.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();

var dataProtection = builder.Services
    .AddDataProtection()
    .SetApplicationName("Zambiq.Booking.BlazorApp");
var dataProtectionKeysPath = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrWhiteSpace(dataProtectionKeysPath))
{
    dataProtection.PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));
}

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "Booking.BlazorApp.Auth";
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/access-denied";
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

var bookingApiBaseUrl = builder.Configuration["BookingApi:BaseUrl"]
    ?? "http://localhost:5086";

builder.Services.AddHttpClient<AuthApiClient>(client =>
    client.BaseAddress = new Uri(bookingApiBaseUrl));
builder.Services.AddHttpClient<RestaurantApiClient>(client =>
    client.BaseAddress = new Uri(bookingApiBaseUrl));
builder.Services.AddHttpClient<RestaurantAccountApiClient>(client =>
    client.BaseAddress = new Uri(bookingApiBaseUrl));
builder.Services.AddHttpClient<ReservationApiClient>(client =>
    client.BaseAddress = new Uri(bookingApiBaseUrl));
builder.Services.AddHttpClient<StaffApiClient>(client =>
    client.BaseAddress = new Uri(bookingApiBaseUrl));
builder.Services.AddHttpClient<OpeningHoursApiClient>(client =>
    client.BaseAddress = new Uri(bookingApiBaseUrl));
builder.Services.AddHttpClient<AvailabilityApiClient>(client =>
    client.BaseAddress = new Uri(bookingApiBaseUrl));
builder.Services.AddHttpClient<AnalyticsApiClient>(client =>
    client.BaseAddress = new Uri(bookingApiBaseUrl));
builder.Services.AddHttpClient<ShiftApiClient>(client =>
    client.BaseAddress = new Uri(bookingApiBaseUrl));
builder.Services.AddHttpClient<StaffAvailabilityApiClient>(client =>
    client.BaseAddress = new Uri(bookingApiBaseUrl));
builder.Services.AddHttpClient<LeaveApiClient>(client =>
    client.BaseAddress = new Uri(bookingApiBaseUrl));
builder.Services.AddHttpClient<DeliveryApiClient>(client =>
    client.BaseAddress = new Uri(bookingApiBaseUrl));
builder.Services.AddHttpClient<WidgetSecurityApiClient>(client =>
    client.BaseAddress = new Uri(bookingApiBaseUrl));
builder.Services.AddHttpClient<WidgetSettingsApiClient>(client =>
    client.BaseAddress = new Uri(bookingApiBaseUrl));
builder.Services.AddHttpClient<AccountingApiClient>(client =>
    client.BaseAddress = new Uri(bookingApiBaseUrl));
builder.Services.AddHttpClient<TableApiClient>(client =>
    client.BaseAddress = new Uri(bookingApiBaseUrl));
builder.Services.AddHttpClient<GoogleBusinessApiClient>(client =>
    client.BaseAddress = new Uri(bookingApiBaseUrl));
builder.Services.AddTransient<WidgetFrameAncestorsProvider>();

var app = builder.Build();
WidgetSecurityPolicy.CreateFrameAncestorsDirective(app.Configuration);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

// Only embed routes receive the configured frame allowlist. Other pages retain the
// framework's default anti-clickjacking headers.
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/embed"))
    {
        var provider = context.RequestServices
            .GetRequiredService<WidgetFrameAncestorsProvider>();
        var restaurantId = WidgetSecurityPolicy.TryGetRestaurantId(
            context.Request.Path,
            out var parsedRestaurantId)
            ? parsedRestaurantId
            : (Guid?)null;
        var widgetFrameAncestorsDirective = await provider.GetDirectiveAsync(
            restaurantId,
            context.RequestAborted);

        context.Response.OnStarting(() =>
        {
            context.Response.Headers.Remove("X-Frame-Options");
            context.Response.Headers["Content-Security-Policy"] = widgetFrameAncestorsDirective;
            return Task.CompletedTask;
        });
    }

    await next();
});

app.MapPost("/auth/login", async (
    [FromForm] LoginForm form,
    HttpContext httpContext,
    AuthApiClient authApiClient) =>
{
    var returnUrl = NormalizeReturnUrl(form.ReturnUrl);

    try
    {
        var login = await authApiClient.LoginAsync(
            new LoginRequest(form.Email, form.Password),
            httpContext.RequestAborted);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, login.User.Id.ToString()),
            new(ClaimTypes.Name, string.IsNullOrWhiteSpace(login.User.DisplayName)
                ? login.User.Email
                : login.User.DisplayName),
            new(ClaimTypes.Email, login.User.Email),
            new(BookingAuthClaimTypes.AccessToken, login.AccessToken),
            new(BookingAuthClaimTypes.RestaurantId, login.User.RestaurantId.ToString()),
            new(BookingAuthClaimTypes.RestaurantName, login.User.RestaurantName)
        };

        foreach (var role in login.User.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var properties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = login.ExpiresAtUtc
        };

        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            properties);

        if (returnUrl == "/admin" &&
            login.User.Roles.Contains("SuperAdmin", StringComparer.Ordinal))
        {
            returnUrl = "/admin/restaurant-accounts/create";
        }

        return Results.LocalRedirect(returnUrl);
    }
    catch (ApiClientException exception)
    {
        var error = Uri.EscapeDataString(exception.Message);
        var encodedReturnUrl = Uri.EscapeDataString(returnUrl);

        return Results.LocalRedirect($"/login?error={error}&returnUrl={encodedReturnUrl}");
    }
});

app.MapPost("/auth/logout", async (HttpContext httpContext) =>
{
    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

    return Results.LocalRedirect("/login?loggedOut=true");
}).RequireAuthorization();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

static string NormalizeReturnUrl(string? returnUrl)
{
    if (string.IsNullOrWhiteSpace(returnUrl) ||
        !Uri.IsWellFormedUriString(returnUrl, UriKind.Relative) ||
        !returnUrl.StartsWith('/') ||
        returnUrl.StartsWith("//", StringComparison.Ordinal))
    {
        return "/admin";
    }

    return returnUrl;
}

public partial class Program
{
}
