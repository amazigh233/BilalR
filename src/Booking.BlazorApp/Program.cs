using Booking.BlazorApp.Components;
using Booking.BlazorApp.ApiClients;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var bookingApiBaseUrl = builder.Configuration["BookingApi:BaseUrl"]
    ?? "http://localhost:5086";

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


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
