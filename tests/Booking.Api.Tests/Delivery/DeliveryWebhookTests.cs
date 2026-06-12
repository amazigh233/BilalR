using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Booking.Api.Contracts.Delivery;
using Booking.Api.Tests.Support;
using Booking.Infrastructure.Identity;

namespace Booking.Api.Tests.Delivery;

public sealed class DeliveryWebhookTests
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    private static DeliveryWebhookApiRequest NewWebhookOrder(string externalOrderId = "TB-1001")
    {
        return new DeliveryWebhookApiRequest(
            externalOrderId,
            "Jan",
            CustomerPhone: "0612345678",
            DeliveryAddress: "Straat 1, Amsterdam",
            Note: null,
            Status: "Confirmed",
            PlacedAtUtc: new DateTime(2026, 06, 08, 18, 30, 00, DateTimeKind.Utc),
            TotalAmount: 24.50m,
            Currency: "EUR",
            Items: [new DeliveryWebhookLineApiItem("Pizza Margherita", 1, 12.00m)]);
    }

    [Fact]
    public async Task ValidSecret_CreatesOrder_OwnerSeesIt()
    {
        await using var factory = new BookingApiFactory();
        await factory.ResetDatabaseAsync();
        var client = factory.CreateClient();

        var owner = await factory.SeedUserAsync(BookingRoles.Owner);
        await AuthenticateAsync(client, factory, owner.User.Email);
        var secret = await ConnectAsync(client, "Thuisbezorgd");

        var webhookStatus = await PostWebhookAsync(factory, "Thuisbezorgd", secret, NewWebhookOrder());
        Assert.Equal(HttpStatusCode.Accepted, webhookStatus);

        var orders = await client.GetFromJsonAsync<IReadOnlyCollection<DeliveryOrderApiResponse>>(
            "api/admin/restaurant/delivery-orders", JsonOptions);
        var order = Assert.Single(orders!);
        Assert.Equal("TB-1001", order.ExternalOrderId);
        Assert.Equal("Jan", order.CustomerName);
        Assert.Single(order.Items);
    }

    [Fact]
    public async Task WrongSecret_IsRejected_AndNoOrderCreated()
    {
        await using var factory = new BookingApiFactory();
        await factory.ResetDatabaseAsync();
        var client = factory.CreateClient();

        var owner = await factory.SeedUserAsync(BookingRoles.Owner);
        await AuthenticateAsync(client, factory, owner.User.Email);
        await ConnectAsync(client, "Thuisbezorgd");

        var status = await PostWebhookAsync(factory, "Thuisbezorgd", "not-the-secret", NewWebhookOrder());
        Assert.Equal(HttpStatusCode.Unauthorized, status);

        var orders = await client.GetFromJsonAsync<IReadOnlyCollection<DeliveryOrderApiResponse>>(
            "api/admin/restaurant/delivery-orders", JsonOptions);
        Assert.Empty(orders!);
    }

    [Fact]
    public async Task DisabledIntegration_RejectsWebhook()
    {
        await using var factory = new BookingApiFactory();
        await factory.ResetDatabaseAsync();
        var client = factory.CreateClient();

        var owner = await factory.SeedUserAsync(BookingRoles.Owner);
        await AuthenticateAsync(client, factory, owner.User.Email);
        var secret = await ConnectAsync(client, "Thuisbezorgd");

        var disable = await client.PostAsync("api/admin/restaurant/delivery-integrations/Thuisbezorgd/disable", null);
        Assert.Equal(HttpStatusCode.OK, disable.StatusCode);

        var status = await PostWebhookAsync(factory, "Thuisbezorgd", secret, NewWebhookOrder());
        Assert.Equal(HttpStatusCode.Unauthorized, status);
    }

    [Fact]
    public async Task DuplicateExternalOrderId_DoesNotCreateSecondOrder()
    {
        await using var factory = new BookingApiFactory();
        await factory.ResetDatabaseAsync();
        var client = factory.CreateClient();

        var owner = await factory.SeedUserAsync(BookingRoles.Owner);
        await AuthenticateAsync(client, factory, owner.User.Email);
        var secret = await ConnectAsync(client, "Thuisbezorgd");

        await PostWebhookAsync(factory, "Thuisbezorgd", secret, NewWebhookOrder());
        await PostWebhookAsync(factory, "Thuisbezorgd", secret, NewWebhookOrder());

        var orders = await client.GetFromJsonAsync<IReadOnlyCollection<DeliveryOrderApiResponse>>(
            "api/admin/restaurant/delivery-orders", JsonOptions);
        Assert.Single(orders!);
    }

    [Fact]
    public async Task OrdersAreTenantIsolated()
    {
        await using var factory = new BookingApiFactory();
        await factory.ResetDatabaseAsync();
        var client = factory.CreateClient();

        var ownerA = await factory.SeedUserAsync(BookingRoles.Owner);
        var ownerB = await factory.SeedUserAsync(BookingRoles.Owner);

        await AuthenticateAsync(client, factory, ownerA.User.Email);
        var secretA = await ConnectAsync(client, "Thuisbezorgd");
        await PostWebhookAsync(factory, "Thuisbezorgd", secretA, NewWebhookOrder());

        await AuthenticateAsync(client, factory, ownerB.User.Email);
        var ordersB = await client.GetFromJsonAsync<IReadOnlyCollection<DeliveryOrderApiResponse>>(
            "api/admin/restaurant/delivery-orders", JsonOptions);
        Assert.Empty(ordersB!);
    }

    [Theory]
    [InlineData("api/delivery/thuisbezorgd/jet-connect/orders", "X-JET-Connect-Secret", "JET-1001")]
    [InlineData("api/delivery/thuisbezorgd/t-connect/orders", "X-Zambiq-Connector-Secret", "TC-1001")]
    public async Task ThuisbezorgdIngressRoutes_CreateOrder(
        string route,
        string secretHeader,
        string externalOrderId)
    {
        await using var factory = new BookingApiFactory();
        await factory.ResetDatabaseAsync();
        var client = factory.CreateClient();

        var owner = await factory.SeedUserAsync(BookingRoles.Owner);
        await AuthenticateAsync(client, factory, owner.User.Email);
        var secret = await ConnectAsync(client, "Thuisbezorgd");

        using var anonymous = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, route)
        {
            Content = JsonContent.Create(NewWebhookOrder(externalOrderId))
        };
        request.Headers.Add(secretHeader, secret);

        using var response = await anonymous.SendAsync(request);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var orders = await client.GetFromJsonAsync<IReadOnlyCollection<DeliveryOrderApiResponse>>(
            "api/admin/restaurant/delivery-orders", JsonOptions);
        Assert.Contains(orders!, order => order.ExternalOrderId == externalOrderId);
    }

    [Fact]
    public async Task Staff_CanViewOrders_ButCannotConnect()
    {
        await using var factory = new BookingApiFactory();
        await factory.ResetDatabaseAsync();
        var client = factory.CreateClient();

        var owner = await factory.SeedUserAsync(BookingRoles.Owner);
        var staff = await factory.SeedUserAsync(BookingRoles.Staff, restaurantId: owner.Restaurant.Id);

        await AuthenticateAsync(client, factory, staff.User.Email);

        var view = await client.GetAsync("api/admin/restaurant/delivery-orders");
        Assert.Equal(HttpStatusCode.OK, view.StatusCode);

        var connect = await client.PostAsync("api/admin/restaurant/delivery-integrations/Thuisbezorgd/connect", null);
        Assert.Equal(HttpStatusCode.Forbidden, connect.StatusCode);
    }

    private static async Task<string> ConnectAsync(HttpClient client, string provider)
    {
        var response = await client.PostAsync($"api/admin/restaurant/delivery-integrations/{provider}/connect", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var connected = await response.Content.ReadFromJsonAsync<ConnectDeliveryApiResponse>(JsonOptions);
        Assert.NotNull(connected);
        if (provider == "Thuisbezorgd")
        {
            Assert.NotNull(connected!.JetConnectOrderUrl);
            Assert.NotNull(connected.TConnectOrderUrl);
        }
        return connected!.Secret;
    }

    private static async Task<HttpStatusCode> PostWebhookAsync(
        BookingApiFactory factory,
        string provider,
        string secret,
        DeliveryWebhookApiRequest body)
    {
        // The webhook is anonymous; use a fresh client with no JWT.
        using var anonymous = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, $"api/delivery/webhook/{provider}")
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Add("X-Webhook-Secret", secret);

        using var response = await anonymous.SendAsync(request);
        return response.StatusCode;
    }

    private static async Task AuthenticateAsync(HttpClient client, BookingApiFactory factory, string? email)
    {
        var token = await factory.LoginAsync(client, email ?? string.Empty);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}
