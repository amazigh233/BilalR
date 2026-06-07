using Booking.Application.Delivery;
using Booking.Application.Tests.Fakes;
using Booking.Domain.Delivery;

namespace Booking.Application.Tests.Delivery;

public sealed class DeliveryUseCasesTests
{
    private static readonly Guid RestaurantId = Guid.NewGuid();

    private static readonly TimeProvider FixedTime =
        new FixedTimeProvider(new DateTimeOffset(2026, 06, 08, 18, 30, 00, TimeSpan.Zero));

    private static IngestDeliveryOrderRequest NewOrderRequest(string externalOrderId = "TB-1001")
    {
        return new IngestDeliveryOrderRequest(
            RestaurantId,
            DeliveryProvider.Thuisbezorgd,
            externalOrderId,
            "Jan",
            CustomerPhone: null,
            DeliveryAddress: "Straat 1",
            Note: null,
            Status: "Confirmed",
            TotalAmount: 24.50m,
            Currency: "EUR",
            PlacedAtUtc: new DateTime(2026, 06, 08, 18, 30, 00, DateTimeKind.Utc),
            Items: [new IngestDeliveryOrderLine("Pizza", 1, 12.00m)]);
    }

    [Fact]
    public async Task Ingest_CreatesOrder()
    {
        var repository = new FakeDeliveryOrderRepository();
        var useCase = new IngestDeliveryOrderUseCase(repository, FixedTime);

        var response = await useCase.ExecuteAsync(NewOrderRequest());

        Assert.Single(repository.Orders);
        Assert.Equal("TB-1001", response.ExternalOrderId);
        Assert.Equal(DeliveryProvider.Thuisbezorgd, response.Provider);
        Assert.Single(response.Items);
    }

    [Fact]
    public async Task Ingest_IsIdempotent_ForSameExternalOrderId()
    {
        var repository = new FakeDeliveryOrderRepository();
        var useCase = new IngestDeliveryOrderUseCase(repository, FixedTime);

        var first = await useCase.ExecuteAsync(NewOrderRequest());
        var second = await useCase.ExecuteAsync(NewOrderRequest());

        Assert.Single(repository.Orders);
        Assert.Equal(first.Id, second.Id);
    }

    [Fact]
    public async Task Connect_GeneratesSecret_AndEnables()
    {
        var repository = new FakeDeliveryIntegrationRepository();
        var useCase = new ConnectDeliveryProviderUseCase(repository, FixedTime);

        var result = await useCase.ExecuteAsync(
            new ConnectDeliveryProviderRequest(RestaurantId, DeliveryProvider.UberEats));

        Assert.False(string.IsNullOrWhiteSpace(result.Secret));
        var integration = Assert.Single(repository.Integrations);
        Assert.True(integration.Enabled);
        Assert.Equal(DeliverySecrets.Hash(result.Secret), integration.WebhookSecretHash);
    }

    [Fact]
    public async Task Connect_Twice_RotatesSecret_WithoutDuplicate()
    {
        var repository = new FakeDeliveryIntegrationRepository();
        var useCase = new ConnectDeliveryProviderUseCase(repository, FixedTime);

        var first = await useCase.ExecuteAsync(
            new ConnectDeliveryProviderRequest(RestaurantId, DeliveryProvider.Thuisbezorgd));
        var second = await useCase.ExecuteAsync(
            new ConnectDeliveryProviderRequest(RestaurantId, DeliveryProvider.Thuisbezorgd));

        Assert.Single(repository.Integrations);
        Assert.NotEqual(first.Secret, second.Secret);
    }

    [Fact]
    public async Task Disable_Throws_WhenNotConnected()
    {
        var repository = new FakeDeliveryIntegrationRepository();
        var useCase = new SetDeliveryProviderEnabledUseCase(repository);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            useCase.ExecuteAsync(new SetDeliveryProviderEnabledRequest(
                RestaurantId, DeliveryProvider.Thuisbezorgd, Enabled: false)));
    }

    [Fact]
    public async Task Resolve_ReturnsRestaurant_ForMatchingEnabledSecret()
    {
        var repository = new FakeDeliveryIntegrationRepository();
        var connect = new ConnectDeliveryProviderUseCase(repository, FixedTime);
        var connected = await connect.ExecuteAsync(
            new ConnectDeliveryProviderRequest(RestaurantId, DeliveryProvider.Thuisbezorgd));

        var resolve = new ResolveDeliveryIntegrationUseCase(repository);
        var restaurantId = await resolve.ExecuteAsync(
            new ResolveDeliveryIntegrationRequest(DeliveryProvider.Thuisbezorgd, connected.Secret));

        Assert.Equal(RestaurantId, restaurantId);
    }

    [Fact]
    public async Task Resolve_ReturnsNull_ForWrongProvider()
    {
        var repository = new FakeDeliveryIntegrationRepository();
        var connect = new ConnectDeliveryProviderUseCase(repository, FixedTime);
        var connected = await connect.ExecuteAsync(
            new ConnectDeliveryProviderRequest(RestaurantId, DeliveryProvider.Thuisbezorgd));

        var resolve = new ResolveDeliveryIntegrationUseCase(repository);
        var restaurantId = await resolve.ExecuteAsync(
            new ResolveDeliveryIntegrationRequest(DeliveryProvider.UberEats, connected.Secret));

        Assert.Null(restaurantId);
    }

    [Fact]
    public async Task Resolve_ReturnsNull_WhenDisabled()
    {
        var repository = new FakeDeliveryIntegrationRepository();
        var connect = new ConnectDeliveryProviderUseCase(repository, FixedTime);
        var connected = await connect.ExecuteAsync(
            new ConnectDeliveryProviderRequest(RestaurantId, DeliveryProvider.Thuisbezorgd));
        await new SetDeliveryProviderEnabledUseCase(repository).ExecuteAsync(
            new SetDeliveryProviderEnabledRequest(RestaurantId, DeliveryProvider.Thuisbezorgd, Enabled: false));

        var resolve = new ResolveDeliveryIntegrationUseCase(repository);
        var restaurantId = await resolve.ExecuteAsync(
            new ResolveDeliveryIntegrationRequest(DeliveryProvider.Thuisbezorgd, connected.Secret));

        Assert.Null(restaurantId);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
