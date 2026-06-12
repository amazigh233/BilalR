using Booking.Application.Abstractions;

namespace Booking.Api.Accounting;

public sealed class AccountingDailySyncService(
    IServiceScopeFactory scopeFactory,
    ILogger<AccountingDailySyncService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        using var timer = new PeriodicTimer(TimeSpan.FromHours(24));
        do
        {
            try
            {
                await SyncDueConnectionsAsync(stoppingToken);
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(exception, "Daily accounting synchronization failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task SyncDueConnectionsAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IAccountingRepository>();
        var connectorService = scope.ServiceProvider.GetRequiredService<AccountingConnectorService>();
        var due = await repository.GetConnectionsDueForSyncAsync(DateTime.UtcNow.AddHours(-20), cancellationToken);
        foreach (var connection in due)
        {
            try
            {
                await connectorService.SyncAsync(connection.RestaurantId, connection.Provider, connection.ExternalId, cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Accounting synchronization failed for {Provider} connection {ConnectionId}.", connection.Provider, connection.Id);
            }
        }
    }
}
