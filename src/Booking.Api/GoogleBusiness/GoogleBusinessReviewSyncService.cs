using Booking.Application.Abstractions;
using Booking.Domain.GoogleBusiness;

namespace Booking.Api.GoogleBusiness;

public sealed class GoogleBusinessReviewSyncService(
    IServiceScopeFactory scopeFactory,
    ILogger<GoogleBusinessReviewSyncService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
        using var timer = new PeriodicTimer(TimeSpan.FromHours(6));
        do
        {
            try
            {
                await SyncAllAsync(stoppingToken);
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(exception, "Google Business Profile review sync failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task SyncAllAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IGoogleBusinessRepository>();
        var service = scope.ServiceProvider.GetRequiredService<GoogleBusinessService>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        if (!config.GetValue<bool>("GoogleBusiness:Enabled")) return;

        var due = await repository.GetConnectionsDueForReviewSyncAsync(DateTime.UtcNow.AddHours(-5), ct);
        foreach (var connection in due)
        {
            await SyncReviewsAsync(connection, service, repository, ct);
        }
    }

    private async Task SyncReviewsAsync(
        GoogleBusinessConnection connection,
        GoogleBusinessService service,
        IGoogleBusinessRepository repository,
        CancellationToken ct)
    {
        try
        {
            var reviews = await service.GetReviewsFromGbpAsync(connection, ct);
            foreach (var item in reviews)
            {
                var existing = await repository.GetReviewByNameAsync(connection.RestaurantId, item.ReviewName, ct);
                if (existing is null)
                {
                    var review = new GoogleReview(
                        connection.RestaurantId,
                        item.ReviewName,
                        item.ReviewerDisplayName,
                        item.StarRating,
                        item.Comment,
                        item.CreateTime,
                        item.UpdateTime,
                        item.ReplyComment,
                        item.ReplyUpdateTime,
                        DateTime.UtcNow);
                    await repository.AddReviewAsync(review, ct);
                }
                else
                {
                    existing.UpdateFromGbp(
                        item.ReviewerDisplayName,
                        item.StarRating,
                        item.Comment,
                        item.UpdateTime,
                        item.ReplyComment,
                        item.ReplyUpdateTime,
                        DateTime.UtcNow);
                    await repository.UpdateReviewAsync(existing, ct);
                }
            }
            connection.MarkReviewSynced(DateTime.UtcNow);
            await repository.UpdateConnectionAsync(connection, ct);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception,
                "Google review sync failed for restaurant {RestaurantId}.", connection.RestaurantId);
            connection.MarkSyncError(exception.Message[..Math.Min(exception.Message.Length, 1000)]);
            await repository.UpdateConnectionAsync(connection, ct);
        }
    }
}
