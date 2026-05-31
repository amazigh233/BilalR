using Booking.BlazorApp.Components;
using Booking.BlazorApp.ApiClients;
using Booking.BlazorApp.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();

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
builder.Services.AddHttpClient<ReservationApiClient>(client =>
    client.BaseAddress = new Uri(bookingApiBaseUrl));
builder.Services.AddHttpClient<OpeningHoursApiClient>(client =>
    client.BaseAddress = new Uri(bookingApiBaseUrl));
builder.Services.AddHttpClient<AvailabilityApiClient>(client =>
    client.BaseAddress = new Uri(bookingApiBaseUrl));

var app = builder.Build();

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
